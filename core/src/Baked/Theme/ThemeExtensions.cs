﻿using Baked.Architecture;
using Baked.Domain;
using Baked.Domain.Configuration;
using Baked.Domain.Model;
using Baked.RestApi;
using Baked.RestApi.Model;
using Baked.Testing;
using Baked.Theme;
using Baked.Ui;

namespace Baked;

public static class ThemeExtensions
{
    public static void AddTheme(this List<IFeature> features, Func<ThemeConfigurator, IFeature<ThemeConfigurator>> configure) =>
        features.Add(configure(new()));

    public static T Apply<T>(this Action<T>? action, T result)
    {
        action?.Invoke(result);

        return result;
    }

    public static Route Index(this Router router,
        string? title = default,
        string? icon = default
    ) => router.Root("/", title ?? "Home", icon ?? "pi pi-home");

    public static Route Root(this Router router, string path, string title, string icon) =>
        router.Create(path, title) with { Icon = icon, SideMenu = true, ErrorSafeLink = true };

    public static Route Child(this Router router, string path, string title, string parentPath) =>
        router.Create(path, title) with { ParentPath = parentPath };

    public static MethodModel GetMethod(this TypeModel type, string name) =>
        type.GetMembers().Methods[name];

    public static ActionModelAttribute GetAction(this MethodModel method) =>
        method.Get<ActionModelAttribute>();

    public static string GetRoute(this ActionModelAttribute action, List<(string key, string value)> routeParameters)
    {
        var routeParts = action.GetRoute().Split("/");
        foreach (var (key, value) in routeParameters)
        {
            var parameter = action.Parameter[key];
            if (parameter.FromRoute)
            {
                routeParts[parameter.RoutePosition] = value;
            }
        }

        return routeParts.Join('/');
    }

    public static IEnumerable<string> GetEnumNames(this TypeModel type) =>
        [.. type.Apply(t => Enum.GetNames(t).Select(n => n.TrimStart('_')))];

    public static TypeModel SkipNullable(this TypeModel type)
    {
        if (!type.IsAssignableTo(typeof(Nullable<>))) { return type; }
        if (!type.TryGetGenerics(out var generics)) { throw new InvalidOperationException($"{type.Name} doesn't provide generics information"); }
        if (type.IsGenericTypeDefinition) { return type; }

        return generics.GenericTypeArguments.First().Model;
    }

    public static TypeModel SkipTask(this TypeModel typeModel) =>
        typeModel.IsAssignableTo<Task>() && typeModel.IsGenericType
            ? typeModel.GetGenerics().GenericTypeArguments.First().Model
            : typeModel;

    public static bool ReturnsList(this MethodOverloadModel methodOverload) =>
        methodOverload.ReturnType.SkipTask().IsAssignableTo<IList>();

    public static PageBuilder From<T>(this Page.Generator _) =>
        context =>
        {
            var (domain, l) = context;

            if (!domain.Types[typeof(T)].TryGetMetadata(out var metadata)) { throw new($"{typeof(T).Name} cannot be used as a page source, because its metadata is not included in domain model"); }

            return metadata.GetRequiredComponent(context.Drill(nameof(Page), typeof(T).Name));
        };

    public static TComponentSchema As<TComponentSchema>(this IComponentSchema schema) where TComponentSchema : IComponentSchema =>
        (TComponentSchema)schema;

    public static TData As<TData>(this IData data) where TData : IData =>
        (TData)data;

    public static PageContext APageContext(this Stubber giveMe,
        string? path = default,
        string? title = default
    )
    {
        path ??= "/";
        title ??= "TEST PAGE";

        return new()
        {
            Route = new(path, title),
            Sitemap = [],
            Domain = giveMe.TheDomainModel(),
            NewLocaleKey = s => s
        };
    }

    public static ComponentContext AComponentContext(this Stubber giveMe,
        object[]? paths = default
    )
    {
        paths ??= [];

        return giveMe
            .APageContext()
            .Drill(paths);
    }

    public static IEnumerable<T> WhereAppliesTo<T>(this IEnumerable<T> enumerable, ComponentContext context) =>
        enumerable.Where(c => c is not IComponentContextFilter when || when.AppliesTo(context));

