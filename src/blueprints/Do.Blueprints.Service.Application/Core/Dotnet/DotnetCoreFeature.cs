using Do.Architecture;
using Microsoft.Extensions.DependencyInjection;

namespace Do.Core.Dotnet;

public class DotnetCoreFeature : IFeature<CoreConfigurator>
{
    public void Configure(LayerConfigurator configurator)
    {
        configurator.ConfigureServiceCollection(services =>
        {
            services.AddSingleton(TimeProvider.System);
        });
    }
}