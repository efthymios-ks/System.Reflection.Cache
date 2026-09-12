using System.Linq.Expressions;

namespace System.Reflection.Cache.Accessors;

/// <summary>
/// Compiles member access into delegates. Everything is typed <c>object</c> at the boundary, so one
/// compiled delegate serves every call site regardless of the declaring or member type.
/// </summary>
internal static class AccessorFactory
{
    public static Func<object?, object?>? BuildGetter(PropertyInfo property)
    {
        var getMethod = property.GetGetMethod(nonPublic: true);

        if (getMethod is null)
        {
            return null;
        }

        var instance = Expression.Parameter(typeof(object), "instance");

        var access = getMethod.IsStatic
            ? Expression.Property(null, property)
            : Expression.Property(Cast(instance, property.DeclaringType!), property);

        return Compile(access, instance);
    }

    public static Action<object?, object?>? BuildSetter(PropertyInfo property)
    {
        var setMethod = property.GetSetMethod(nonPublic: true);

        if (setMethod is null)
        {
            return null;
        }

        return BuildSetter(
            property.DeclaringType!,
            property.PropertyType,
            setMethod.IsStatic,
            (instance, value) => Expression.Assign(
                setMethod.IsStatic
                    ? Expression.Property(null, property)
                    : Expression.Property(Cast(instance, property.DeclaringType!), property),
                value
            )
        );
    }

    public static Func<object?, object?> BuildGetter(FieldInfo field)
    {
        var instance = Expression.Parameter(typeof(object), "instance");

        var access = field.IsStatic
            ? Expression.Field(null, field)
            : Expression.Field(Cast(instance, field.DeclaringType!), field);

        return Compile(access, instance);
    }

    /// <summary>
    /// Null for a readonly or literal field: assigning to one is rejected when the expression tree
    /// is compiled, which would otherwise surface far from the cause.
    /// </summary>
    public static Action<object?, object?>? BuildSetter(FieldInfo field)
    {
        if (field.IsInitOnly || field.IsLiteral)
        {
            return null;
        }

        return BuildSetter(
            field.DeclaringType!,
            field.FieldType,
            field.IsStatic,
            (instance, value) => Expression.Assign(
                field.IsStatic
                    ? Expression.Field(null, field)
                    : Expression.Field(Cast(instance, field.DeclaringType!), field),
                value
            )
        );
    }

    public static Func<object?, object?[], object?> BuildInvoker(MethodInfo method)
    {
        var instance = Expression.Parameter(typeof(object), "instance");
        var arguments = Expression.Parameter(typeof(object?[]), "arguments");

        var call = Expression.Call(
            method.IsStatic ? null : Cast(instance, method.DeclaringType!),
            method,
            Arguments(method.GetParameters(), arguments)
        );

        // A void method still has to return something through the object-typed delegate.
        var body = method.ReturnType == typeof(void)
            ? Expression.Block(call, Expression.Constant(null, typeof(object)))
            : Box(call);

        return Expression
            .Lambda<Func<object?, object?[], object?>>(body, instance, arguments)
            .Compile();
    }

    public static Func<object?[], object> BuildInvoker(ConstructorInfo constructor)
    {
        var arguments = Expression.Parameter(typeof(object?[]), "arguments");

        var create = Expression.New(constructor, Arguments(constructor.GetParameters(), arguments));

        return Expression
            .Lambda<Func<object?[], object>>(Box(create), arguments)
            .Compile();
    }

    private static Action<object?, object?>? BuildSetter(
        Type declaringType,
        Type memberType,
        bool isStatic,
        Func<ParameterExpression, Expression, Expression> assign
    )
    {
        // A mutable struct assigned through a boxed copy would update the copy and discard it.
        if (!isStatic && declaringType.IsValueType)
        {
            return null;
        }

        var instance = Expression.Parameter(typeof(object), "instance");
        var value = Expression.Parameter(typeof(object), "value");

        var body = assign(instance, Cast(value, memberType));

        return Expression
            .Lambda<Action<object?, object?>>(body, instance, value)
            .Compile();
    }

    private static Expression[] Arguments(ParameterInfo[] parameters, ParameterExpression arguments)
        =>
        [
            .. parameters.Select((parameter, index) => Cast(
                Expression.ArrayIndex(arguments, Expression.Constant(index)),
                parameter.ParameterType
            ))
        ];

    private static Func<object?, object?> Compile(Expression access, ParameterExpression instance)
        => Expression
            .Lambda<Func<object?, object?>>(Box(access), instance)
            .Compile();

    private static Expression Box(Expression value)
        => value.Type == typeof(object)
            ? value
            : Expression.Convert(value, typeof(object));

    private static Expression Cast(Expression value, Type targetType)
        => value.Type == targetType
            ? value
            : Expression.Convert(value, targetType);
}