    // NOTE
    //
    // This is refactored to remove duplication in below conventions but
    // compromises code readability. It basically wraps the given builder
    // and applies given function only when given filter (`when`) passes.
    //
    // Filter is applied within the function because it is the only
    // way to access to the component context.
    static void WrapBuilder<TSchema>(
        this DescriptorBuilderAttribute<TSchema> attribute,
        Func<ComponentContext, bool> when,
        Action<TSchema, ComponentContext> apply
    )
    {
        var prev = attribute.Builder;

        attribute.Builder = cc =>
        {
            var result = prev(cc);

            if (when(cc))
            {
                apply(result, cc);
            }

            return result;
        };
    }

    #region Descriptor Builder & Schema

    public static List<TSchema> GetSchemas<TSchema>(this ICustomAttributesModel metadata, ComponentContext context)
    {
        if (!metadata.TryGetAll<DescriptorBuilderAttribute<TSchema>>(out var builders)) { return []; }

        return
        [
            .. builders
            .WhereAppliesTo(context)
            .Cast<IComponentContextBasedBuilder<TSchema>>()
            .Select(b => b.Build(context))
        ];
    }

    public static TSchema GetRequiredSchema<TSchema>(this ICustomAttributesModel metadata, ComponentContext context) =>
        metadata.GetSchema<TSchema>(context) ??
        throw new($"`{metadata.CustomAttributes.Name}` doesn't have descriptor for schema type `{typeof(TSchema).Name}` at path `{context.Path}`");

    public static TSchema? GetSchema<TSchema>(this ICustomAttributesModel metadata, ComponentContext context)
    {
        if (!metadata.TryGetAll<DescriptorBuilderAttribute<TSchema>>(out var builders)) { return default; }

        var builder = builders
            .WhereAppliesTo(context)
            .Cast<IComponentContextBasedBuilder<TSchema>>()
            .LastOrDefault();
        if (builder is null) { return default; }

        return builder.Build(context);
    }

    #region Add Metadata

    public static void AddTypeSchema<TSchema>(this IDomainModelConventionCollection conventions, Func<TSchema> schema,
        Func<TypeModelMetadataContext, bool>? when = default,
        Func<ComponentContext, bool>? whenComponent = default,
        int order = default
    ) => conventions.AddTypeSchema(
        schema: _ => schema(),
        when: when,
        whenComponent: whenComponent,
        order: order
    );

    public static void AddTypeSchema<TSchema>(this IDomainModelConventionCollection conventions, Func<TypeModelMetadataContext, TSchema> schema,
        Func<TypeModelMetadataContext, bool>? when = default,
        Func<ComponentContext, bool>? whenComponent = default,
        int order = default
    ) => conventions.AddTypeSchema(
        schema: (c, _) => schema(c),
        when: when,
        whenComponent: whenComponent,
        order: order
    );

    public static void AddTypeSchema<TSchema>(this IDomainModelConventionCollection conventions, Func<TypeModelMetadataContext, ComponentContext, TSchema> schema,
        Func<TypeModelMetadataContext, bool>? when = default,
        Func<ComponentContext, bool>? whenComponent = default,
        int order = default
    )
    {
        when ??= c => true;
        whenComponent ??= c => true;
        order += RestApiLayer.MaxConventionOrder * 2;

        conventions.AddTypeAttribute(
            attribute: c => new DescriptorBuilderAttribute<TSchema>()
            {
                Builder = cc => schema(c, cc),
                Filter = whenComponent
            },
            when: when,
            order: order
        );
    }

    public static void AddPropertySchema<TSchema>(this IDomainModelConventionCollection conventions, Func<TSchema> schema,
        Func<PropertyModelContext, bool>? when = default,
        Func<ComponentContext, bool>? whenComponent = default,
        int order = default
    ) => conventions.AddPropertySchema(
        schema: _ => schema(),
        when: when,
        whenComponent: whenComponent,
        order: order
    );

    public static void AddPropertySchema<TSchema>(this IDomainModelConventionCollection conventions, Func<PropertyModelContext, TSchema> schema,
        Func<PropertyModelContext, bool>? when = default,
        Func<ComponentContext, bool>? whenComponent = default,
        int order = default
    ) => conventions.AddPropertySchema(
        schema: (c, _) => schema(c),
        when: when,
        whenComponent: whenComponent,
        order: order
    );

