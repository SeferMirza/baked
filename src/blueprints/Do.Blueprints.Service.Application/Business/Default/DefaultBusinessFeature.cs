﻿using Do.Architecture;
using Do.Domain.Model;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Do.Business.Default;

public class DefaultBusinessFeature(List<Assembly> _assemblies, Assembly _controllerAssembly)
    : IFeature<BusinessConfigurator>
{
    const BindingFlags _defaultMemberBindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;

    public void Configure(LayerConfigurator configurator)
    {
        configurator.ConfigureAssemblyCollection(assemblies =>
        {
            foreach (var assembly in _assemblies)
            {
                assemblies.Add(assembly);
            }
        });

        configurator.ConfigureDomainBuilderOptions(options =>
        {
            options.ConstuctorBindingFlags = _defaultMemberBindingFlags;
            options.MethodBindingFlags = _defaultMemberBindingFlags;
            options.PropertyBindingFlags = _defaultMemberBindingFlags;
        });

        configurator.ConfigureServiceCollection(services =>
        {
            var domainModel = configurator.Context.GetDomainModel();

            foreach (var type in domainModel.Types)
            {
                if (
                    !IsInDomain(type) ||
                    !type.IsPublic ||
                    type.Namespace?.StartsWith("System") == true ||
                    (type.IsSealed && type.IsAbstract) || // if type is static
                    type.IsAbstract ||
                    type.IsValueType ||
                    type.IsGenericMethodParameter ||
                    type.IsGenericTypeParameter ||
                    type.IsAssignableTo<MulticastDelegate>() ||
                    type.IsAssignableTo<Exception>() ||
                    type.IsAssignableTo<Attribute>() ||
                    (type.ContainsGenericParameters && !type.GenericTypeArguments.Any()) ||
                    type.Methods.Contains("<Clone>$") // if type is record
                ) { continue; }

                if (type.Methods.Contains("With"))
                {
                    type.Apply(t =>
                    {
                        services.AddTransientWithFactory(t);
                        type.Interfaces
                            .Where(IsInDomain)
                            .Apply(i => services.AddTransientWithFactory(i, t));
                    });
                }
                else if (type.IsAssignableTo<IScoped>())
                {
                    type.Apply(t =>
                    {
                        services.AddScopedWithFactory(t);
                        type.Interfaces
                            .Where(IsInDomain)
                            .Apply(i => services.AddScopedWithFactory(i, t));
                    });
                }
                else
                {
                    if (type.Properties.Any(p => p.IsPublic)) { continue; }

                    type.Apply(t =>
                        {
                            services.AddSingleton(t);
                            type.Interfaces
                                .Where(IsInDomain)
                                .Apply(i => services.AddSingleton(i, t, forward: true));
                        });
                }
            }

            bool IsInDomain(TypeModel type) => domainModel.Assemblies.ContainsModel(type.Assembly);
        });

        configurator.ConfigureApplicationParts(applicationParts =>
        {
            applicationParts.Add(new(_controllerAssembly));
        });
    }
}
