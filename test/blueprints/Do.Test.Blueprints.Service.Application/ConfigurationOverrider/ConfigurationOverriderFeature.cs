﻿using Do.Architecture;
using Do.Authentication.FixedToken;
using Microsoft.OpenApi.Models;

namespace Do.Test.ConfigurationOverrider;

public class ConfigurationOverriderFeature : IFeature
{
    public void Configure(LayerConfigurator configurator)
    {
        configurator.ConfigureAutoPersistenceModel(model =>
        {
            model.Override<Entity>(x => x.Map(e => e.String).Length(500));
        });

        configurator.ConfigureSwaggerGenOptions(swaggerGenOptions =>
        {
            swaggerGenOptions.AddSecurityDefinition("AdditionalSecurity",
                new()
                {
                    Type = SecuritySchemeType.ApiKey,
                    In = ParameterLocation.Header,
                    Name = "X-Secret",
                    Description = "Enter secret information",
                }
            );

            swaggerGenOptions.AddSecurityRequirementToOperationsThatUse<Middleware>("AdditionalSecurity");
            swaggerGenOptions.AddParameterToOperationsThatUse<Middleware>("X-Security", @in: ParameterLocation.Header, required: true);
        });

        configurator.ConfigureApiModel(api =>
        {
            api.Controllers.Add(new("TestController")
            {
                Actions = [new("TestAction")]
            });
        });

        configurator.ConfigureGeneratedAssemblyCollection(assemblies =>
        {
            assemblies.Add(
                "Controllers",
                assembly => assembly
                    .AddReferenceFrom<Program>()
                    .AddCodes(
                        Codes.Entities.Code,
                        Codes.Parents.Code,
                        Codes.Remote.Code,
                        Codes.Singleton.Code
                    ),
                compilationOptions => compilationOptions
                    .WithUsings(
                        "System",
                        "System.Linq",
                        "System.Collections",
                        "System.Collections.Generic",
                        "System.Threading.Tasks"
                    )
            );
        });

        configurator.ConfigureApplicationParts(applicationParts =>
        {
            applicationParts.Add(new(configurator.Context.GetGeneratedAssembly("Controllers")));
        });
    }
}
