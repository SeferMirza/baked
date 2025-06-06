﻿using Baked.Domain.Configuration;
using Baked.RestApi.Model;
using Humanizer;

namespace Baked.RestApi.Conventions;

public class RemoveFromRouteConvention(IEnumerable<string> _parts,
    Func<ActionModelAttribute, bool>? _when = default,
    Func<MethodModelContext, bool>? _whenContext = default
) : IDomainModelConvention<MethodModelContext>
{
    public void Apply(MethodModelContext context)
    {
        if (!context.Method.TryGetSingle<ActionModelAttribute>(out var action)) { return; }
        if (_when is not null && !_when(action)) { return; }
        if (_whenContext is not null && !_whenContext(context)) { return; }

        for (var i = 0; i < action.RouteParts.Count; i++)
        {
            var routePart = RemoveParts(action.RouteParts[i], _parts);
            action.RouteParts[i] = routePart;
            if (string.IsNullOrWhiteSpace(routePart))
            {
                action.RouteParts.RemoveAt(i);
                i--;
            }
        }

        action.Name = RemoveParts(action.Name, _parts);
    }

    string RemoveParts(string from, IEnumerable<string> parts) =>
        from.Humanize().Split(" ").Select(w => w.Pascalize()).Except(parts).Join();
}