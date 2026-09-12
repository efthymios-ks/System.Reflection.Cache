namespace System.Reflection.Cache;

internal static class Lazily
{
    /// <summary>
    /// A cached type is shared by every caller, so its lazily built parts have to be safe to touch
    /// from several threads at once.
    /// </summary>
    public static Lazy<TTarget> Create<TTarget>(Func<TTarget> factory)
        => new(factory, LazyThreadSafetyMode.ExecutionAndPublication);
}
