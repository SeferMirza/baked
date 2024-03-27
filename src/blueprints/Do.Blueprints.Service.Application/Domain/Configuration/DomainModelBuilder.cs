﻿using Do.Domain.Model;
using System.Reflection;

namespace Do.Domain.Configuration;

internal class DomainModelBuilder(DomainBuilderOptions _options)
{
    readonly ModelKeyedCollection<TypeModel> _reflectedTypes = [];
    readonly ModelKeyedCollection<TypeModel> _referencedTypes = [];

    internal DomainModel BuildFrom(IDomainTypeCollection reflectedTypes)
    {
        foreach (var type in reflectedTypes)
        {
            _reflectedTypes.Add(new(type, TypeModel.IdFrom(type)));
        }

        foreach (var typeModel in _reflectedTypes)
        {
            typeModel.Apply(t =>
            {
                typeModel.SetGenerics(
                    genericTypeArguments: typeModel.IsGenericType ? BuildGenericTypeArguments(t) : [],
                    genericTypeDefinition: typeModel.IsGenericType ? BuildGenericTypeDefinition(t) : default
                );
                typeModel.SetInheritance(
                    baseType: BuildBaseType(t),
                    interfaces: BuildInterfaces(t)
                );
                typeModel.SetMetadata(
                    customAttributes: BuildCustomAttributes(t)
                );
                typeModel.SetMembers(
                    constructor: BuildConstructor(t, typeModel),
                    properties: BuildProperties(t, typeModel),
                    methodGroups: BuildMethodGroups(t, typeModel)
                );
            });
        }

        return new(new(_reflectedTypes), new(_referencedTypes));
    }

    TypeModel GetOrCreateTypeModel(Type type)
    {
        var id = TypeModel.IdFrom(type);
        if (_reflectedTypes.TryGetValue(id, out var result)) { return result; }
        if (_referencedTypes.TryGetValue(id, out result)) { return result; }

        var typeModel = new TypeModel(type, id);

        _referencedTypes.Add(typeModel);

        typeModel.Apply(t =>
        {
            if (_options.ReferencedType.ShouldSkipSetGenerics.Any(f => f(t))) { return; }

            typeModel.SetGenerics(
                genericTypeArguments: typeModel.IsGenericType ? BuildGenericTypeArguments(t) : [],
                genericTypeDefinition: typeModel.IsGenericType ? BuildGenericTypeDefinition(t) : default
            );
        });

        typeModel.Apply(t =>
        {
            if (_options.ReferencedType.ShouldSkipSetInheritance.Any(f => f(t))) { return; }

            typeModel.SetInheritance(
                baseType: BuildBaseType(t),
                interfaces: BuildInterfaces(t)
            );
        });

        return typeModel;
    }

    MethodGroupModel? BuildConstructor(Type type, TypeModel target)
    {
        var constructorInfos = type.GetConstructors(_options.ReflectedType.ConstructorBindingFlags) ?? [];
        if (!constructorInfos.Any()) { return null; }

        var ctor = new MethodGroupModel(target, ".ctor");
        ctor.Init(
            methods: [.. constructorInfos.Select(c => BuildConstructorOverload(c, ctor))],
            customAttributes: []
        );

        return ctor;
    }

    ModelCollection<MethodGroupModel> BuildMethodGroups(Type type, TypeModel target)
    {
        var methods = new Dictionary<string, MethodGroupModel>();
        var methodInfos = type.GetMethods(_options.ReflectedType.MethodBindingFlags) ?? [];
        foreach (var group in methodInfos.GroupBy(m => m.Name))
        {
            var method = methods[group.Key] = new(target, group.Key);
            // reflected type parent
            method.Init(
                methods: [.. group.Select(m => BuildMethodOverload(m, method))],
                customAttributes: []
            );
        }

        return new(methods.Values);
    }

    TypeModel? BuildBaseType(Type type) =>
        type.BaseType is null ? default : GetOrCreateTypeModel(type.BaseType);

    AttributeCollection BuildCustomAttributes(MemberInfo member) =>
        new(member.GetCustomAttributes());

    ModelCollection<TypeModel> BuildGenericTypeArguments(Type type) =>
        new(type.GenericTypeArguments.Select(GetOrCreateTypeModel));

    TypeModel? BuildGenericTypeDefinition(Type type) =>
         type.IsGenericTypeDefinition ? default : GetOrCreateTypeModel(type.GetGenericTypeDefinition());

    ModelCollection<TypeModel> BuildInterfaces(Type type) =>
        new(type.GetInterfaces().Select(GetOrCreateTypeModel));

    MethodModel BuildConstructorOverload(ConstructorInfo constructor, MethodGroupModel parent)
    {
        var overload = new MethodModel(parent, constructor.IsPublic, constructor.IsFamily, constructor.IsVirtual);
        overload.Init(
            parameters: BuildParameters(constructor, overload),
            customAttributes: []
        );

        return overload;
    }

    MethodModel BuildMethodOverload(MethodInfo method, MethodGroupModel group)
    {
        var overload = new MethodModel(group, method.IsPublic, method.IsFamily, method.IsVirtual, GetOrCreateTypeModel(method.ReturnType));
        overload.Init(
            parameters: BuildParameters(method, overload),
            customAttributes: []
        );

        return overload;
    }

    ModelCollection<ParameterModel> BuildParameters(MethodBase method, MethodModel overload) =>
        new(method.GetParameters().Select(p => BuildParameter(p, overload)));

    ParameterModel BuildParameter(ParameterInfo parameter, MethodModel overload) =>
        new(overload, parameter.Name ?? string.Empty, GetOrCreateTypeModel(parameter.ParameterType), parameter.IsOptional, parameter.DefaultValue, BuildCustomAttributes(parameter.Member));

    ModelCollection<PropertyModel> BuildProperties(Type type, TypeModel owner) =>
        new(type.GetProperties(_options.ReflectedType.PropertyBindingFlags).Select(p => BuildProperty(p, owner)));

    PropertyModel BuildProperty(PropertyInfo property, TypeModel owner) =>
        new(owner, property.Name, GetOrCreateTypeModel(property.PropertyType), IsPublic(property), IsVirtual(property), BuildCustomAttributes(property));

    bool IsPublic(PropertyInfo property) =>
        property.GetMethod?.IsPublic == true;

    bool IsVirtual(PropertyInfo property) =>
        property.GetMethod?.IsVirtual == true;
}