    public static void AddPropertySchema<TSchema>(this IDomainModelConventionCollection conventions, Func<PropertyModelContext, ComponentContext, TSchema> schema,
        Func<PropertyModelContext, bool>? when = default,
        Func<ComponentContext, bool>? whenComponent = default,
        int order = default
    )
    {
        when ??= c => true;
        whenComponent ??= c => true;
        order += RestApiLayer.MaxConventionOrder * 2;

        conventions.AddPropertyAttribute(
            attribute: c => new DescriptorBuilderAttribute<TSchema>()
            {
                Builder = cc => schema(c, cc),
                Filter = whenComponent
            },
            when: when,
            order: order
        );
    }

    public static void AddMethodSchema<TSchema>(this IDomainModelConventionCollection conventions, Func<TSchema> schema,
        Func<MethodModelContext, bool>? when = default,
        Func<ComponentContext, bool>? whenComponent = default,
        int order = default
    ) => conventions.AddMethodSchema(
        schema: _ => schema(),
        when: when,
        whenComponent: whenComponent,
        order: order
    );

    public static void AddMethodSchema<TSchema>(this IDomainModelConventionCollection conventions, Func<MethodModelContext, TSchema> schema,
        Func<MethodModelContext, bool>? when = default,
        Func<ComponentContext, bool>? whenComponent = default,
        int order = default
    ) => conventions.AddMethodSchema(
        schema: (c, _) => schema(c),
        when: when,
        whenComponent: whenComponent,
        order: order
    );

    public static void AddMethodSchema<TSchema>(this IDomainModelConventionCollection conventions, Func<MethodModelContext, ComponentContext, TSchema> schema,
        Func<MethodModelContext, bool>? when = default,
        Func<ComponentContext, bool>? whenComponent = default,
        int order = default
    )
    {
        when ??= c => true;
        whenComponent ??= c => true;
        order += RestApiLayer.MaxConventionOrder * 2;

        conventions.AddMethodAttribute(
            attribute: c => new DescriptorBuilderAttribute<TSchema>()
            {
                Builder = cc => schema(c, cc),
                Filter = whenComponent
            },
            when: c => c.Type.Has<ControllerModelAttribute>() && c.Method.Has<ActionModelAttribute>() && when(c),
            order: order
        );
    }

    public static void AddParameterSchema<TSchema>(this IDomainModelConventionCollection conventions, Func<TSchema> schema,
        Func<ParameterModelContext, bool>? when = default,
        Func<ComponentContext, bool>? whenComponent = default,
        int order = default
    ) => conventions.AddParameterSchema(
        schema: _ => schema(),
        when: when,
        whenComponent: whenComponent,
        order: order
    );

    public static void AddParameterSchema<TSchema>(this IDomainModelConventionCollection conventions, Func<ParameterModelContext, TSchema> schema,
        Func<ParameterModelContext, bool>? when = default,
        Func<ComponentContext, bool>? whenComponent = default,
        int order = default
    ) => conventions.AddParameterSchema(
        schema: (c, _) => schema(c),
        when: when,
        whenComponent: whenComponent,
        order: order
    );

    public static void AddParameterSchema<TSchema>(this IDomainModelConventionCollection conventions, Func<ParameterModelContext, ComponentContext, TSchema> schema,
        Func<ParameterModelContext, bool>? when = default,
        Func<ComponentContext, bool>? whenComponent = default,
        int order = default
    )
    {
        when ??= c => true;
        whenComponent ??= c => true;
        order += RestApiLayer.MaxConventionOrder * 2;

        conventions.AddParameterAttribute(
            attribute: c => new DescriptorBuilderAttribute<TSchema>()
            {
                Builder = cc => schema(c, cc),
                Filter = whenComponent
            },
            when: c => c.Type.Has<ControllerModelAttribute>() && c.Parameter.Has<ParameterModelAttribute>() && when(c),
            order: order
        );
    }

    #endregion

    #region Add Configuration

    public static void AddTypeSchemaConfiguration<TSchema>(this IDomainModelConventionCollection conventions, Action<TSchema> schema,
        Func<TypeModelMetadataContext, bool>? when = default,
        Func<ComponentContext, bool>? whenComponent = default,
        int order = default
    ) => conventions.AddTypeSchemaConfiguration<TSchema>((s, _) => schema(s),
        when: when,
        whenComponent: whenComponent,
        order: order
    );

