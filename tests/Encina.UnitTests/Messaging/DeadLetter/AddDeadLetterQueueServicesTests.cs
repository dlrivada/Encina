using Encina.Messaging;
using Encina.Messaging.DeadLetter;
using Encina.Messaging.Health;
using Encina.Testing.Fakes.Stores;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shouldly;

namespace Encina.UnitTests.Messaging.DeadLetter;

/// <summary>
/// Tests for <see cref="MessagingServiceCollectionExtensions.AddDeadLetterQueueServices{TStore, TFactory}"/>.
/// </summary>
public sealed class AddDeadLetterQueueServicesTests
{
    private static readonly ServiceProviderOptions StrictOptions = new() { ValidateOnBuild = true, ValidateScopes = true };

    [Fact]
    public void Disabled_RegistersNothing()
    {
        var services = new ServiceCollection();

        services.AddDeadLetterQueueServices<FakeDeadLetterStore, StubFactory>(false, new DeadLetterOptions());

        services.ShouldBeEmpty();
    }

    [Fact]
    public void Enabled_BuildsWithValidateOnBuildAndResolvesEveryDependency()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDeadLetterQueueServices<FakeDeadLetterStore, StubFactory>(true, new DeadLetterOptions());

        using var provider = services.BuildServiceProvider(StrictOptions);
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IDeadLetterManager>().ShouldBeOfType<DeadLetterManager>();
        scope.ServiceProvider.GetRequiredService<DeadLetterOrchestrator>().ShouldNotBeNull();
        scope.ServiceProvider.GetServices<IEncinaHealthCheck>().ShouldContain(h => h is DeadLetterHealthCheck);
        provider.GetServices<IHostedService>().ShouldContain(h => h is DeadLetterCleanupProcessor);
    }

    [Fact]
    public void Enabled_WithCleanupDisabled_DoesNotRegisterTheCleanupProcessor()
    {
        var services = new ServiceCollection();
        services.AddDeadLetterQueueServices<FakeDeadLetterStore, StubFactory>(
            true, new DeadLetterOptions { EnableAutomaticCleanup = false });

        services.Any(d => d.ImplementationType == typeof(DeadLetterCleanupProcessor)).ShouldBeFalse();
    }

    [Fact]
    public void ApplicationStoreRegisteredFirst_IsKept()
    {
        var services = new ServiceCollection();
        var own = new FakeDeadLetterStore();
        services.AddSingleton<IDeadLetterStore>(own);

        services.AddDeadLetterQueueServices<OtherStore, StubFactory>(true, new DeadLetterOptions());

        services.Count(d => d.ServiceType == typeof(IDeadLetterStore)).ShouldBe(1);
        services.Single(d => d.ServiceType == typeof(IDeadLetterStore)).ImplementationInstance.ShouldBe(own);
    }

    [Fact]
    public void ProviderRegisteredFirst_IsNotReplacedByALaterCallWithOtherTypes()
    {
        var services = new ServiceCollection();
        services.AddDeadLetterQueueServices<FakeDeadLetterStore, StubFactory>(true, new DeadLetterOptions());

        services.AddDeadLetterQueueServices<OtherStore, StubFactory>(true, new DeadLetterOptions());

        services.Single(d => d.ServiceType == typeof(IDeadLetterStore)).ImplementationType.ShouldBe(typeof(FakeDeadLetterStore));
    }

    [Fact]
    public void OptionsOfTheFirstCall_AreKeptByALaterCall()
    {
        var first = new DeadLetterOptions { RetentionPeriod = TimeSpan.FromDays(3) };
        var services = new ServiceCollection();
        services.AddDeadLetterQueueServices<FakeDeadLetterStore, StubFactory>(true, first);

        services.AddDeadLetterQueueServices<OtherStore, StubFactory>(true, new DeadLetterOptions { RetentionPeriod = TimeSpan.FromDays(9) });

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<DeadLetterOptions>().ShouldBeSameAs(first);
        services.Count(d => d.ImplementationType == typeof(DeadLetterCleanupProcessor)).ShouldBe(1);
    }

    [Fact]
    public void NullServices_Throws()
    {
        Should.Throw<ArgumentNullException>(
            () => MessagingServiceCollectionExtensions.AddDeadLetterQueueServices<FakeDeadLetterStore, StubFactory>(
                null!, true, new DeadLetterOptions()));
    }

    [Fact]
    public void NullOptions_Throws()
    {
        Should.Throw<ArgumentNullException>(
            () => new ServiceCollection().AddDeadLetterQueueServices<FakeDeadLetterStore, StubFactory>(true, null!));
    }

    private sealed class StubFactory : IDeadLetterMessageFactory
    {
        public IDeadLetterMessage Create(DeadLetterData data) => throw new NotSupportedException();
    }

    private sealed class OtherStore : IDeadLetterStore
    {
        public Task<LanguageExt.Either<EncinaError, bool>> AddAsync(IDeadLetterMessage message, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<LanguageExt.Either<EncinaError, LanguageExt.Option<IDeadLetterMessage>>> GetAsync(Guid messageId, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<LanguageExt.Either<EncinaError, IEnumerable<IDeadLetterMessage>>> GetMessagesAsync(DeadLetterFilter? filter = null, int skip = 0, int take = 100, bool newestFirst = false, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<LanguageExt.Either<EncinaError, int>> GetCountAsync(DeadLetterFilter? filter = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<LanguageExt.Either<EncinaError, bool>> TryClaimForReplayAsync(Guid messageId, DateTime claimExpiredBeforeUtc, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<LanguageExt.Either<EncinaError, bool>> MarkAsReplayedAsync(Guid messageId, string replayResult, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<LanguageExt.Either<EncinaError, bool>> DeleteAsync(Guid messageId, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<LanguageExt.Either<EncinaError, int>> DeleteManyAsync(DeadLetterFilter filter, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<LanguageExt.Either<EncinaError, int>> DeleteExpiredAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<LanguageExt.Either<EncinaError, LanguageExt.Unit>> SaveChangesAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
