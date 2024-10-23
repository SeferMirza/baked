# Coding Style

Add this feature using `AddCodingStyles()` extension;

```csharp
app.Features.AddCodingStyles([...]);
```

## Add/Remove Child

Configures routes for methods in `AddChild` and `RemoveChild(Child)` signature
to have a resource route `POST /../children` and `DELETE /../children/{childId}`
respectively.

```csharp
c => c.AddRemoveChild()
```

## Command Pattern

Configures public methods of transient services as api methods. This coding
style allows you to have a public initializer (`With`) with parameters which
will render as query parameters. It also removes `Execute` and `Process` names
from route.

```csharp
c => c.CommandPattern()
```

## Entity Extension via Composition

Allows classes to extend entities via composition. This marks a transient class
as an entity extension when it implements implicit casting to an entity. Methods
of these extension classes are rendered under entity group.

```csharp
c => c.EntityExtensionViaComposition()
```

## Entity Subclass via Composition

Allows classes to be subclasses of entities via composition. This marks a
transient class as an entity subclass when it implements explicit casting to an
entity. Methods of these extension classes are rendered under entity group. It
uses the first unique property to discriminate entity records.

> [!WARNING]
>
> First unique property is expected to be `enum` or `string`. Otherwise subclass
> routing won't work.

```csharp
c => c.EntitySubclassViaComposition()
```

## Object as JSON

Configures all `object` parameters, return types and properties to be treated as
`JSON` content.

```csharp
c => c.ObjectAsJson()
```

## Records are DTOs

Configures domain type records as valid input paramters. Methods containing
record parameters render as api endpoints.

```csharp
c => c.RecordsAreDtos()
```

## Remaining Services are Singleton

Adds `SingletonAttribute` to the services that has no `TransientAttribute` or
`ScopedAttribute`.

```csharp
c => c.RemainingServicesAreSingleton()
```

## Rich Entity

Adds `QueryAttribute` to classes that inject `IQueryContext<TEntity>`. Using
generic argument of `IQueryContext<TEntity>` finds corresponding entity class
and add `EntityAttribute` to it.

Configures `NHibernate` to initialize entities using dependency injection, making
them rich entities.

Configures routes and swagger docs to use entity methods as resource actions.

```csharp
c => c.RichEntity()
```

## Rich Transient

Adds `RichTransientAttribute` to classes that are `Transient` having
a public `With` method with value type parameter named `Id`

Configures routes and swagger docs to use entity methods as resource actions.

```csharp
c => c.RichTransient()
```

## Scoped by Suffix

Adds `ScopedAttribute` to the services that has name with any of the given
suffixes.

```csharp
c => c.ScopedBySuffix(suffixes: ["Context", "Scope"])
```

> [!NOTE]
>
> Default suffix is `Context`.

## Single by Unique

Scans query classes to have methods that conforms to `SingleBy[Property]` naming
convetion. Treats such methods as single by unique methods and adds that
property to entity id route parameter so that entites can be found through
unique properties as well as their ids. For instance, if entity has
`SingleByName` then its id route parameter is updated from `{id}` to
`{idOrName}`.

```csharp
c => c.SingleByUnique()
```

## `Uri` Return is Redirect

Adds redirect support to your api endpoints. It configures an endpoint to use
redirect result when its corresponding method returns `Uri`. Combined with
`CommandPattern`, it allows you to create callback `GET` endpoints when method
doesn't have any parameters. For actions that have parameters, it configures its
corresponding endpoint to accept form instead of a `json` body.

```csharp
c => c.UriReturnIsRedirect()
```

## Use Built-in Types

Configures built-in .NET types to be used as entity properties and service
parameters. Uses `IParsable` interface to configure primitives. Additionally
configures `string`, enums, `Uri` and `IEnumerable<>` types.

It also allows for string properties to use `TEXT` column type instead of
`VARCHAR` by suffixes.

```csharp
c => c.UseBuiltInTypes(textPropertySuffixes: ["Data", "Description"])
```

> [!TIP]
>
> Default text property suffix is `Data`.

## Use Nullable Types

Adds support for nullable value and reference types. Configures api model to
forbid sending null or empty values to not-null parameters.

```csharp
c => c.UseNullableTypes()
```

## With Method

Adds `TransientAttribute` to the services that has a `With` method. This coding
style makes usages like `newEntity().With(name)` possible.

```csharp
c => c.WithMethod()
```
