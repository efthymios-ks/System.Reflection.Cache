using System.Reflection.Cache.Tests.Shared;
using Xunit;

namespace System.Reflection.Cache.Tests;

public class CachedFieldTests
{
    private static readonly CachedType _type = CachedType.Of<Person>();

    [Fact]
    public void Fields_ShouldExposeThePublicFields()
        => Assert.True(_type.Fields.Contains(nameof(Person.Nickname)));

    [Fact]
    public void Fields_WhenTheNameIsUnknown_ShouldReturnNull()
        => Assert.Null(_type.Fields["NotAField"]);

    [Fact]
    public void FieldType_ShouldBeTheDeclaredType()
        => Assert.Equal(typeof(string), _type.Fields[nameof(Person.Nickname)]!.FieldType);

    [Fact]
    public void GetValue_ShouldReadFromTheInstance()
    {
        // Arrange & Act
        var person = new Person { Nickname = "Annie" };

        // Assert
        Assert.Equal("Annie", _type.Fields[nameof(Person.Nickname)]!.GetValue(person));
    }

    [Fact]
    public void SetValue_ShouldWriteToTheInstance()
    {
        // Arrange
        var person = new Person();

        // Act
        _type.Fields[nameof(Person.Nickname)]!.SetValue(person, "Bobby");

        // Assert
        Assert.Equal("Bobby", person.Nickname);
    }

    [Fact]
    public void GetValue_WhenTheFieldIsReadonly_ShouldStillRead()
        => Assert.Equal("unknown", _type.Fields[nameof(Person.Origin)]!.GetValue(new Person()));

    [Fact]
    public void CanWrite_WhenTheFieldIsReadonly_ShouldBeFalse()
    {
        // Arrange & Act
        var field = _type.Fields[nameof(Person.Origin)]!;

        // Assert
        Assert.False(field.CanWrite);
        Assert.Throws<InvalidOperationException>(() => field.SetValue(new Person(), "x"));
    }

    [Fact]
    public void CanWrite_WhenTheFieldIsConst_ShouldBeFalse()
        => Assert.False(_type.Fields[nameof(Person.Species)]!.CanWrite);

    [Fact]
    public void GetValue_WhenTheFieldIsConst_ShouldReadTheLiteral()
        => Assert.Equal("human", _type.Fields[nameof(Person.Species)]!.GetValue(null));

    [Fact]
    public void GetValue_WhenTheFieldIsStatic_ShouldReadWithoutAnInstance()
    {
        // Arrange
        Person.Count = 7;

        Assert.Equal(7, _type.Fields[nameof(Person.Count)]!.GetValue(null));

        // Act
        Person.Count = 0;
    }

    [Fact]
    public void SetValue_WhenTheFieldIsStatic_ShouldWriteWithoutAnInstance()
    {
        // Arrange
        _type.Fields[nameof(Person.Count)]!.SetValue(null, 9);

        Assert.Equal(9, Person.Count);

        // Act
        Person.Count = 0;
    }

    [Fact]
    public void IsStatic_ShouldTellStaticAndInstanceFieldsApart()
    {
        // Act & Assert
        Assert.True(_type.Fields[nameof(Person.Count)]!.IsStatic);
        Assert.False(_type.Fields[nameof(Person.Nickname)]!.IsStatic);
    }

    [Fact]
    public void ToString_ShouldShowTheTypeAndName()
        => Assert.Equal("String Nickname", _type.Fields[nameof(Person.Nickname)]!.ToString());

    [Fact]
    public void Field_ShouldExposeTheUnderlyingFieldInfo()
        => Assert.Equal(
            typeof(Person).GetField(nameof(Person.Nickname)),
            _type.Fields[nameof(Person.Nickname)]!.Field
        );
}

public class CachedMethodTests
{
    private static readonly CachedType _type = CachedType.Of<Person>();

    [Fact]
    public void Methods_ShouldExposeThePublicMethods()
        => Assert.True(_type.Methods.Contains(nameof(Person.Add)));

    [Fact]
    public void Methods_ShouldNotExposePropertyAccessors()
        => Assert.DoesNotContain(_type.Methods, method => method.Name.StartsWith("get_", StringComparison.Ordinal));

    [Fact]
    public void Methods_WhenTheNameIsUnknown_ShouldReturnAnEmptyList()
        => Assert.Empty(_type.Methods["NotAMethod"]);

    [Fact]
    public void Methods_WhenTheNameIsOverloaded_ShouldReturnEveryOverload()
        => Assert.Equal(2, _type.Methods[nameof(Person.Greet)].Count);

    [Fact]
    public void Find_WhenTheNameIsUnique_ShouldReturnThatMethod()
        => Assert.Equal(nameof(Person.Add), _type.Methods.Find(nameof(Person.Add))!.Name);

