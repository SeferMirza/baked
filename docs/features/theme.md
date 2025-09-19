# Theme

This feature implementations configures `DomainModel` metadata attributes and
extracts component descriptors for ui theme pages and components of an
application.

## Default

Default Theme feature introduces many components and sets defaults for the
domain model. It also configures error pages, layouts, and pages.

To use this theme directly without any update use this;

```csharp
c => c.Default(
    index: ...,
    routes: [...],
    errorPageOptions: ep => ...,
    sideMenuOptions: sm => ...,
    headerOptions: h => ...,
    debugComponentPaths: ...
)
```

Or you may prefer to create your custom theme on top of this default theme;

```csharp
public static class CustomThemeExtensions
{
    public static CustomThemeFeature Custom(this ThemeConfigurator _) =>
        new(
        [
            // All app routes
        ]);
}

public class CustomThemeFeature(IEnumerable<Func<Router, Route>> routes)
    : DefaultThemeFeature(routes.Select(r => r(new())))
{
    public override void Configure(LayerConfigurator configurator)
    {
        // Applies default theme rules
        base.Configure(configurator);

        configurator.ConfigureDomainModelBuilder(builder =>
        {
            // Your custom conventions and page overrides
        });

        configurator.ConfigureComponentExports(c =>
        {
            // Add your component exports using your own `Components` extensions
            // c.AddFromExtensions(typeof(Components));
        });

        configurator.ConfigurePageDescriptors(pages =>
        {
            // Add other pages like `auth/login.vue` etc.
        });
    }
}
```

Below you can find the rules that comes with this theme. For other user
experiences, see [UX Feature](ux.md)

| Group      | Rules                                                                                    |
| ---        | ---                                                                                      |
| Property   | All public properties get a `DataAttribute` with a camelized name and titleized label    |
|            | Default component is `None`                                                              |
|            | `string` and `Guid` properties render with `String`                                      |
| Method     | All actions with `ActionModelAttribute` get a `TabAttribute`                             |
|            | Each action is wired as a remote method with `MethodRemote`                              |
| Parameter  | Parameters with `ParameterModelAttribute` use `Parameter` schema                         |
|            | Required and default values are taken from the attribute                                 |
| Enum       | Enum types render inline with `EnumInline`                                               |
| Data Table | Default rows set to 5                                                                    |
|            | Paginator enabled                                                                        |
|            | Export option added for methods with `DataTable`                                         |
|            | Properties with `DataAttribute` render as `DataTable.Column`                             |
|            | Column titles use the label of the property and are exportable                           |
| Error Page | Error page includes safe links for routes with `ErrorSafeLink`                           |
|            | Predefined error messages:                                                               |
|            | &nbsp; ↳ `403` Access Denied                                                             |
|            | &nbsp; ↳ `404` Page Not Found                                                            |
|            | &nbsp; ↳ `500` Unexpected Error                                                          |
|            | Messages can be customized with `errorPageOptions:`                                      |
| Layouts    | `DefaultLayout` includes                                                                 |
|            | &nbsp; ↳ Side menu with routes marked for `SideMenu`                                     |
|            | &nbsp; ↳ Header sitemap with enabled routes                                              |
|            | Both side menu and header can be customized with `sideMenuOptions:` and `headerOptions:` |
|            | `ModalLayout` also included                                                              |
| Pages      | Builds pages from routes using domain model and localization                             |
|            | Each route becomes a page if it can be resolved                                          |

### Menu and Routes

`routes:` list is a list of builder functions that takes `Router` as a parameter
and returns `Route` instance. Three types of route is supported out of the box.

```csharp
r => r.Index() with { ... },
r => r.Root("/my-parent", "MyParent", "pi pi-user") with { ... },
r => r.Child("/my-parent/my-child", "MyChild", "/my-parent") with { ... },
```

### Page Types

There are three types of page, `.Implemented()`, `.Described(d => ...)` and
`.Generated(g => ...)`. Each route has a `Page` property that defines what page
to render at that route.

By default pages use `.Implemented()`, meaning you need to have a `.vue` page at
the given route, e.g. for `/my-parent` route to work you need to have
`/pages/my-parent.vue` file in the UI project.

```csharp
r => r.Root("/my-parent", "MyParent", "pi pi-user") with { Page = p => p.Implemented() }

// or

r => r.Root("/my-parent", "MyParent", "pi pi-user")
```

`.Described(d => ...)` is appropriate when you decide to fully describe the page
manually from your theme feature. Add an extension method to `Page.Describer`
for your custom page and use it in routes.

```csharp
r => r.Root("/my-parent", "MyParent", "pi pi-user") with { Page = p => p.Described(d => d.MyParent()) }
```

`.Generated(g => ...)` is appropriate when you have necessary conventions to
generate a page descriptor out of a domain model.

```csharp
r => r.Root("/my-parent", "MyParent", "pi pi-user") with { Page = p => p.Generated(g => g.From<MyParent>()) }
```
