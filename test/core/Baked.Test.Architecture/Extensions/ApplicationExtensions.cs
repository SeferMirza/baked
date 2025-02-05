﻿using Baked.Architecture;
using Baked.Testing;

namespace Baked.Test;

public static class ApplicationExtensions
{
    public static Application AnApplication(this Stubber giveMe,
        IPhase? phase = default,
        IPhase[]? phases = default,
        ILayer? layer = default,
        ILayer[]? layers = default,
        IFeature? feature = default,
        IFeature[]? features = default,
        ApplicationContext? bakeContext = default,
        ApplicationContext? context = default,
        RunFlags runFlags = RunFlags.Start
    )
    {
        layers ??= [layer ?? giveMe.Spec.MockMe.ALayer(phase: phase, phases: phases)];
        features ??= [feature ?? giveMe.Spec.MockMe.AFeature()];

        return giveMe.ABake(context: context, bakeContext: bakeContext, runflags: runFlags).Application(app =>
        {
            app.Layers.AddRange(layers);
            app.Features.AddRange(features);
        });
    }
}