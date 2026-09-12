using System.Reflection.Cache.Tests.Shared;
using Xunit;

namespace System.Reflection.Cache.Tests;

public class CachedTypeTests
{
    [Fact]
    public void Of_WhenTheTypeIsNull_ShouldThrowArgumentNull()
        => Assert.Throws<ArgumentNullException>(() => CachedType.Of(null!));

    [Fact]
    public void Of_WhenCalledTwice_ShouldReturnTheSameInstance()
        => Assert.Same(CachedType.Of<Person>(), CachedType.Of<Person>());

    [Fact]
    public void Of_WhenCalledByTypeAndByGeneric_ShouldReturnTheSameInstance()
        => Assert.Same(CachedType.Of<Person>(), CachedType.Of(typeof(Person)));

    [Fact]
    public void Of_WhenGivenDifferentTypes_ShouldReturnDifferentInstances()
        => Assert.NotSame(CachedType.Of<Person>(), CachedType.Of<Employee>());

    [Fact]
    public void Of_WhenGivenNonDefaultBindingFlags_ShouldNotReuseTheCachedEntry()
    {
        // Arrange & Act
        var withDefaults = CachedType.Of<Person>();
        var withPrivates = CachedType.Of(typeof(Person), BindingFlags.NonPublic | BindingFlags.Instance);

        // Assert
        Assert.NotSame(withDefaults, withPrivates);
    }

    [Fact]
    public void Of_WhenGivenTheDefaultBindingFlagsExplicitly_ShouldReuseTheCachedEntry()
        => Assert.Same(
            CachedType.Of<Person>(),
            CachedType.Of(typeof(Person), CachedType.DefaultBindingFlags)
        );

    [Fact]
    public void Of_WhenUsedConcurrently_ShouldBuildOneInstance()
    {
        // Arrange
        CachedType.Clear();

        // Act
        var results = Enumerable
            .Range(0, 32)
            .AsParallel()
            .Select(_ => CachedType.Of<Employee>())
            .ToArray();

        // Assert
        Assert.Single(results.Distinct());
    }

    [Fact]
    public void Name_ShouldBeTheShortTypeName()
        => Assert.Equal(nameof(Person), CachedType.Of<Person>().Name);

    [Fact]
    public void FullName_ShouldIncludeTheNamespace()
        => Assert.Equal(typeof(Person).FullName, CachedType.Of<Person>().FullName);

    [Fact]
    public void FullName_WhenTheTypeHasNone_ShouldFallBackToTheName()
    {
        // Arrange & Act
        var genericParameter = typeof(List<>).GetGenericArguments()[0];

        // Assert
        Assert.Equal(genericParameter.Name, CachedType.Of(genericParameter).FullName);
    }

    [Fact]
    public void Type_ShouldExposeTheUnderlyingType()
        => Assert.Equal(typeof(Person), CachedType.Of<Person>().Type);

    [Fact]
    public void ToString_ShouldBeTheFullName()
        => Assert.Equal(typeof(Person).FullName, CachedType.Of<Person>().ToString());

    [Fact]
    public void Attributes_ShouldExposeTheTypeAttributes()
    {
        // Arrange & Act
        var label = CachedType.Of<Person>().Attributes.Get<LabelAttribute>();

        // Assert
        Assert.Equal("person", label?.Value);
    }

    [Fact]
    public void Attributes_WhenTheAttributeIsAbsent_ShouldReturnNull()
        => Assert.Null(CachedType.Of<Person>().Attributes.Get<MarkerAttribute>());

    [Fact]
    public void Attributes_WhenAskedWhetherOneExists_ShouldAnswerWithoutMaterialising()
    {
        // Arrange & Act
        var attributes = CachedType.Of<Person>().Attributes;

        // Assert
        Assert.True(attributes.Has<LabelAttribute>());
        Assert.False(attributes.Has<MarkerAttribute>());
    }

    [Fact]
    public void Attributes_WhenAnAttributeRepeats_ShouldReturnEveryOne()
    {
        // Arrange & Act
        var name = CachedType.Of<Person>().Properties[nameof(Person.Name)]!;

        // Assert
        Assert.Equal(2, name.Attributes.GetAll<LabelAttribute>().Count);
    }

    [Fact]
    public void Attributes_ShouldBeEnumerable()
        => Assert.NotEmpty(CachedType.Of<Person>().Attributes);

    [Fact]
    public void Clear_WhenCalled_ShouldForceTheNextLookupToRebuild()
    {
        // Arrange & Act
        var before = CachedType.Of<Nullables>();
        CachedType.Clear();

        // Assert
        Assert.NotSame(before, CachedType.Of<Nullables>());
    }

    [Fact]
    public void CreateInstance_WhenGivenNoArguments_ShouldUseTheParameterlessConstructor()
    {
        // Act & Assert
        var person = Assert.IsType<Person>(CachedType.Of<Person>().CreateInstance());

        Assert.Equal(string.Empty, person.Name);
    }

    [Fact]
    public void CreateInstance_WhenGivenArguments_ShouldPickTheMatchingConstructor()
    {
        // Act & Assert
        var person = Assert.IsType<Person>(CachedType.Of<Person>().CreateInstance("Ann", 30));

        Assert.Equal("Ann", person.Name);
        Assert.Equal(30, person.Age);
    }

    [Fact]
    public void CreateInstance_WhenAnArgumentIsNull_ShouldStillMatchAReferenceParameter()
    {
        // Act & Assert
        var person = Assert.IsType<Person>(CachedType.Of<Person>().CreateInstance([null]));

        Assert.Null(person.Name);
    }

    [Fact]
    public void CreateInstance_WhenNoConstructorMatches_ShouldThrowMissingMethod()
        => Assert.Throws<MissingMethodException>(() => CachedType.Of<Person>().CreateInstance(1, 2, 3));

    [Fact]
    public void CreateInstance_WhenANullIsGivenForAValueTypeParameter_ShouldNotMatch()
        => Assert.Throws<MissingMethodException>(() =>
            CachedType.Of<NoDefaultConstructor>().CreateInstance([null]));

    [Fact]
    public void CreateInstance_WhenTheArgumentsAreNull_ShouldThrowArgumentNull()
        => Assert.Throws<ArgumentNullException>(() => CachedType.Of<Person>().CreateInstance(null!));

    [Fact]
    public void Constructors_ShouldExposeEveryPublicConstructor()
        => Assert.Equal(3, CachedType.Of<Person>().Constructors.Count);

    [Fact]
    public void Constructors_ShouldReportTheirParameterTypes()
    {
        // Arrange & Act
        var constructor = CachedType.Of<Person>().Constructors
            .Single(candidate => candidate.ParameterTypes.Count == 2);

        // Assert
        Assert.Equal([typeof(string), typeof(int)], constructor.ParameterTypes);
    }

    [Fact]
    public void Constructor_Invoke_ShouldBuildTheInstance()
    {
        // Arrange & Act
        var constructor = CachedType.Of<Person>().Constructors
            .Single(candidate => candidate.ParameterTypes.Count == 1);

        // Assert
        var person = Assert.IsType<Person>(constructor.Invoke("Bob"));

        Assert.Equal("Bob", person.Name);
    }

    [Fact]
    public void Constructor_ToString_ShouldListTheParameterTypes()
    {
        // Arrange & Act
        var constructor = CachedType.Of<Person>().Constructors
            .Single(candidate => candidate.ParameterTypes.Count == 2);

        // Assert
        Assert.Equal(".ctor(String, Int32)", constructor.ToString());
    }
}