    public static void AddTypeSchemaConfiguration<TSchema>(this IDomainModelConventionCollection conventions, Action<TSchema, TypeModelMetadataContext> schema,
        Func<TypeModelMetadataContext, bool>? when = default,
        Func<ComponentContext, bool>? whenComponent = default,
        int order = default
    ) => conventions.AddTypeSchemaConfiguration<TSchema>((s, c, _) => schema(s, c),
        when: when,
        whenComponent: whenComponent,
        order: order
    );

    public static void AddTypeSchemaConfiguration<TSchema>(this IDomainModelConventionCollection conventions, Action<TSchema, TypeModelMetadataContext, ComponentContext> schema,
        Func<TypeModelMetadataContext, bool>? when = default,
        Func<ComponentContext, bool>? whenComponent = default,
        int order = default
    )
    {
        when ??= _ => true;
        whenComponent ??= _ => true;

        conventions.AddTypeAttributeConfiguration<DescriptorBuilderAttribute<TSchema>>(
            attribute: (attribute, c) => attribute.WrapBuilder(
                apply: (s, cc) => schema(s, c, cc),
                when: whenComponent
            ),
            when: c => when(c),
            order: order
        );
    }

    public static void AddPropertySchemaConfiguration<TSchema>(this IDomainModelConventionCollection conventions, Action<TSchema> schema,
        Func<PropertyModelContext, bool>? when = default,
        Func<ComponentContext, bool>? whenComponent = default,
        int order = default
    ) => conventions.AddPropertySchemaConfiguration<TSchema>((s, _) => schema(s),
        when: when,
        whenComponent: whenComponent,
        order: order
    );

    public static void AddPropertySchemaConfiguration<TSchema>(this IDomainModelConventionCollection conventions, Action<TSchema, PropertyModelContext> schema,
        Func<PropertyModelContext, bool>? when = default,
        Func<ComponentContext, bool>? whenComponent = default,
        int order = default
    ) => conventions.AddPropertySchemaConfiguration<TSchema>((s, c, _) => schema(s, c),
        when: when,
        whenComponent: whenComponent,
        order: order
    );

    public static void AddPropertySchemaConfiguration<TSchema>(this IDomainModelConventionCollection conventions, Action<TSchema, PropertyModelContext, ComponentContext> schema,
        Func<PropertyModelContext, bool>? when = default,
        Func<ComponentContext, bool>? whenComponent = default,
        int order = default
    )
    {
        when ??= _ => true;
        whenComponent ??= _ => true;

        conventions.AddPropertyAttributeConfiguration<DescriptorBuilderAttribute<TSchema>>(
            attribute: (attribute, c) => attribute.WrapBuilder(
                apply: (s, cc) => schema(s, c, cc),
                when: whenComponent
            ),
            when: c => when(c),
            order: order
        );
    }

    public static void AddMethodSchemaConfiguration<TSchema>(this IDomainModelConventionCollection conventions, Action<TSchema> schema,
        Func<MethodModelContext, bool>? when = default,
        Func<ComponentContext, bool>? whenComponent = default,
        int order = default
    ) => conventions.AddMethodSchemaConfiguration<TSchema>((s, _) => schema(s),
        when: when,
        whenComponent: whenComponent,
        order: order
    );

    public static void AddMethodSchemaConfiguration<TSchema>(this IDomainModelConventionCollection conventions, Action<TSchema, MethodModelContext> schema,
        Func<MethodModelContext, bool>? when = default,
        Func<ComponentContext, bool>? whenComponent = default,
        int order = default
    ) => conventions.AddMethodSchemaConfiguration<TSchema>((s, c, _) => schema(s, c),
        when: when,
        whenComponent: whenComponent,
        order: order
    );

    public static void AddMethodSchemaConfiguration<TSchema>(this IDomainModelConventionCollection conventions, Action<TSchema, MethodModelContext, ComponentContext> schema,
        Func<MethodModelContext, bool>? when = default,
        Func<ComponentContext, bool>? whenComponent = default,
        int order = default
    )
    {
        when ??= _ => true;
        whenComponent ??= _ => true;

        conventions.AddMethodAttributeConfiguration<DescriptorBuilderAttribute<TSchema>>(
            attribute: (attribute, c) => attribute.WrapBuilder(
                apply: (s, cc) => schema(s, c, cc),
                when: whenComponent
            ),
            when: c => when(c),
            order: order
        );
    }

