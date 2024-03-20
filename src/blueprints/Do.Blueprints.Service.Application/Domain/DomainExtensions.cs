﻿using Do.Architecture;
using Do.Domain;
using Do.Domain.Model;

namespace Do;

public static class DomainExtensions
{
    public static void AddDomain(this ICollection<ILayer> layers) => layers.Add(new DomainLayer());

    public static DomainModel GetDomainModel(this ApplicationContext source) => source.Get<DomainModel>();

    public static void ConfigureDomainAssemblyCollection(this LayerConfigurator configurator, Action<IDomainAssemblyCollection> configuration) => configurator.Configure(configuration);
    public static void ConfigureDomainTypeCollection(this LayerConfigurator configurator, Action<IDomainTypeCollection> configuration) => configurator.Configure(configuration);
    public static void ConfigureDomainBuilderOptions(this LayerConfigurator configurator, Action<DomainBuilderOptions> configuration) => configurator.Configure(configuration);
    public static void ConfigureDomainMetaData(this LayerConfigurator configurator, Action<DomainMetadataProcessors> configuration) => configurator.Configure(configuration);

    public static void Add<T>(this IDomainTypeCollection source) => source.Add(typeof(T));
}
