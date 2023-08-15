﻿using System.Reflection;
using Do.Architecture;
using Do.Branding;
using Do.Testing;

namespace Do.Test;

public static class ArchitectureSpecExtensions
{
    #region Forge

    public static Forge AForge(this Stubber giveMe,
        IBanner? banner = default,
        ApplicationContext? context = default
    )
    {
        banner ??= giveMe.Spec.MockMe.ABanner();
        context ??= new();

        return new(banner, () => new(context));
    }

    #endregion

    #region Application

    public static Application AnApplication(this Stubber giveMe,
        ILayer? layer = default,
        ILayer[]? layers = default,
        IFeature? feature = default,
        IFeature[]? features = default,
        ApplicationContext? context = default
    )
    {
        layers ??= new[] { layer ?? giveMe.Spec.MockMe.ALayer() };
        features ??= new[] { feature ?? giveMe.Spec.MockMe.AFeature() };

        return giveMe.AForge(context: context).Application(app =>
        {
            app.Layers.AddRange(layers);
            app.Features.AddRange(features);
        });
    }

    #endregion

    #region ApplicationContext

    public static ApplicationContext AnApplicationContext(this Stubber _) => new();
    public static ApplicationContext AnApplicationContext<T>(this Stubber giveMe, T content) where T : notnull
    {
        var result = giveMe.AnApplicationContext();

        result.Add(content);

        return result;
    }

    public static void ShouldHave<T>(this ApplicationContext context, T value)
    {
        context.Has<T>().ShouldBeTrue($"Context should have an item with type {typeof(T)}");
        context.Get<T>().ShouldBe(value);
    }

    public static void ShouldNotHave<T>(this ApplicationContext context, T value)
    {
        if (context.Has<T>())
        {
            context.Get<T>().ShouldNotBe(value);
        }
    }

    #endregion

    #region Banner

    public static IBanner ABanner(this Mocker _) =>
        new Mock<IBanner>().Object;

    public static void VerifyPrinted(this IBanner source) =>
        Mock.Get(source).Verify(b => b.Print());

    #endregion

    #region Layer

    public static ILayer ALayer(this Mocker mockMe,
        object? target = default,
        object[]? targets = default,
        PhaseContext? phaseContext = default,
        IPhase? phase = default,
        IPhase[]? phases = default,
        Action? onApplyPhase = default
    )
    {
        phaseContext ??= mockMe.Spec.GiveMe.APhaseContext(target: target, targets: targets);
        phases ??= new[] { phase ?? mockMe.APhase() };

        var result = new Mock<ILayer>();

        result.Setup(l => l.GetPhases()).Returns(phases);

        var setupGetContext = result
            .Setup(l => l.GetContext(It.IsAny<IPhase>(), It.IsAny<ApplicationContext>()))
            .Returns(phaseContext);

        if (onApplyPhase != default)
        {
            setupGetContext.Callback((IPhase _, ApplicationContext _) => onApplyPhase());
        }

        return result.Object;
    }

    public static void VerifyInitialized(this ILayer source) =>
        Mock.Get(source).Verify(l => l.GetPhases());

    public static void VerifyApplied(this ILayer source, IPhase phase) =>
        Mock.Get(source)
            .Verify(l => l.GetContext(
                phase,
                It.IsAny<ApplicationContext>()
            ));

    #endregion

    #region LayerConfigurator

    public static LayerConfigurator ALayerConfigurator<TTarget>(this Stubber giveMe,
        TTarget? configuration = default
    ) where TTarget : notnull
    {
        configuration ??= giveMe.AnInstanceOf<TTarget>();

        return LayerConfigurator.Create<TTarget>(configuration);
    }

    #endregion

    #region Phase

