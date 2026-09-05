using System.Reflection.Cache.Tests.Shared;
using Xunit;

namespace System.Reflection.Cache.Tests;

public class CachedPropertyTests
{
    private static readonly CachedType _type = CachedType.Of<Person>();

    [Fact]
    public void Properties_ShouldExposeThePublicProperties()
    {
        // Act & Assert
        Assert.True(_type.Properties.Contains(nameof(Person.Name)));
        Assert.True(_type.Properties.Contains(nameof(Person.Age)));
    }

    [Fact]
    public void Properties_WhenThePropertyIsPrivate_ShouldNotExposeItByDefault()
        => Assert.False(_type.Properties.Contains("Secret"));

    [Fact]
    public void Properties_WhenTheBindingFlagsIncludePrivates_ShouldExposeThem()
    {
        // Arrange & Act
        var withPrivates = CachedType.Of(typeof(Person), BindingFlags.NonPublic | BindingFlags.Instance);

        // Assert
        Assert.True(withPrivates.Properties.Contains("Secret"));
    }

    [Fact]
    public void Properties_WhenThePropertyIsAnIndexer_ShouldSkipIt()
        => Assert.DoesNotContain(_type.Properties, property => property.Property.GetIndexParameters().Length > 0);

    [Fact]
    public void Properties_WhenTheNameIsUnknown_ShouldReturnNull()
        => Assert.Null(_type.Properties["NotAProperty"]);

    [Fact]
    public void Properties_ShouldBeEnumerableAndIndexable()
    {
        // Act & Assert
        Assert.NotEmpty(_type.Properties);
        Assert.NotNull(_type.Properties[0]);
    }

    [Fact]
    public void Properties_WhenTheTypeInherits_ShouldIncludeTheBaseProperties()
    {
        // Arrange & Act
        var employee = CachedType.Of<Employee>();

        // Assert
        Assert.True(employee.Properties.Contains(nameof(Employee.Department)));
        Assert.True(employee.Properties.Contains(nameof(Person.Name)));
    }

    [Fact]
    public void PropertyType_ShouldBeTheDeclaredType()
        => Assert.Equal(typeof(int), _type.Properties[nameof(Person.Age)]!.PropertyType);

    [Fact]
    public void GetValue_ShouldReadFromTheInstance()
    {
        // Arrange & Act
        var person = new Person { Name = "Ann", Age = 30 };

        // Assert
        Assert.Equal("Ann", _type.Properties[nameof(Person.Name)]!.GetValue(person));
        Assert.Equal(30, _type.Properties[nameof(Person.Age)]!.GetValue(person));
    }

    [Fact]
    public void GetValue_WhenThePropertyIsNull_ShouldReturnNull()
        => Assert.Null(CachedType.Of<Nullables>().Properties[nameof(Nullables.Text)]!.GetValue(new Nullables()));

    [Fact]
    public void SetValue_ShouldWriteToTheInstance()
    {
        // Arrange
        var person = new Person();

        // Act
        _type.Properties[nameof(Person.Name)]!.SetValue(person, "Bob");
        _type.Properties[nameof(Person.Age)]!.SetValue(person, 41);

        // Assert
        Assert.Equal("Bob", person.Name);
        Assert.Equal(41, person.Age);
    }

    [Fact]
    public void SetValue_WhenThePropertyIsNullable_ShouldAcceptNull()
    {
        // Arrange
        var target = new Nullables { Number = 1 };

        // Act
        CachedType.Of<Nullables>().Properties[nameof(Nullables.Number)]!.SetValue(target, null);

        // Assert
        Assert.Null(target.Number);
    }

    [Fact]
    public void CanRead_WhenThePropertyHasAGetter_ShouldBeTrue()
        => Assert.True(_type.Properties[nameof(Person.Display)]!.CanRead);

    [Fact]
    public void CanWrite_WhenThePropertyIsGetOnly_ShouldBeFalse()
        => Assert.False(_type.Properties[nameof(Person.Display)]!.CanWrite);

    [Fact]
    public void SetValue_WhenThePropertyIsGetOnly_ShouldThrowInvalidOperation()
    {
        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            _type.Properties[nameof(Person.Display)]!.SetValue(new Person(), "x"));

        Assert.Contains(nameof(Person.Display), exception.Message);
    }

    [Fact]
    public void GetValue_WhenThePropertyIsStatic_ShouldReadWithoutAnInstance()
    {
        // Arrange & Act
        Person.Kingdom = "animalia";

        // Assert
        Assert.Equal("animalia", _type.Properties[nameof(Person.Kingdom)]!.GetValue(null));
    }

    [Fact]
    public void SetValue_WhenThePropertyIsStatic_ShouldWriteWithoutAnInstance()
    {
        // Arrange
        _type.Properties[nameof(Person.Kingdom)]!.SetValue(null, "plantae");

        Assert.Equal("plantae", Person.Kingdom);

        // Act
        Person.Kingdom = "animalia";
    }

    [Fact]
    public void IsStatic_ShouldTellStaticAndInstancePropertiesApart()
    {
        // Act & Assert
        Assert.True(_type.Properties[nameof(Person.Kingdom)]!.IsStatic);
        Assert.False(_type.Properties[nameof(Person.Name)]!.IsStatic);
    }

    [Fact]
    public void GetValue_WhenTheDeclaringTypeIsAStruct_ShouldStillRead()
    {
        // Arrange & Act
        var point = new Point(2, 3);

        // Assert
        Assert.Equal(2, CachedType.Of<Point>().Properties[nameof(Point.X)]!.GetValue(point));
    }

    [Fact]
    public void CanWrite_WhenTheDeclaringTypeIsAStruct_ShouldBeFalse()
    {
        // Arrange & Act
        // Writing through a boxed struct would update the box and throw the change away.
        var property = CachedType.Of<Point>().Properties[nameof(Point.X)]!;

        // Assert
        Assert.False(property.CanWrite);
        Assert.Throws<InvalidOperationException>(() => property.SetValue(new Point(1, 1), 9));
    }

    [Fact]
    public void Attributes_ShouldExposeThePropertyAttributes()
    {
        // Arrange & Act
        var labels = _type.Properties[nameof(Person.Name)]!.Attributes.GetAll<LabelAttribute>();

        // Assert
        Assert.Equal(["name", "primary"], labels.Select(label => label.Value).Order());
    }

    [Fact]
    public void ToString_ShouldShowTheTypeAndName()
        => Assert.Equal("Int32 Age", _type.Properties[nameof(Person.Age)]!.ToString());

    [Fact]
    public void Property_ShouldExposeTheUnderlyingPropertyInfo()
        => Assert.Equal(
            typeof(Person).GetProperty(nameof(Person.Name)),
            _type.Properties[nameof(Person.Name)]!.Property
        );
}
