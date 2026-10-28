using Encina.Audit.Marten;
using Encina.Audit.Marten.Crypto;
using Encina.IntegrationTests.Infrastructure.Marten.Fixtures;

using Marten;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

using Shouldly;

namespace Encina.IntegrationTests.Infrastructure.Marten.Audit;

/// <summary>
/// The contract of <see cref="ITemporalKeyProvider.DestroyKeysBeforeAsync"/> (issue #2770): every implementation
/// destroys the keys of the elapsed periods whatever the destruction reference (erasure, retention or none), rejects
/// an invalid reference with the matching error code without destroying anything, and skips a period already destroyed.
/// The Marten leg needs PostgreSQL through Testcontainers, so the contract lives in the integration project
/// (<c>Encina.ContractTests</c> has no container fixture and does not reference <c>Encina.Audit.Marten</c>).
/// </summary>
public abstract class TemporalKeyProviderContractTests
{
    private const string Period = "1800-03";
    private static readonly DateTime Cutoff = new(1801, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTimeOffset At = new(2026, 10, 1, 8, 0, 0, TimeSpan.Zero);
    protected static readonly DateTimeOffset KeyCreationTime = new(1800, 3, 15, 0, 0, 0, TimeSpan.Zero);

    protected abstract ITemporalKeyProvider CreateProvider();

    public static TheoryData<string> ValidReferences() => ["erasure", "retention", "none"];

    private static KeyDestructionReference? Reference(string kind) => kind switch
    {
        "erasure" => new ErasureReference("erasure-1", At, "dpo"),
        "retention" => new RetentionReference("audit-retention:P2555D", At.AddYears(-7), At, "system"),
        _ => null
    };

    [Theory]
    [MemberData(nameof(ValidReferences))]
    public async Task Destroy_WithAnyValidReference_DestroysTheElapsedPeriodAndSkipsItAfterwards(string kind)
    {
        var sut = CreateProvider();
        (await sut.GetOrCreateKeyAsync(Period)).IsRight.ShouldBeTrue();

        var first = await sut.DestroyKeysBeforeAsync(Cutoff, TemporalKeyGranularity.Monthly, Reference(kind));
        var second = await sut.DestroyKeysBeforeAsync(Cutoff, TemporalKeyGranularity.Monthly, Reference(kind));

        first.IsRight.ShouldBeTrue();
        ((int)first).ShouldBe(1);
        second.IsRight.ShouldBeTrue();
        ((int)second).ShouldBe(0);
        var destroyed = await sut.IsKeyDestroyedAsync(Period);
        destroyed.IsRight.ShouldBeTrue();
        ((bool)destroyed).ShouldBeTrue();
        (await sut.GetKeyAsync(Period, 1)).IsLeft.ShouldBeTrue();
        (await sut.GetOrCreateKeyAsync(Period)).IsLeft.ShouldBeTrue("a destroyed period is never re-created");
    }

    [Theory]
    [InlineData("erasure")]
    [InlineData("retention")]
    public async Task Destroy_WithAnInvalidReference_ReturnsTheMatchingErrorAndDestroysNothing(string kind)
    {
        var sut = CreateProvider();
        (await sut.GetOrCreateKeyAsync(Period)).IsRight.ShouldBeTrue();
        KeyDestructionReference invalid = kind == "erasure"
            ? new ErasureReference(" ", At, "dpo")
            : new RetentionReference("policy", default, At, "system");

        var result = await sut.DestroyKeysBeforeAsync(Cutoff, TemporalKeyGranularity.Monthly, invalid);

        var expectedCode = kind == "erasure"
            ? MartenAuditErrors.InvalidErasureReferenceCode
            : MartenAuditErrors.InvalidRetentionReferenceCode;
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.GetCode().IfNone(string.Empty).ShouldBe(expectedCode));
        (await sut.GetKeyAsync(Period, 1)).IsRight.ShouldBeTrue("nothing may be destroyed with an invalid reference");
    }

    [Fact]
    public async Task Destroy_KeepsPeriodsNotOlderThanTheCutoff()
    {
        var sut = CreateProvider();
        (await sut.GetOrCreateKeyAsync(Period)).IsRight.ShouldBeTrue();

        var result = await sut.DestroyKeysBeforeAsync(new DateTime(1799, 1, 1, 0, 0, 0, DateTimeKind.Utc), TemporalKeyGranularity.Monthly);

        result.IsRight.ShouldBeTrue();
        ((int)result).ShouldBe(0);
        (await sut.GetKeyAsync(Period, 1)).IsRight.ShouldBeTrue();
    }
}

/// <summary>The contract against <see cref="InMemoryTemporalKeyProvider"/>.</summary>
[Trait("Category", "Integration")]
public sealed class InMemoryTemporalKeyProviderContractTests : TemporalKeyProviderContractTests
{
    protected override ITemporalKeyProvider CreateProvider() =>
        new InMemoryTemporalKeyProvider(new FakeTimeProvider(KeyCreationTime), NullLogger<InMemoryTemporalKeyProvider>.Instance);
}

/// <summary>The contract against <see cref="MartenTemporalKeyProvider"/> on real PostgreSQL.</summary>
[Collection(MartenCollection.Name)]
[Trait("Category", "Integration")]
[Trait("Database", "PostgreSQL")]
public sealed class MartenTemporalKeyProviderContractTests(MartenFixture fixture) : TemporalKeyProviderContractTests, IAsyncLifetime
{
    private DocumentStore? _store;

    public async ValueTask InitializeAsync()
    {
        Assert.SkipUnless(fixture.IsAvailable, "Marten PostgreSQL container not available");

        // A store of its own: DestroyKeysBeforeAsync destroys every older period of the schema it runs against.
        _store = DocumentStore.For(options =>
        {
            options.Connection(fixture.ConnectionString);
            options.DatabaseSchemaName = $"audit_keys_contract_{Guid.NewGuid():N}"[..40];
        });
        await _store.Storage.ApplyAllConfiguredChangesToDatabaseAsync();
    }

    public ValueTask DisposeAsync()
    {
        _store?.Dispose();
        return ValueTask.CompletedTask;
    }

    protected override ITemporalKeyProvider CreateProvider() =>
        new MartenTemporalKeyProvider(
            _store!.LightweightSession(),
            new FakeTimeProvider(KeyCreationTime),
            NullLogger<MartenTemporalKeyProvider>.Instance);
}
