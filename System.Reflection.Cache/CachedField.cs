using System.Reflection.Cache.Accessors;

namespace System.Reflection.Cache;

public sealed class CachedField : ICachedMember
{
    private readonly Lazy<Func<object?, object?>> _getter;
    private readonly Lazy<Action<object?, object?>?> _setter;
    private readonly Lazy<CachedAttributes> _attributes;

    internal CachedField(FieldInfo field)
    {
        Field = field;
        _getter = Lazily.Create(() => AccessorFactory.BuildGetter(field));
        _setter = Lazily.Create(() => AccessorFactory.BuildSetter(field));
        _attributes = Lazily.Create(() => new CachedAttributes(field.GetCustomAttributes()));
    }

    public FieldInfo Field { get; }

    public string Name
        => Field.Name;

    public Type FieldType
        => Field.FieldType;

    public bool IsStatic
        => Field.IsStatic;

    /// <summary>False for a readonly or const field, and for any instance field of a struct.</summary>
    public bool CanWrite
        => _setter.Value is not null;

    public CachedAttributes Attributes
        => _attributes.Value;

    public object? GetValue(object? instance)
        => _getter.Value(instance);

    public void SetValue(object? instance, object? value)
        => (_setter.Value ?? throw NotWritable()).Invoke(instance, value);

    public override string ToString()
        => $"{FieldType.Name} {Name}";

    private InvalidOperationException NotWritable()
        => new($"Field '{Field.DeclaringType?.Name}.{Name}' is read-only.");
}
