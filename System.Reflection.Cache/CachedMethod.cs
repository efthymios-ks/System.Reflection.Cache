using System.Reflection.Cache.Accessors;

namespace System.Reflection.Cache;

public sealed class CachedMethod : ICachedMember
{
    private readonly Lazy<Func<object?, object?[], object?>> _invoker;
    private readonly Lazy<CachedAttributes> _attributes;

    internal CachedMethod(MethodInfo method)
    {
        Method = method;
        ParameterTypes = [.. method.GetParameters().Select(parameter => parameter.ParameterType)];
        _invoker = Lazily.Create(() => AccessorFactory.BuildInvoker(method));
        _attributes = Lazily.Create(() => new CachedAttributes(method.GetCustomAttributes()));
    }

    public MethodInfo Method { get; }

    public IReadOnlyList<Type> ParameterTypes { get; }

    public string Name
        => Method.Name;

    public Type ReturnType
        => Method.ReturnType;

    public bool IsStatic
        => Method.IsStatic;

    public CachedAttributes Attributes
        => _attributes.Value;

    /// <summary>Null for a void method.</summary>
    public object? Invoke(object? instance, params object?[] arguments)
        => _invoker.Value(instance, arguments);

    public override string ToString()
        => $"{ReturnType.Name} {Name}({string.Join(", ", ParameterTypes.Select(type => type.Name))})";
}
