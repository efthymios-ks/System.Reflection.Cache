using System.Reflection.Cache.Accessors;

namespace System.Reflection.Cache;

public sealed class CachedConstructor
{
    private readonly Lazy<Func<object?[], object>> _invoker;
    private readonly Lazy<CachedAttributes> _attributes;

    internal CachedConstructor(ConstructorInfo constructor)
    {
        Constructor = constructor;
        ParameterTypes = [.. constructor.GetParameters().Select(parameter => parameter.ParameterType)];
        _invoker = Lazily.Create(() => AccessorFactory.BuildInvoker(constructor));
        _attributes = Lazily.Create(() => new CachedAttributes(constructor.GetCustomAttributes()));
    }

    public ConstructorInfo Constructor { get; }

    public IReadOnlyList<Type> ParameterTypes { get; }

    public CachedAttributes Attributes
        => _attributes.Value;

    public object Invoke(params object?[] arguments)
        => _invoker.Value(arguments);

    public override string ToString()
        => $".ctor({string.Join(", ", ParameterTypes.Select(type => type.Name))})";
}
