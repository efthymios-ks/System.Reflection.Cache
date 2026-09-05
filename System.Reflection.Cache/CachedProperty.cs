using System.Reflection.Cache.Accessors;

namespace System.Reflection.Cache;

public sealed class CachedProperty : ICachedMember
{
    private readonly Lazy<Func<object?, object?>?> _getter;
    private readonly Lazy<Action<object?, object?>?> _setter;
    private readonly Lazy<CachedAttributes> _attributes;

    internal CachedProperty(PropertyInfo property)
    {
        Property = property;
        _getter = Lazily.Create(() => AccessorFactory.BuildGetter(property));
        _setter = Lazily.Create(() => AccessorFactory.BuildSetter(property));
        _attributes = Lazily.Create(() => new CachedAttributes(property.GetCustomAttributes()));
    }

    public PropertyInfo Property { get; }

    public string Name
        => Property.Name;

    public Type PropertyType
        => Property.PropertyType;

    public bool IsStatic
        => Property.GetAccessors(nonPublic: true)[0].IsStatic;

    public bool CanRead
        => _getter.Value is not null;

    /// <summary>False for a read-only property, and for any instance property of a struct.</summary>
    public bool CanWrite
        => _setter.Value is not null;

    public CachedAttributes Attributes
        => _attributes.Value;

    public object? GetValue(object? instance)
        => (_getter.Value ?? throw NotReadable()).Invoke(instance);

    public void SetValue(object? instance, object? value)
        => (_setter.Value ?? throw NotWritable()).Invoke(instance, value);

    public override string ToString()
        => $"{PropertyType.Name} {Name}";

    private InvalidOperationException NotReadable()
        => new($"Property '{Property.DeclaringType?.Name}.{Name}' has no getter.");

    private InvalidOperationException NotWritable()
        => new($"Property '{Property.DeclaringType?.Name}.{Name}' cannot be written through a boxed instance.");
}
