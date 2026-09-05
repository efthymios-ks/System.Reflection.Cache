using System.Collections;

namespace System.Reflection.Cache;

/// <summary>The attributes on a type or member, materialised once.</summary>
public sealed class CachedAttributes : IReadOnlyList<Attribute>
{
    private readonly Attribute[] _attributes;

    internal CachedAttributes(IEnumerable<Attribute> attributes)
        => _attributes = [.. attributes];

    public int Count
        => _attributes.Length;

    public Attribute this[int index]
        => _attributes[index];

    public TAttribute? Get<TAttribute>() where TAttribute : Attribute
        => _attributes.OfType<TAttribute>().FirstOrDefault();

    public IReadOnlyList<TAttribute> GetAll<TAttribute>() where TAttribute : Attribute
        => [.. _attributes.OfType<TAttribute>()];

    public bool Has<TAttribute>() where TAttribute : Attribute
        => _attributes.OfType<TAttribute>().Any();

    public IEnumerator<Attribute> GetEnumerator()
        => ((IEnumerable<Attribute>)_attributes).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => _attributes.GetEnumerator();
}
