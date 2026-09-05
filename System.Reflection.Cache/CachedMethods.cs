using System.Collections;

namespace System.Reflection.Cache;

/// <summary>
/// Methods keyed by name. Overloads share a name, so a name maps to a list rather than to one
/// method — keying them one-to-one would silently drop all but the last.
/// </summary>
public sealed class CachedMethods : IReadOnlyList<CachedMethod>
{
    private static readonly CachedMethod[] _none = [];

    private readonly CachedMethod[] _methods;
    private readonly Dictionary<string, CachedMethod[]> _byName;

    internal CachedMethods(IEnumerable<CachedMethod> methods)
    {
        _methods = [.. methods];
        _byName = _methods
            .GroupBy(method => method.Name, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
    }

    public int Count
        => _methods.Length;

    public CachedMethod this[int index]
        => _methods[index];

    /// <summary>Every overload of that name, empty when there is none.</summary>
    public IReadOnlyList<CachedMethod> this[string name]
        => _byName.GetValueOrDefault(name, _none);

    public bool Contains(string name)
        => _byName.ContainsKey(name);

    /// <summary>The only method of that name, or null. Throws when the name is overloaded.</summary>
    public CachedMethod? Find(string name)
    {
        var overloads = this[name];

        return overloads.Count switch
        {
            0 => null,
            1 => overloads[0],
            _ => throw new AmbiguousMatchException(
                $"'{name}' has {overloads.Count} overloads; pass the parameter types to pick one."
            )
        };
    }

    public CachedMethod? Find(string name, params Type[] parameterTypes)
        => this[name].FirstOrDefault(method => method.ParameterTypes.SequenceEqual(parameterTypes));

    public IEnumerator<CachedMethod> GetEnumerator()
        => ((IEnumerable<CachedMethod>)_methods).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => _methods.GetEnumerator();
}
