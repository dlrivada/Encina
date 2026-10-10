using Encina.Marten;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace Encina.UnitTests.Marten;

/// <summary>
/// Registration-completeness tests (AGENTS.md section 3) for
/// <see cref="EventPublishingPipelineBehavior{TRequest, TResponse}"/>: <c>AddEncinaMarten</c> must put it in
/// the resolved pipeline of a command when <see cref="EncinaMartenOptions.AutoPublishDomainEvents"/> is on.
/// </summary>
public class EventPublishingRegistrationTests
{
    private static ServiceProvider BuildProvider(Action<EncinaMartenOptions>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncina();
        services.AddScoped(_ => Substitute.For<IDocumentSession>());
        services.AddSingleton(Substitute.For<IDocumentStore>());

        if (configure is null)
        {
            services.AddEncinaMarten();
        }
        else
        {
            services.AddEncinaMarten(configure);
        }

        return services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    [Fact]
    public void AddEncinaMarten_DefaultOptions_BehaviorIsInCommandPipeline()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        var behaviors = scope.ServiceProvider
            .GetServices<IPipelineBehavior<RecordCommand, Guid>>()
            .ToList();

        behaviors.ShouldContain(b => b is EventPublishingPipelineBehavior<RecordCommand, Guid>);
    }

    [Fact]
    public void AddEncinaMarten_AutoPublishDisabled_BehaviorIsNotInPipeline()
    {
        using var provider = BuildProvider(o => o.AutoPublishDomainEvents = false);
        using var scope = provider.CreateScope();

        var behaviors = scope.ServiceProvider
            .GetServices<IPipelineBehavior<RecordCommand, Guid>>()
            .ToList();

        behaviors.ShouldNotContain(b => b is EventPublishingPipelineBehavior<RecordCommand, Guid>);
    }

    [Fact]
    public void AddEncinaMarten_CalledTwice_RegistersBehaviorOnce()
    {
        var services = new ServiceCollection();

        services.AddEncinaMarten();
        services.AddEncinaMarten();

        services.Count(d => d.ImplementationType == typeof(EventPublishingPipelineBehavior<,>)).ShouldBe(1);
    }

    [Fact]
    public void AddEncinaMarten_QueryRequest_BehaviorIsSkippedBecauseItIsNotACommand()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        var behaviors = scope.ServiceProvider
            .GetServices<IPipelineBehavior<RecordQuery, Guid>>()
            .ToList();

        behaviors.ShouldNotContain(b =>
            b.GetType().IsGenericType &&
            b.GetType().GetGenericTypeDefinition() == typeof(EventPublishingPipelineBehavior<,>));
    }

    public sealed record RecordCommand : ICommand<Guid>;

    public sealed record RecordQuery : IQuery<Guid>;
}
