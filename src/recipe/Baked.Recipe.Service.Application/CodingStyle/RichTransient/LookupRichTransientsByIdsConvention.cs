﻿using Baked.Business;
using Baked.Domain.Configuration;
using Baked.RestApi.Model;
using Humanizer;

namespace Baked.CodingStyle.RichTransient;

public class LookupRichTransientsByIdsConvention : IDomainModelConvention<ParameterModelContext>
{
    public void Apply(ParameterModelContext context)
    {
        if (!context.Method.TryGetSingle<ActionModelAttribute>(out var action)) { return; }
        if (!context.Parameter.TryGetSingle<ParameterModelAttribute>(out var parameter)) { return; }
        if (!parameter.IsInvokeMethodParameter) { return; }
        if (!context.Parameter.ParameterType.TryGetElementType(out var elementType)) { return; }
        if (!elementType.GetMetadata().Has<LocatableAttribute>()) { return; }

        var initializer = elementType.GetMembers().Methods.Having<InitializerAttribute>().Single();
        if (!initializer.DefaultOverload.Parameters.TryGetValue("id", out var idParameter)) { return; }

        var factoryParameter = action.AddFactoryAsService(elementType);
        var actionShouldBeAsync = initializer.DefaultOverload.ReturnType.IsAssignableTo<Task>();

        parameter.Type = $"IEnumerable<{idParameter.ParameterType.CSharpFriendlyFullName}>";
        parameter.Name = $"{context.Parameter.Name.Singularize()}Ids";
        parameter.LookupRenderer = p => elementType.BuildInitializerByIds(p,
            isArray: context.Parameter.ParameterType.IsArray
        );

        if (actionShouldBeAsync)
        {
            action.MakeAsync();
        }
    }
}