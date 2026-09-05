# System.Reflection.Cache

Reflection metadata read once and kept, with compiled accessors instead of `MemberInfo.Invoke`. A
demo, not a package — clone it and copy what is useful.

```
CachedType.cs         Of<T>() / Of(Type), Properties, Fields, Methods, Constructors, Attributes
CachedProperty.cs     GetValue / SetValue
CachedField.cs        GetValue / SetValue
CachedMethod.cs       Invoke
CachedConstructor.cs  Invoke
CachedMembers.cs      by-name lookup over a member list
CachedAttributes.cs   Get / GetAll / Has
Accessors/            compiled getters, setters and invokers
```

## Read and write

```csharp
var type = CachedType.Of<Person>();

var name = type.Properties["Name"]!.GetValue(person);
type.Properties["Age"]!.SetValue(person, 41);

var nickname = type.Fields["Nickname"]!.GetValue(person);
```

Lookup by name returns null rather than throwing, so a miss needs no try/catch. Accessors are
compiled on first use and reused, so repeated access costs no reflection.

| Member | Reports |
| --- | --- |
| `CanRead` | false with no getter |
| `CanWrite` | false for get-only, readonly, const, and any instance member of a struct |
| `IsStatic` | pass `null` as the instance |

Writing through a boxed struct would update the box and throw the change away, so it is refused
rather than silently doing nothing.

## Call

```csharp
var greet = type.Methods.Find("Greet", typeof(string))!;

var greeting = greet.Invoke(person, "Hi");   // "Hi Ann"
var sum = type.Methods.Find("Add")!.Invoke(person, 3, 4);   // 7
```

| Call | Returns |
| --- | --- |
| `Methods["Greet"]` | every overload of that name, empty when none |
| `Methods.Find("Add")` | the only method of that name; throws when overloaded |
| `Methods.Find("Greet", typeof(string))` | the overload with those parameter types |
| `Invoke` on a void method | null |

Overloads share a name, so a name maps to a list. Keying them one-to-one would silently drop all
but the last.

## Construct

```csharp
var person = (Person)type.CreateInstance("Ann", 30);
```

The constructor is picked by argument count and runtime type; a null argument matches any parameter
that can hold null. `MissingMethodException` when none fits.

## Attributes

```csharp
var label = type.Attributes.Get<LabelAttribute>();
var labels = type.Properties["Name"]!.Attributes.GetAll<LabelAttribute>();

if (type.Attributes.Has<ObsoleteAttribute>()) { }
```

## Binding flags

`Of<T>()` caches public instance and static members. Other flags return an uncached view, since the
flags are part of the identity and caching every combination would grow without bound.

```csharp
var withPrivates = CachedType.Of(typeof(Person), BindingFlags.NonPublic | BindingFlags.Instance);
```

Types are keyed by `Type`, not by name — a name is null for a generic parameter and is not unique
across assemblies. Entries never expire, because reflection metadata never changes.