    public static void AddParameterSchemaConfiguration<TSchema>(this IDomainModelConventionCollection conventions, Action<TSchema> schema,
        Func<ParameterModelContext, bool>? when = default,
        Func<ComponentContext, bool>? whenComponent = default,
        int order = default
    ) => conventions.AddParameterSchemaConfiguration<TSchema>((s, _) => schema(s),
        when: when,
        whenComponent: whenComponent,
        order: order
    );

    public static void AddParameterSchemaConfiguration<TSchema>(this IDomainModelConventionCollection conventions, Action<TSchema, ParameterModelContext> schema,
        Func<ParameterModelContext, bool>? when = default,
        Func<ComponentContext, bool>? whenComponent = default,
        int order = default
    ) => conventions.AddParameterSchemaConfiguration<TSchema>((s, c, _) => schema(s, c),
        when: when,
        whenComponent: whenComponent,
        order: order
    );

    public static void AddParameterSchemaConfiguration<TSchema>(this IDomainModelConventionCollection conventions, Action<TSchema, ParameterModelContext, ComponentContext> schema,
        Func<ParameterModelContext, bool>? when = default,
        Func<ComponentContext, bool>? whenComponent = default,
        int order = default
    )
    {
        when ??= _ => true;
        whenComponent ??= _ => true;

        conventions.AddParameterAttributeConfiguration<DescriptorBuilderAttribute<TSchema>>(
            attribute: (attribute, c) => attribute.WrapBuilder(
                apply: (s, cc) => schema(s, c, cc),
                when: whenComponent
            ),
            when: c => when(c),
            order: order
        );
    }

    #endregion

    #endregion

    #region Component Descriptor Builder & Component

    public static IComponentDescriptor GetRequiredComponent(this ICustomAttributesModel metadata, ComponentContext context) =>
        metadata.GetComponent(context) ??
        throw new($"{metadata.CustomAttributes.Name} doesn't have any component descriptor at path `{context.Path}`");

    public static IComponentDescriptor? GetComponent(this ICustomAttributesModel metadata, ComponentContext context)
    {
        if (!metadata.TryGetAll<ContextBasedComponentAttribute>(out var contextBasedComponents)) { return default; }

        foreach (var contextBasedComponent in contextBasedComponents.WhereAppliesTo(context).Reverse())
        {
            var builderType = typeof(ComponentDescriptorBuilderAttribute<>).MakeGenericType(contextBasedComponent.SchemaType);
            if (!metadata.TryGetAll(builderType, out var builders)) { continue; }

            var builder = builders
                .Cast<IComponentContextBasedBuilder<IComponentDescriptor>>()
                .WhereAppliesTo(context)
                .LastOrDefault();
            if (builder is null) { continue; }

            return builder.Build(context);
        }

        return default;
    }

    #region Add Metadata

    public static void AddTypeComponent<TSchema>(this IDomainModelConventionCollection conventions, Func<ComponentDescriptor<TSchema>> component,
        Func<TypeModelMetadataContext, bool>? when = default,
        Func<ComponentContext, bool>? whenComponent = default,
        int order = default
    ) where TSchema : IComponentSchema =>
        conventions.AddTypeComponent(
            component: _ => component(),
            when: when,
            whenComponent: whenComponent,
            order: order
        );

    public static void AddTypeComponent<TSchema>(this IDomainModelConventionCollection conventions, Func<TypeModelMetadataContext, ComponentDescriptor<TSchema>> component,
        Func<TypeModelMetadataContext, bool>? when = default,
        Func<ComponentContext, bool>? whenComponent = default,
        int order = default
    ) where TSchema : IComponentSchema =>
        conventions.AddTypeComponent(
            component: (c, _) => component(c),
            when: when,
            whenComponent: whenComponent,
            order: order
        );

    public static void AddTypeComponent<TSchema>(this IDomainModelConventionCollection conventions, Func<TypeModelMetadataContext, ComponentContext, ComponentDescriptor<TSchema>> component,
        Func<TypeModelMetadataContext, bool>? when = default,
        Func<ComponentContext, bool>? whenComponent = default,
        int order = default
    ) where TSchema : IComponentSchema
    {
        when ??= c => true;
        whenComponent ??= c => true;
        order += RestApiLayer.MaxConventionOrder * 2;

        conventions.AddTypeAttribute(
            apply: (c, add) =>
            {
                add(c.Type, new ComponentDescriptorBuilderAttribute<TSchema>()
                {
                    Builder = cc => component(c, cc),
                    Filter = whenComponent
                });
                add(c.Type, new ContextBasedComponentAttribute(typeof(TSchema))
                {
                    Filter = whenComponent
                });
            },
            when: c => when(c),
            order: order
        );
    }

