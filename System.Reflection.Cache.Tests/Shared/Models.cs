namespace System.Reflection.Cache.Tests.Shared;

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public sealed class LabelAttribute(string value) : Attribute
{
    public string Value { get; } = value;
}

[AttributeUsage(AttributeTargets.All)]
public sealed class MarkerAttribute : Attribute;

[Label("person")]
public class Person
{
    public const string Species = "human";

    public static int Count;

    public readonly string Origin = "unknown";

    public string Nickname = string.Empty;

    public Person()
    {
    }

    public Person(string name)
        => Name = name;

    public Person(string name, int age)
    {
        Name = name;
        Age = age;
    }

    public static string Kingdom { get; set; } = "animalia";

    [Label("name")]
    [Label("primary")]
    public string Name { get; set; } = string.Empty;

    public int Age { get; set; }

    public string Display
        => $"{Name} ({Age})";

    public string this[int index]
        => Name[index].ToString();

    private string Secret { get; set; } = "hidden";

    [Marker]
    public string Greet()
        => $"Hello {Name}";

    public string Greet(string greeting)
        => $"{greeting} {Name}";

    public static int Add(int left, int right)
        => left + right;

    public void Reset()
    {
        Name = string.Empty;
        Age = 0;
    }

    public static string Describe(string prefix)
        => $"{prefix}:{Kingdom}";

    private string Whisper()
        => Secret;
}

public sealed class Employee : Person
{
    public string Department { get; set; } = string.Empty;
}

public struct Point(int x, int y)
{
    public int X { get; set; } = x;

    public int Y { get; set; } = y;

    public readonly int Sum()
        => X + Y;
}

public sealed class NoDefaultConstructor(int value)
{
    public int Value { get; } = value;
}

public sealed class Nullables
{
    public string? Text { get; set; }

    public int? Number { get; set; }
}