    public static IPhase APhase(this Mocker _,
        Func<bool>? isReady = default,
        Action? onInitialize = default,
        PhaseOrder order = PhaseOrder.Normal
    )
    {
        isReady ??= () => true;
        onInitialize ??= () => { };

        var result = new Mock<IPhase>();

        result.Setup(p => p.Order).Returns(order);

        result
            .Setup(p => p.IsReady(It.IsAny<ApplicationContext>()))
            .Returns((ApplicationContext _) => isReady());

        result
            .Setup(p => p.Initialize(It.IsAny<ApplicationContext>()))
            .Callback((ApplicationContext _) => onInitialize());

        return result.Object;
    }

    public static void VerifyInitialized(this IPhase source) =>
        Mock.Get(source).Verify(p => p.Initialize(It.IsAny<ApplicationContext>()));

    public static void VerifyInitialized(this IPhase source, ApplicationContext context) =>
        Mock.Get(source).Verify(p => p.Initialize(context));

    #endregion

    #region PhaseContext

    public static PhaseContext APhaseContext(this Stubber giveMe,
        object? target = default,
        object[]? targets = default,
        Action? onDispose = default
    )
    {
        targets ??= new[] { target ?? new() };
        onDispose ??= () => { };

        var configurators = new List<LayerConfigurator>();
        foreach (var t in targets)
        {
            var create = typeof(LayerConfigurator)
                .GetMethods(BindingFlags.Static | BindingFlags.Public)
                .FirstOrDefault(c => c.Name == nameof(LayerConfigurator.Create) && c.GetGenericArguments().Length == 1);
            create.ShouldNotBeNull();

            var configurator = create.MakeGenericMethod(t.GetType()).Invoke(null, new[] { t });
            configurator.ShouldNotBeNull();

            configurators.Add((LayerConfigurator)configurator);
        }

        return new(configurators) { OnDispose = onDispose };
    }

    public static void ShouldConfigureTarget<TTarget>(this PhaseContext source, TTarget expected)
    {
        var configured = false;
        foreach (var configurator in source.Configurators)
        {
            configurator.Configure((TTarget actual) =>
            {
                actual.ShouldBe(expected);

                configured = true;
            });
        }

        configured.ShouldBeTrue("Phase context didn't get configured");
    }

    public static void ShouldConfigureTwoTargets<TTarget1, TTarget2>(this PhaseContext source, TTarget1 expected1, TTarget2 expected2)
    {
        var configured = false;
        foreach (var configurator in source.Configurators)
        {
            configurator.Configure((TTarget1 actual1, TTarget2 actual2) =>
            {
                actual1.ShouldBe(expected1);
                actual2.ShouldBe(expected2);

                configured = true;
            });
        }

        configured.ShouldBeTrue("Phase context didn't get configured");
    }

    public static void ShouldConfigureThreeTargets<TTarget1, TTarget2, TTarget3>(this PhaseContext source, TTarget1 expected1, TTarget2 expected2, TTarget3 expected3)
    {
        var configured = false;
        foreach (var configurator in source.Configurators)
        {
            configurator.Configure((TTarget1 actual1, TTarget2 actual2, TTarget3 actual3) =>
            {
                actual1.ShouldBe(expected1);
                actual2.ShouldBe(expected2);
                actual3.ShouldBe(expected3);

                configured = true;
            });
        }

        configured.ShouldBeTrue("Phase context didn't get configured");
    }

    public static void ShouldAddValueToContextOnDispose<T>(this PhaseContext source, T value, ApplicationContext context)
    {
        using (source)
        {
            context.ShouldNotHave(value);
        }

        context.ShouldHave(value);
    }

    #endregion

    #region Feature

    public static IFeature AFeature(this Mocker _) =>
        new Mock<IFeature>().Object;

    public static void VerifyInitialized(this IFeature source) =>
        Mock.Get(source).Verify(f => f.Configure(It.IsAny<LayerConfigurator>()));

    public static void VerifyConfigures<TTarget>(this IFeature source, TTarget target) where TTarget : notnull =>
        Mock.Get(source).Verify(f => f.Configure(LayerConfigurator.Create(target)));

    public static void VerifyConfiguresNothing(this IFeature source) =>
        Mock.Get(source).Verify(f => f.Configure(It.IsAny<LayerConfigurator>()), Times.Never());

    #endregion
}