    public static void AddPropertyComponent<TSchema>(this IDomainModelConventionCollection conventions, Func<ComponentDescriptor<TSchema>> component,
        Func<PropertyModelContext, bool>? when = default,
        Func<ComponentContext, bool>? whenComponent = default,
        int order = default
    ) where TSchema : IComponentSchema =>
        conventions.AddPropertyComponent(
            component: _ => component(),
            when: when,
            whenComponent: whenComponent,
            order: order
        );

    public static void AddPropertyComponent<TSchema>(this IDomainModelConventionCollection conventions, Func<PropertyModelContext, ComponentDescriptor<TSchema>> component,
        Func<PropertyModelContext, bool>? when = default,
        Func<ComponentContext, bool>? whenComponent = default,
        int order = default
    ) where TSchema : IComponentSchema =>
        conventions.AddPropertyComponent(
            component: (c, _) => component(c),
            when: when,
            whenComponent: whenComponent,
            order: order
        );

    public static void AddPropertyComponent<TSchema>(this IDomainModelConventionCollection conventions, Func<PropertyModelContext, ComponentContext, ComponentDescriptor<TSchema>> component,
        Func<PropertyModelContext, bool>? when = default,
        Func<ComponentContext, bool>? whenComponent = default,
        int order = default
    ) where TSchema : IComponentSchema
    {
        when ??= c => true;
        whenComponent ??= c => true;
        order += RestApiLayer.MaxConventionOrder * 2;

        conventions.AddPropertyAttribute(
            apply: (c, add) =>
            {
                add(c.Property, new ComponentDescriptorBuilderAttribute<TSchema>()
                {
                    Builder = cc => component(c, cc),
                    Filter = whenComponent
                });
                add(c.Property, new ContextBasedComponentAttribute(typeof(TSchema))
                {
                    Filter = whenComponent
                });
            },
            when: c => when(c),
            order: order
        );
    }

    public static void AddMethodComponent<TSchema>(this IDomainModelConventionCollection conventions, Func<ComponentDescriptor<TSchema>> component,
        Func<MethodModelContext, bool>? when = default,
        Func<ComponentContext, bool>? whenComponent = default,
        int order = default
    ) where TSchema : IComponentSchema =>
        conventions.AddMethodComponent(
            component: _ => component(),
            when: when,
            whenComponent: whenComponent,
            order: order
        );

    public static void AddMethodComponent<TSchema>(this IDomainModelConventionCollection conventions, Func<MethodModelContext, ComponentDescriptor<TSchema>> component,
        Func<MethodModelContext, bool>? when = default,
        Func<ComponentContext, bool>? whenComponent = default,
        int order = default
    ) where TSchema : IComponentSchema =>
        conventions.AddMethodComponent(
            component: (c, _) => component(c),
            when: when,
            whenComponent: whenComponent,
            order: order
        );

    public static void AddMethodComponent<TSchema>(this IDomainModelConventionCollection conventions, Func<MethodModelContext, ComponentContext, ComponentDescriptor<TSchema>> component,
        Func<MethodModelContext, bool>? when = default,
        Func<ComponentContext, bool>? whenComponent = default,
        int order = default
    ) where TSchema : IComponentSchema
    {
        when ??= c => true;
        whenComponent ??= c => true;
        order += RestApiLayer.MaxConventionOrder * 2;

        conventions.AddMethodAttribute(
            apply: (c, add) =>
            {
                add(c.Method, new ComponentDescriptorBuilderAttribute<TSchema>()
                {
                    Builder = cc => component(c, cc),
                    Filter = whenComponent
                });
                add(c.Method, new ContextBasedComponentAttribute(typeof(TSchema))
                {
                    Filter = whenComponent
                });
            },
            when: c => when(c),
            order: order
        );
    }

    public static void AddParameterComponent<TSchema>(this IDomainModelConventionCollection conventions, Func<ComponentDescriptor<TSchema>> component,
        Func<ParameterModelContext, bool>? when = default,
        Func<ComponentContext, bool>? whenComponent = default,
        int order = default
    ) where TSchema : IComponentSchema =>
        conventions.AddParameterComponent(
            component: _ => component(),
            when: when,
            whenComponent: whenComponent,
            order: order
        );

