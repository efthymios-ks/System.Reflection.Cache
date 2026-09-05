using System.Collections.Concurrent;

namespace System.Reflection.Cache;

/// <summary>
/// A type's reflection metadata, read once and kept. Everything below it is built on first use, so
/// asking for a type costs nothing until you ask it something.
/// </summary>
public sealed class CachedType
{
    /// <summary>
    /// Keyed by <see cref="Type"/> rather than by name: a name is null for a generic parameter and
    /// is not unique across assemblies or load contexts. Reflection metadata never changes, so
    /// entries never expire.
    /// </summary>
    private static readonly ConcurrentDictionary<Type, CachedType> _cache = new();

    private readonly Lazy<CachedMembers<CachedProperty>> _properties;
    private readonly Lazy<CachedMembers<CachedField>> _fields;
    private readonly Lazy<CachedMethods> _methods;
    private readonly Lazy<IReadOnlyList<CachedConstructor>> _constructors;
    private readonly Lazy<CachedAttributes> _attributes;

    private CachedType(Type type, BindingFlags bindingFlags)
    {
        Type = type;
        BindingFlags = bindingFlags;

        _properties = Lazily.Create(() => new CachedMembers<CachedProperty>(
            type.GetProperties(bindingFlags)
                .Where(property => property.GetIndexParameters().Length == 0)
                .Select(property => new CachedProperty(property))
        ));

        _fields = Lazily.Create(() => new CachedMembers<CachedField>(
            type.GetFields(bindingFlags).Select(field => new CachedField(field))
        ));

        _methods = Lazily.Create(() => new CachedMethods(
            type.GetMethods(bindingFlags)
                .Where(method => !method.IsSpecialName)
                .Select(method => new CachedMethod(method))
        ));

        _constructors = Lazily.Create<IReadOnlyList<CachedConstructor>>(() =>
            [.. type.GetConstructors(bindingFlags).Select(constructor => new CachedConstructor(constructor))]
        );

        _attributes = Lazily.Create(() => new CachedAttributes(type.GetCustomAttributes()));
    }

    /// <summary>Public instance and static members, which is what almost every caller wants.</summary>
    public const BindingFlags DefaultBindingFlags =
        BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;

    public Type Type { get; }

    public BindingFlags BindingFlags { get; }

    public string Name
        => Type.Name;

    /// <summary>Falls back to <see cref="Name"/>, which is all a generic parameter has.</summary>
    public string FullName
        => Type.FullName ?? Type.Name;

    public CachedMembers<CachedProperty> Properties
        => _properties.Value;

    public CachedMembers<CachedField> Fields
        => _fields.Value;

    public CachedMethods Methods
        => _methods.Value;

    public IReadOnlyList<CachedConstructor> Constructors
        => _constructors.Value;

    public CachedAttributes Attributes
        => _attributes.Value;

    public static CachedType Of<TTarget>()
        => Of(typeof(TTarget));

    public static CachedType Of(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return _cache.GetOrAdd(type, static key => new CachedType(key, DefaultBindingFlags));
    }

    /// <summary>
    /// A view over other binding flags — private members, for instance. Not cached, because the
    /// flags are part of the identity and caching every combination would grow without bound.
    /// </summary>
    public static CachedType Of(Type type, BindingFlags bindingFlags)
    {
        ArgumentNullException.ThrowIfNull(type);

        return bindingFlags == DefaultBindingFlags
            ? Of(type)
            : new CachedType(type, bindingFlags);
    }

    public object CreateInstance(params object?[] arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        var constructor = Constructors.FirstOrDefault(candidate => Matches(candidate, arguments))
            ?? throw new MissingMethodException(
                $"'{Name}' has no constructor taking {arguments.Length} argument(s) of those types."
            );

        return constructor.Invoke(arguments);
    }

    /// <summary>Empties the cache. Nothing a consumer needs; kept for tests and cold-start timing.</summary>
    internal static void Clear()
        => _cache.Clear();

    public override string ToString()
        => FullName;

    private static bool Matches(CachedConstructor constructor, object?[] arguments)
    {
        if (constructor.ParameterTypes.Count != arguments.Length)
        {
            return false;
        }

        for (var index = 0; index < arguments.Length; index++)
        {
            var parameterType = constructor.ParameterTypes[index];

            // A null argument fits anything that can hold null.
            if (arguments[index] is not { } argument)
            {
                if (parameterType.IsValueType && Nullable.GetUnderlyingType(parameterType) is null)
                {
                    return false;
                }

                continue;
            }

            if (!parameterType.IsInstanceOfType(argument))
            {
                return false;
            }
        }

        return true;
    }
}
