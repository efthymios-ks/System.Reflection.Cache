using System.Collections;

namespace System.Reflection.Cache;

/// <summary>A member list that can also be looked up by name, without a dictionary at every call site.</summary>
public sealed class CachedMembers<TMember> : IReadOnlyList<TMember>
    where TMember : ICachedMember
{
    private readonly TMember[] _members;
    private readonly Dictionary<string, TMember> _byName;

    internal CachedMembers(IEnumerable<TMember> members)
    {
        _members = [.. members];
        _byName = new Dictionary<string, TMember>(_members.Length, StringComparer.Ordinal);

        foreach (var member in _members)
        {
            // A name shadowed by a derived type appears twice; the most derived one wins.
            _byName[member.Name] = member;
        }
    }

    public int Count
        => _members.Length;

    public TMember this[int index]
        => _members[index];

    /// <summary>Null when there is no such member, so a lookup needs no try/catch.</summary>
    public TMember? this[string name]
        => _byName.GetValueOrDefault(name);

    public bool Contains(string name)
        => _byName.ContainsKey(name);

    public IEnumerator<TMember> GetEnumerator()
        => ((IEnumerable<TMember>)_members).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => _members.GetEnumerator();
}