    public static void AddParameterComponent<TSchema>(this IDomainModelConventionCollection conventions, Func<ParameterModelContext, ComponentDescriptor<TSchema>> component,
        Func<ParameterModelContext, bool>? when = default,
        Func<ComponentContext, bool>? whenComponent = default,
        int order = default
    ) where TSchema : IComponentSchema =>
        conventions.AddParameterComponent(
            component: (c, _) => component(c),
            when: when,
            whenComponent: whenComponent,
            order: order
        );

    public static void AddParameterComponent<TSchema>(this IDomainModelConventionCollection conventions, Func<ParameterModelContext, ComponentContext, ComponentDescriptor<TSchema>> component,
        Func<ParameterModelContext, bool>? when = default,
        Func<ComponentContext, bool>? whenComponent = default,
        int order = default
    ) where TSchema : IComponentSchema
    {
        when ??= c => true;
        whenComponent ??= c => true;
        order += RestApiLayer.MaxConventionOrder * 2;

        conventions.AddParameterAttribute(
            apply: (c, add) =>
            {
                add(c.Parameter, new ComponentDescriptorBuilderAttribute<TSchema>()
                {
                    Builder = cc => component(c, cc),
                    Filter = whenComponent
                });
                add(c.Parameter, new ContextBasedComponentAttribute(typeof(TSchema))
                {
                    Filter = whenComponent
                });
            },
            when: c => when(c),
            order: order
        );
    }

    #endregion

    #region Add Configuration

    public static void AddTypeComponentConfiguration<TSchema>(this IDomainModelConventionCollection conventions, Action<ComponentDescriptor<TSchema>> component,
        Func<TypeModelMetadataContext, bool>? when = default,
        Func<ComponentContext, bool>? whenComponent = default,
        int order = default
    ) where TSchema : IComponentSchema =>
        conventions.AddTypeComponentConfiguration<TSchema>((s, _) => component(s),
            when: when,
            whenComponent: whenComponent,
            order: order
        );

    public static void AddTypeComponentConfiguration<TSchema>(this IDomainModelConventionCollection conventions, Action<ComponentDescriptor<TSchema>, TypeModelMetadataContext> component,
        Func<TypeModelMetadataContext, bool>? when = default,
        Func<ComponentContext, bool>? whenComponent = default,
        int order = default
    ) where TSchema : IComponentSchema =>
        conventions.AddTypeComponentConfiguration<TSchema>((s, c, _) => component(s, c),
            when: when,
            whenComponent: whenComponent,
            order: order
        );

    public static void AddTypeComponentConfiguration<TSchema>(this IDomainModelConventionCollection conventions, Action<ComponentDescriptor<TSchema>, TypeModelMetadataContext, ComponentContext> component,
        Func<TypeModelMetadataContext, bool>? when = default,
        Func<ComponentContext, bool>? whenComponent = default,
        int order = default
    ) where TSchema : IComponentSchema
    {
        when ??= _ => true;
        whenComponent ??= _ => true;

        conventions.AddTypeAttributeConfiguration<ComponentDescriptorBuilderAttribute<TSchema>>(
            attribute: (attribute, c) => attribute.WrapBuilder(
                apply: (d, cc) => component(d, c, cc),
                when: whenComponent
            ),
            when: c => when(c),
            order: order
        );
    }

    public static void AddPropertyComponentConfiguration<TSchema>(this IDomainModelConventionCollection conventions, Action<ComponentDescriptor<TSchema>> component,
        Func<PropertyModelContext, bool>? when = default,
        Func<ComponentContext, bool>? whenComponent = default,
        int order = default
    ) where TSchema : IComponentSchema =>
        conventions.AddPropertyComponentConfiguration<TSchema>((s, _) => component(s),
            when: when,
            whenComponent: whenComponent,
            order: order
        );

    public static void AddPropertyComponentConfiguration<TSchema>(this IDomainModelConventionCollection conventions, Action<ComponentDescriptor<TSchema>, PropertyModelContext> component,
        Func<PropertyModelContext, bool>? when = default,
        Func<ComponentContext, bool>? whenComponent = default,
        int order = default
    ) where TSchema : IComponentSchema =>
        conventions.AddPropertyComponentConfiguration<TSchema>((s, c, _) => component(s, c),
            when: when,
            whenComponent: whenComponent,
            order: order
        );

