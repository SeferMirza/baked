﻿using Do.Business;
using Do.Domain.Model;
using Do.RestApi.Configuration;
using Humanizer;

namespace Do.CodingStyle.RichEntity;

public class TargetEntityFromRouteConvention(DomainModel _domain)
    : IApiModelConvention<ParameterModelContext>
{
    public void Apply(ParameterModelContext context)
    {
        if (context.Action.MethodModel?.Has<InitializerAttribute>() == true) { return; }
        if (context.Parameter.IsInvokeMethodParameter) { return; }

        var entityType = context.Parameter.TypeModel;
        if (!entityType.TryGetQueryContextType(_domain, out var queryContextType)) { return; }

        var queryContextParameter = context.Action.AddQueryContextAsService(queryContextType);

        context.Parameter.ConvertToId(name: "id");
        context.MoveParameterToRoute(resourceName: entityType.Name.Pluralize(), constraint: "guid");
        context.Action.FindTargetStatement = queryContextParameter.BuildSingleBy(context.Parameter.Name, fromRoute: true);
    }
}