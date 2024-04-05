﻿using System.Reflection;
using Do.Architecture;
using Do.RestApi;
using Do.RestApi.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace Do;

public static class RestApiExtensions
{
    public static void AddRestApi(this IList<ILayer> source) => source.Add(new RestApiLayer());

    public static void ConfigureApiModel(this LayerConfigurator source, Action<ApiModel> configuration) => source.Configure(configuration);
    public static void ConfigureApiModelConventions(this LayerConfigurator source, Action<IApiModelConventionCollection> configuration) => source.Configure(configuration);
    public static void ConfigureApplicationParts(this LayerConfigurator source, Action<IApplicationPartCollection> configuration) => source.Configure(configuration);
    public static void ConfigureMvcNewtonsoftJsonOptions(this LayerConfigurator source, Action<MvcNewtonsoftJsonOptions> configuration) => source.Configure(configuration);
    public static void ConfigureSwaggerGenOptions(this LayerConfigurator source, Action<SwaggerGenOptions> configuration) => source.Configure(configuration);
    public static void ConfigureSwaggerOptions(this LayerConfigurator source, Action<SwaggerOptions> configuration) => source.Configure(configuration);
    public static void ConfigureSwaggerUIOptions(this LayerConfigurator source, Action<SwaggerUIOptions> configuration) => source.Configure(configuration);

    public static IMvcBuilder AddApplicationParts(this IMvcBuilder source, IApplicationPartCollection applicationParts)
    {
        foreach (var applicationPart in applicationParts)
        {
            source.AddApplicationPart(applicationPart.Assembly);
        }

        return source;
    }

    public static void Add<T>(this ICollection<Assembly> assemblies) =>
        assemblies.Add(typeof(T).Assembly);

    internal static IMvcBuilder AddNewtonsoftJson(this IMvcBuilder source, MvcNewtonsoftJsonOptions options)
    {
        source.AddNewtonsoftJson();
        source.Services.AddOptions();
        source.Services.AddSingleton<IOptions<MvcNewtonsoftJsonOptions>>(sp => new OptionsWrapper<MvcNewtonsoftJsonOptions>(options));

        return source;
    }
}