    public static void AddPropertyComponentConfiguration<TSchema>(this IDomainModelConventionCollection conventions, Action<ComponentDescriptor<TSchema>, PropertyModelContext, ComponentContext> component,
        Func<PropertyModelContext, bool>? when = default,
        Func<ComponentContext, bool>? whenComponent = default,
        int order = default
    ) where TSchema : IComponentSchema
    {
        when ??= _ => true;
        whenComponent ??= _ => true;

        conventions.AddPropertyAttributeConfiguration<ComponentDescriptorBuilderAttribute<TSchema>>(
            attribute: (attribute, c) => attribute.WrapBuilder(
                apply: (d, cc) => component(d, c, cc),
                when: whenComponent
            ),
            when: c => when(c),
            order: order
        );
    }

    public static void AddMethodComponentConfiguration<TSchema>(this IDomainModelConventionCollection conventions, Action<ComponentDescriptor<TSchema>> component,
        Func<MethodModelContext, bool>? when = default,
        Func<ComponentContext, bool>? whenComponent = default,
        int order = default
    ) where TSchema : IComponentSchema =>
        conventions.AddMethodComponentConfiguration<TSchema>((s, _) => component(s),
            when: when,
            whenComponent: whenComponent,
            order: order
        );

    public static void AddMethodComponentConfiguration<TSchema>(this IDomainModelConventionCollection conventions, Action<ComponentDescriptor<TSchema>, MethodModelContext> component,
        Func<MethodModelContext, bool>? when = default,
        Func<ComponentContext, bool>? whenComponent = default,
        int order = default
    ) where TSchema : IComponentSchema =>
        conventions.AddMethodComponentConfiguration<TSchema>((s, c, _) => component(s, c),
            when: when,
            whenComponent: whenComponent,
            order: order
        );

    public static void AddMethodComponentConfiguration<TSchema>(this IDomainModelConventionCollection conventions, Action<ComponentDescriptor<TSchema>, MethodModelContext, ComponentContext> component,
        Func<MethodModelContext, bool>? when = default,
        Func<ComponentContext, bool>? whenComponent = default,
        int order = default
    ) where TSchema : IComponentSchema
    {
        when ??= _ => true;
        whenComponent ??= _ => true;

        conventions.AddMethodAttributeConfiguration<ComponentDescriptorBuilderAttribute<TSchema>>(
            attribute: (attribute, c) => attribute.WrapBuilder(
                apply: (d, cc) => component(d, c, cc),
                when: whenComponent
            ),
            when: c => when(c),
            order: order
        );
    }

    public static void AddParameterComponentConfiguration<TSchema>(this IDomainModelConventionCollection conventions, Action<ComponentDescriptor<TSchema>> component,
        Func<ParameterModelContext, bool>? when = default,
        Func<ComponentContext, bool>? whenComponent = default,
        int order = default
    ) where TSchema : IComponentSchema =>
        conventions.AddParameterComponentConfiguration<TSchema>((s, _) => component(s),
            when: when,
            whenComponent: whenComponent,
            order: order
        );

    public static void AddParameterComponentConfiguration<TSchema>(this IDomainModelConventionCollection conventions, Action<ComponentDescriptor<TSchema>, ParameterModelContext> component,
        Func<ParameterModelContext, bool>? when = default,
        Func<ComponentContext, bool>? whenComponent = default,
        int order = default
    ) where TSchema : IComponentSchema =>
        conventions.AddParameterComponentConfiguration<TSchema>((s, c, _) => component(s, c),
            when: when,
            whenComponent: whenComponent,
            order: order
        );

    public static void AddParameterComponentConfiguration<TSchema>(this IDomainModelConventionCollection conventions, Action<ComponentDescriptor<TSchema>, ParameterModelContext, ComponentContext> component,
        Func<ParameterModelContext, bool>? when = default,
        Func<ComponentContext, bool>? whenComponent = default,
        int order = default
    ) where TSchema : IComponentSchema
    {
        when ??= _ => true;
        whenComponent ??= _ => true;

        conventions.AddParameterAttributeConfiguration<ComponentDescriptorBuilderAttribute<TSchema>>(
            attribute: (attribute, c) => attribute.WrapBuilder(
                apply: (d, cc) => component(d, c, cc),
                when: whenComponent
            ),
            when: c => when(c),
            order: order
        );
    }

    #endregion

    #endregion
}