﻿using Baked.Architecture;

namespace Baked.Lifetime.Transient;

public class TransientLifetimeFeature : IFeature<LifetimeConfigurator>
{
    public void Configure(LayerConfigurator configurator)
    {
        configurator.ConfigureDomainModelBuilder(builder =>
        {
            builder.Index.Type.Add<TransientAttribute>();
        });

        configurator.ConfigureDomainServiceCollection(services =>
        {
            var domain = configurator.Context.GetDomainModel();
            foreach (var transient in domain.Types.Having<TransientAttribute>())
            {
                services.AddTransient(transient, useFactory: true);
            }
        });
    }
}