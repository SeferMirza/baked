# Domain

DO introduces a model generation mechanism to reflect the business domain of
a project. The generated model instance can be used in directly in layers or 
in features while configuring configuration targets.

```csharp
app.Layers.AddDomain();
```

## Configuration Targets

This layer provides `IAssemblyCollection`, `ITypeCollection` and 
`DomainBuilderOptions` as configuration targets for building `DomainModel`.

### `IAssemblyCollection`

This target is provided in `BuildConfiguration` phase. To configure it in a 
feature;

```csharp
configurator.ConfigureDomainAssemblyCollection(assemblies =>
{
    ...
});
```

### `ITypeCollection`

This target is provided in `BuildConfiguration` phase. To configure it in a 
feature;

```csharp
configurator.ConfigureDomainTypeCollection(types =>
{
    ...
});
```

### `DomainOptions`

This target is provided in `BuildConfiguration` phase. To configure it in a 
feature;

```csharp
configurator.ConfigureDomainBuilderOptions(options =>
{
    ...
});
```

## Phases

This layer introduces following phases to the application it is added;

- `BuildDomain`: This phase adds an `DomainModel` instance to the application
  context and runs before `AddServices` to allow the access to `DomainModel`.
