using Encina.Messaging;
using Encina.Messaging.DeadLetter;
using Encina.Messaging.Serialization;
using Encina.Messaging.Tenancy;
using Encina.Tenancy;
using Encina.Testing.Fakes;
using Encina.Testing.Fakes.Models;
using Encina.Testing.Fakes.Stores;
using Encina.Testing.Shouldly;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Messaging.DeadLetter;

/// <summary>
/// The input rules the orchestrator applies before any I/O (UTC instants, trimmed identity values), the
/// tenancy marker registration and the fake store's clock.
/// </summary>
public sealed class DeadLetterInputRulesAndRegistrationTests
{
    private static readonly DateTime FixedUtcNow = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private static (DeadLetterOrchestrator Orchestrator, IDeadLetterStore Store, IDeadLetterMessageFactory Factory) NewOrchestrator()
    {
        var store = Substitute.For<IDeadLetterStore>();
        store.AddAsync(Arg.Any<IDeadLetterMessage>(), Arg.Any<CancellationToken>()).Returns(Right<EncinaError, bool>(true));
        store.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Right<EncinaError, Unit>(unit));
        var factory = Substitute.For<IDeadLetterMessageFactory>();
        var orchestrator = new DeadLetterOrchestrator(
            store,
            factory,
            new DeadLetterOptions(),
            NullLogger<DeadLetterOrchestrator>.Instance,
            new JsonMessageSerializer(),
            Substitute.For<IRequestContextAccessor>(),
            new FakeTimeProvider(new DateTimeOffset(FixedUtcNow)));
        return (orchestrator, store, factory);
    }

    private static DeadLetterContext Context(
        DateTime? firstFailedAtUtc = null,
        string? sourceMessageId = "order-1",
        string? tenantId = null,
        string sourcePattern = "Outbox")
        => new(
            EncinaErrors.Create("x.failed", "failure"),
            Exception: null,
            SourcePattern: sourcePattern,
            TotalRetryAttempts: 1,
            FirstFailedAtUtc: firstFailedAtUtc ?? FixedUtcNow,
            SourceMessageId: sourceMessageId,
            TenantId: tenantId);

    public sealed record Probe(int Value);

    // ------------------------------------------------------------------ UTC instants

    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public async Task AddAsync_FirstFailedAtWithoutUtcKind_ThrowsBeforeAnyStoreCall(DateTimeKind kind)
    {
        var (orchestrator, store, factory) = NewOrchestrator();
        var instant = DateTime.SpecifyKind(new DateTime(2026, 1, 1, 8, 0, 0), kind);

        var ex = await Should.ThrowAsync<ArgumentException>(
            () => orchestrator.AddAsync(new Probe(1), Context(firstFailedAtUtc: instant)));

        ex.ParamName.ShouldBe("firstFailedAtUtc");
        factory.ReceivedCalls().ShouldBeEmpty();
        store.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task AddAsync_FirstFailedAtUtc_IsAccepted()
    {
        var (orchestrator, _, _) = NewOrchestrator();

        (await orchestrator.AddAsync(new Probe(1), Context())).IsRight.ShouldBeTrue();
    }

    // ------------------------------------------------------------------ trimmed identity values

    [Theory]
    [InlineData(" order-1", "sourceMessageId")]
    [InlineData("order-1 ", "sourceMessageId")]
    [InlineData("order-1\n", "sourceMessageId")]
    public async Task AddAsync_SourceMessageIdWithEdgeWhiteSpace_ThrowsBeforeAnyStoreCall(string value, string parameter)
    {
        var (orchestrator, store, factory) = NewOrchestrator();

        var ex = await Should.ThrowAsync<ArgumentException>(
            () => orchestrator.AddAsync(new Probe(1), Context(sourceMessageId: value)));

        ex.ParamName.ShouldBe(parameter);
        factory.ReceivedCalls().ShouldBeEmpty();
        store.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task AddAsync_TenantIdWithTrailingSpace_Throws()
    {
        var (orchestrator, _, _) = NewOrchestrator();

        var ex = await Should.ThrowAsync<ArgumentException>(
            () => orchestrator.AddAsync(new Probe(1), Context(tenantId: "tenant-a ")));

        ex.ParamName.ShouldBe("tenantId");
    }

    [Fact]
    public async Task AddAsync_SourcePatternWithLeadingSpace_Throws()
    {
        var (orchestrator, _, _) = NewOrchestrator();

        var ex = await Should.ThrowAsync<ArgumentException>(
            () => orchestrator.AddAsync(new Probe(1), Context(sourcePattern: " Outbox")));

        ex.ParamName.ShouldBe("sourcePattern");
    }

    [Fact]
    public async Task AddAsync_InnerSpacesInIdentityValues_AreAccepted()
    {
        var (orchestrator, _, _) = NewOrchestrator();

        (await orchestrator.AddAsync(new Probe(1), Context(sourceMessageId: "order 1", tenantId: "tenant a"))).IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task AddFromFailedMessageAsync_FirstAttemptWithoutUtcKind_Throws()
    {
        var (orchestrator, _, _) = NewOrchestrator();
        var failed = new global::Encina.Messaging.Recoverability.FailedMessage
        {
            Id = Guid.NewGuid(),
            RequestType = typeof(Probe).AssemblyQualifiedName!,
            Request = new Probe(1),
            Error = EncinaErrors.Create("x.failed", "failure"),
            TotalAttempts = 1,
            ImmediateRetryAttempts = 0,
            DelayedRetryAttempts = 0,
            FirstAttemptAtUtc = new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Unspecified),
            FailedAtUtc = FixedUtcNow
        };

        await Should.ThrowAsync<ArgumentException>(() => orchestrator.AddFromFailedMessageAsync(failed, "Recoverability"));
    }

    // ------------------------------------------------------------------ tenancy marker

    [Fact]
    public void AddEncinaTenancy_RegistersTheTenancyMarker()
    {
        var services = new ServiceCollection();

        services.AddEncinaTenancy();

        using var provider = services.BuildServiceProvider();
        provider.GetService<TenancyInUse>().ShouldBeSameAs(TenancyInUse.Instance);
    }

    [Fact]
    public async Task Manager_ResolvedWithTenancyRegistered_DeniesWhenNoTenantIsResolved()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaTenancy();
        services.AddDeadLetterQueueServices<FakeDeadLetterStore, StubFactory>(true, new DeadLetterOptions());

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        using var scope = provider.CreateScope();
        var manager = scope.ServiceProvider.GetRequiredService<IDeadLetterManager>();

        (await manager.GetCountAsync()).ShouldBeErrorWithCode(DeadLetterErrorCodes.TenantRequired);
        (await manager.GetCountAsync(new DeadLetterFilter { AllTenants = true })).ShouldBeRight().ShouldBe(0);
    }

    [Fact]
    public async Task Manager_ResolvedWithoutTenancy_CountsTheWholeQueue()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDeadLetterQueueServices<FakeDeadLetterStore, StubFactory>(true, new DeadLetterOptions());

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        using var scope = provider.CreateScope();

        (await scope.ServiceProvider.GetRequiredService<IDeadLetterManager>().GetCountAsync()).ShouldBeRight().ShouldBe(0);
    }

    // ------------------------------------------------------------------ fake store clock

    [Fact]
    public async Task AddFakeDeadLetterStore_UsesTheTimeProviderRegisteredInTheContainer()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(FixedUtcNow));
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(clock);
        services.AddFakeDeadLetterStore();
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });
        var store = provider.GetRequiredService<IDeadLetterStore>();
        var message = new FakeDeadLetterMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "t",
            RequestContent = "{}",
            ErrorCode = "e",
            SourcePattern = "Outbox",
            SourceMessageId = "m-1",
            DeadLetteredAtUtc = FixedUtcNow,
            FirstFailedAtUtc = FixedUtcNow
        };
        (await store.AddAsync(message)).ShouldBeRight();

        // Claimed at the container's clock, a claim made at FixedUtcNow is live until the clock moves past it.
        (await store.TryClaimForReplayAsync(message.Id, FixedUtcNow.AddMinutes(-5))).ShouldBeRight().ShouldBeTrue();
        (await store.TryClaimForReplayAsync(message.Id, FixedUtcNow.AddMinutes(-5))).ShouldBeRight().ShouldBeFalse();
        clock.Advance(TimeSpan.FromMinutes(10));
        (await store.TryClaimForReplayAsync(message.Id, FixedUtcNow.AddMinutes(5))).ShouldBeRight().ShouldBeTrue();
    }

    [Fact]
    public void AddFakeDeadLetterStore_WithoutATimeProvider_ResolvesTheStoreAndTheConcreteTypeAsOneInstance()
    {
        var services = new ServiceCollection();
        services.AddFakeDeadLetterStore();
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });

        provider.GetRequiredService<IDeadLetterStore>().ShouldBeSameAs(provider.GetRequiredService<FakeDeadLetterStore>());
    }

    private sealed class StubFactory : IDeadLetterMessageFactory
    {
        public IDeadLetterMessage Create(DeadLetterData data) => throw new NotSupportedException();
    }
}
