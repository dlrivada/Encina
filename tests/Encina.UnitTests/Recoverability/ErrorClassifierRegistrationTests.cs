using Encina.Messaging.Recoverability;
using Encina.Polly;
using Encina.Recoverability;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Encina.UnitTests.Recoverability;

/// <summary>
/// Registration of <see cref="IErrorClassifier"/> by <c>AddEncinaPolly</c> and <c>AddEncinaRecoverability</c>
/// in any order (#1223, AGENTS.md registration completeness).
/// </summary>
public sealed class ErrorClassifierRegistrationTests
{
    [Retry(MaxAttempts = 2, BaseDelayMs = 1)]
    public sealed record ProbeRequest : IRequest<string>;

    private sealed class CustomClassifier : IErrorClassifier
    {
        public ErrorClassification Classify(EncinaError encinaError, Exception? exception) => ErrorClassification.Permanent;
    }

    private static ServiceProvider Build(IServiceCollection services) =>
        services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

    private static ServiceCollection Base()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        return services;
    }

    [Fact]
    public void AddEncinaPolly_RegistersTheDefaultClassifierAndTheRetryBehaviorResolves()
    {
        var services = Base();
        services.AddEncinaPolly();

        using var provider = Build(services);

        provider.GetRequiredService<IErrorClassifier>().ShouldBeOfType<DefaultErrorClassifier>();
        provider.GetServices<IPipelineBehavior<ProbeRequest, string>>().OfType<RetryPipelineBehavior<ProbeRequest, string>>().ShouldHaveSingleItem();
    }

    [Fact]
    public void AddEncinaPolly_KeepsAClassifierTheApplicationRegisteredFirst()
    {
        var custom = new CustomClassifier();
        var services = Base();
        services.AddSingleton<IErrorClassifier>(custom);
        services.AddEncinaPolly();

        using var provider = Build(services);

        provider.GetRequiredService<IErrorClassifier>().ShouldBeSameAs(custom);
    }

    [Fact]
    public void Recoverability_WithACustomClassifierOption_ReplacesThePollyDefaultInEitherOrder()
    {
        var custom = new CustomClassifier();

        var pollyFirst = Base();
        pollyFirst.AddEncinaPolly();
        pollyFirst.AddEncinaRecoverability(o => { o.EnableDelayedRetries = false; o.ErrorClassifier = custom; });

        var recoverabilityFirst = Base();
        recoverabilityFirst.AddEncinaRecoverability(o => { o.EnableDelayedRetries = false; o.ErrorClassifier = custom; });
        recoverabilityFirst.AddEncinaPolly();

        foreach (var services in new[] { pollyFirst, recoverabilityFirst })
        {
            using var provider = Build(services);
            provider.GetRequiredService<IErrorClassifier>().ShouldBeSameAs(custom);
        }
    }

    [Fact]
    public void Recoverability_WithoutACustomClassifier_KeepsTheRegisteredOne()
    {
        var custom = new CustomClassifier();
        var services = Base();
        services.AddSingleton<IErrorClassifier>(custom);
        services.AddEncinaRecoverability(o => o.EnableDelayedRetries = false);

        using var provider = Build(services);

        provider.GetRequiredService<IErrorClassifier>().ShouldBeSameAs(custom);
    }

    [Fact]
    public void RecoverabilityBehavior_UsesTheClassifierRegisteredInDi()
    {
        var services = Base();
        services.AddEncinaRecoverability<CustomClassifier>(o => o.EnableDelayedRetries = false);

        using var provider = Build(services);
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetServices<IPipelineBehavior<ProbeRequest, string>>()
            .OfType<RecoverabilityPipelineBehavior<ProbeRequest, string>>().ShouldHaveSingleItem();
        scope.ServiceProvider.GetRequiredService<IErrorClassifier>().ShouldBeOfType<CustomClassifier>();
    }

    [Fact]
    public async Task RecoverabilityBehavior_ClassifiesThroughTheDiClassifier()
    {
        // The DI classifier says Permanent for everything: one attempt, no immediate retry.
        var services = Base();
        services.AddEncinaRecoverability<CustomClassifier>(o => { o.EnableDelayedRetries = false; o.ImmediateRetries = 3; });
        using var provider = Build(services);
        using var scope = provider.CreateScope();
        var behavior = scope.ServiceProvider.GetServices<IPipelineBehavior<ProbeRequest, string>>()
            .OfType<RecoverabilityPipelineBehavior<ProbeRequest, string>>().Single();
        var calls = 0;

        await behavior.Handle(
            new ProbeRequest(),
            Substitute.For<IRequestContext>(),
            () => { calls++; return ValueTask.FromResult(LanguageExt.Prelude.Left<EncinaError, string>(EncinaError.New("boom"))); },
            CancellationToken.None);

        calls.ShouldBe(1);
    }
}
