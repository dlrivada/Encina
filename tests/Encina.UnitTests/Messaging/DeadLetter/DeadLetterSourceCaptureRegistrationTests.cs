using System.Data;
using System.Reflection;
using Encina.Messaging;
using Encina.Messaging.DeadLetter;
using Encina.Messaging.Inbox;
using Encina.Messaging.Outbox;
using Encina.Messaging.Recoverability;
using Encina.Messaging.Sagas;
using Encina.Messaging.Scheduling;
using Encina.Testing.Fakes.Stores;
using Encina.UnitTests.Messaging.Sagas;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Shouldly;

namespace Encina.UnitTests.Messaging.DeadLetter;

/// <summary>
/// With every <c>IntegrateWith*</c> flag on, the dead letter queue and the five sources build under
/// <see cref="ServiceProviderOptions.ValidateOnBuild"/> and <see cref="ServiceProviderOptions.ValidateScopes"/>,
/// and every source receives the one <see cref="DeadLetterSourceCapture"/>, whichever is registered first (#1991).
/// </summary>
public sealed class DeadLetterSourceCaptureRegistrationTests
{
    public sealed record ProbeCommand(int Value) : IRequest<int>;

    public enum Order
    {
        SameConfiguration,
        DeadLetterQueueFirst,
        DeadLetterQueueAfter
    }

    public static IEnumerable<object[]> Cases()
    {
        foreach (var provider in AdoDapperMessagingDiRegistrationTests.ProviderCases())
        {
            foreach (var order in Enum.GetValues<Order>())
            {
                yield return [provider[0], provider[1], order];
            }
        }
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void AllFlagsOn_EverySourceGetsTheCapture(
        string providerName,
        Action<IServiceCollection, Action<MessagingConfiguration>> register,
        Order order)
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<IDbConnection>());
        services.AddScoped(_ => Substitute.For<IDelayedRetryStore>());
        services.AddScoped(_ => Substitute.For<IDelayedRetryMessageFactory>());

        // Act
        if (order == Order.DeadLetterQueueFirst)
            AddDeadLetterQueue(services);

        register(services, config =>
        {
            config.UseOutbox = true;
            config.UseInbox = true;
            config.UseSagas = true;
            config.UseScheduling = true;
            config.UseRecoverability = true;
            config.UseDeadLetterQueue = order == Order.SameConfiguration;
        });

        if (order == Order.DeadLetterQueueAfter)
            AddDeadLetterQueue(services);

        // Assert
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        using var scope = provider.CreateScope();
        var capture = provider.GetRequiredService<DeadLetterSourceCapture>();
        var options = provider.GetRequiredService<DeadLetterOptions>();
        new[] { options.IntegrateWithRecoverability, options.IntegrateWithOutbox, options.IntegrateWithInbox, options.IntegrateWithScheduling, options.IntegrateWithSagas }
            .ShouldAllBe(flag => flag, providerName);

        CaptureOf(scope.ServiceProvider.GetRequiredService<OutboxOrchestrator>()).ShouldBeSameAs(capture, providerName);
        CaptureOf(scope.ServiceProvider.GetRequiredService<InboxOrchestrator>()).ShouldBeSameAs(capture, providerName);
        CaptureOf(scope.ServiceProvider.GetRequiredService<SchedulerOrchestrator>()).ShouldBeSameAs(capture, providerName);
        CaptureOf(scope.ServiceProvider.GetRequiredService<SagaOrchestrator>()).ShouldBeSameAs(capture, providerName);
        CaptureOf(scope.ServiceProvider.GetRequiredService<ISagaNotFoundDispatcher>()).ShouldBeSameAs(capture, providerName);
        CaptureOf(scope.ServiceProvider.GetServices<IPipelineBehavior<ProbeCommand, int>>()
            .OfType<RecoverabilityPipelineBehavior<ProbeCommand, int>>().Single()).ShouldBeSameAs(capture, providerName);
        CaptureOf(provider.GetServices<IHostedService>().OfType<DelayedRetryProcessor>().Single()).ShouldBeSameAs(capture, providerName);
    }

    [Fact]
    public void WithoutTheDeadLetterQueue_TheSourcesResolveWithoutACapture()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<IDbConnection>());

        // Act
        global::Encina.ADO.SqlServer.ServiceCollectionExtensions.AddEncinaADO(services, config =>
        {
            config.UseOutbox = true;
            config.UseInbox = true;
            config.UseSagas = true;
            config.UseScheduling = true;
        });

        // Assert
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        using var scope = provider.CreateScope();
        provider.GetService<DeadLetterSourceCapture>().ShouldBeNull();
        CaptureOf(scope.ServiceProvider.GetRequiredService<SagaOrchestrator>()).ShouldBeNull();
        CaptureOf(scope.ServiceProvider.GetRequiredService<InboxOrchestrator>()).ShouldBeNull();
    }

    private static void AddDeadLetterQueue(IServiceCollection services)
        => services.AddEncinaDeadLetterQueue<FakeDeadLetterStore, DeadLetterCaptureHost.PassThroughFactory>();

    // The capture a source was constructed with (its private field), so the test proves the DI wiring.
    private static object? CaptureOf(object source)
        => source.GetType()
            .GetField("_deadLetterCapture", BindingFlags.Instance | BindingFlags.NonPublic)
            .ShouldNotBeNull()
            .GetValue(source);
}