    [Fact]
    public void Find_WhenTheNameIsUnknown_ShouldReturnNull()
        => Assert.Null(_type.Methods.Find("NotAMethod"));

    [Fact]
    public void Find_WhenTheNameIsOverloaded_ShouldThrowAmbiguousMatch()
        => Assert.Throws<AmbiguousMatchException>(() => _type.Methods.Find(nameof(Person.Greet)));

    [Fact]
    public void Find_WhenGivenParameterTypes_ShouldPickThatOverload()
    {
        // Arrange & Act
        var method = _type.Methods.Find(nameof(Person.Greet), typeof(string));

        // Assert
        Assert.Equal([typeof(string)], method!.ParameterTypes);
    }

    [Fact]
    public void Find_WhenNoOverloadTakesThoseTypes_ShouldReturnNull()
        => Assert.Null(_type.Methods.Find(nameof(Person.Greet), typeof(int)));

    [Fact]
    public void Invoke_WhenTheMethodTakesNoArguments_ShouldReturnItsResult()
    {
        // Arrange & Act
        var person = new Person { Name = "Ann" };
        var method = _type.Methods.Find(nameof(Person.Greet), [])!;

        // Assert
        Assert.Equal("Hello Ann", method.Invoke(person));
    }

    [Fact]
    public void Invoke_WhenTheMethodTakesArguments_ShouldPassThem()
    {
        // Arrange & Act
        var person = new Person { Name = "Ann" };
        var method = _type.Methods.Find(nameof(Person.Greet), typeof(string))!;

        // Assert
        Assert.Equal("Hi Ann", method.Invoke(person, "Hi"));
    }

    [Fact]
    public void Invoke_WhenTheMethodTakesValueTypes_ShouldReturnTheBoxedResult()
        => Assert.Equal(7, _type.Methods.Find(nameof(Person.Add))!.Invoke(null, 3, 4));

    [Fact]
    public void Invoke_WhenTheMethodReturnsVoid_ShouldReturnNull()
    {
        // Arrange & Act
        var person = new Person { Name = "Ann", Age = 30 };

        // Assert
        Assert.Null(_type.Methods.Find(nameof(Person.Reset))!.Invoke(person));
        Assert.Equal(string.Empty, person.Name);
    }

    [Fact]
    public void Invoke_WhenTheMethodIsStatic_ShouldRunWithoutAnInstance()
    {
        // Arrange & Act
        Person.Kingdom = "animalia";

        // Assert
        Assert.Equal("a:animalia", _type.Methods.Find(nameof(Person.Describe))!.Invoke(null, "a"));
    }

    [Fact]
    public void Invoke_WhenTheDeclaringTypeIsAStruct_ShouldStillRun()
        => Assert.Equal(5, CachedType.Of<Point>().Methods.Find(nameof(Point.Sum))!.Invoke(new Point(2, 3)));

    [Fact]
    public void Invoke_WhenTheMethodIsPrivate_ShouldRunUnderTheRightBindingFlags()
    {
        // Arrange & Act
        var withPrivates = CachedType.Of(typeof(Person), BindingFlags.NonPublic | BindingFlags.Instance);
        var method = withPrivates.Methods.Find("Whisper")!;

        // Assert
        Assert.Equal("hidden", method.Invoke(new Person()));
    }

    [Fact]
    public void ReturnType_ShouldBeTheDeclaredReturnType()
        => Assert.Equal(typeof(int), _type.Methods.Find(nameof(Person.Add))!.ReturnType);

    [Fact]
    public void IsStatic_ShouldTellStaticAndInstanceMethodsApart()
    {
        // Act & Assert
        Assert.True(_type.Methods.Find(nameof(Person.Describe))!.IsStatic);
        Assert.True(_type.Methods.Find(nameof(Person.Add))!.IsStatic);
        Assert.False(_type.Methods.Find(nameof(Person.Reset))!.IsStatic);
    }

    [Fact]
    public void Attributes_ShouldExposeTheMethodAttributes()
        => Assert.True(_type.Methods.Find(nameof(Person.Greet), [])!.Attributes.Has<MarkerAttribute>());

    [Fact]
    public void ToString_ShouldShowTheSignature()
        => Assert.Equal("Int32 Add(Int32, Int32)", _type.Methods.Find(nameof(Person.Add))!.ToString());

    [Fact]
    public void Method_ShouldExposeTheUnderlyingMethodInfo()
        => Assert.Equal(
            typeof(Person).GetMethod(nameof(Person.Add)),
            _type.Methods.Find(nameof(Person.Add))!.Method
        );

    [Fact]
    public void Methods_ShouldBeEnumerableAndIndexable()
    {
        // Act & Assert
        Assert.NotEmpty(_type.Methods);
        Assert.NotNull(_type.Methods[0]);
    }
}
