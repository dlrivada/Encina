using Encina.OpenTelemetry.Audit;
using Encina.Security.Audit;
using Encina.Testing;
using LanguageExt;
using NSubstitute;
using Shouldly;

namespace Encina.UnitTests.OpenTelemetry.Audit;

/// <summary>
/// Unit tests for <see cref="InstrumentedAuditStore"/>.
/// </summary>
public sealed class InstrumentedAuditStoreTests
{
    private readonly IAuditStore _inner;
    private readonly InstrumentedAuditStore _sut;

    public InstrumentedAuditStoreTests()
    {
        _inner = Substitute.For<IAuditStore>();
        _sut = new InstrumentedAuditStore(_inner);
    }

    [Fact]
    public async Task RecordAsync_DelegatesToInner()
    {
        // Arrange
        var entry = CreateAuditEntry();
        _inner.RecordAsync(entry, Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, Unit>.Right(Unit.Default));

        // Act
        var result = await _sut.RecordAsync(entry);

        // Assert
        result.ShouldBeSuccess();
        await _inner.Received(1).RecordAsync(entry, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RecordAsync_WhenInnerFails_ReturnsError()
    {
        // Arrange
        var entry = CreateAuditEntry();
        var error = EncinaError.New("audit failed");
        _inner.RecordAsync(entry, Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, Unit>.Left(error));

        // Act
        var result = await _sut.RecordAsync(entry);

        // Assert
        result.ShouldBeError();
    }

    [Fact]
    public async Task GetByEntityAsync_DelegatesToInner()
    {
        // Arrange
        var entries = new List<AuditEntry> { CreateAuditEntry() };
        _inner.GetByEntityAsync("Order", "123", Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, IReadOnlyList<AuditEntry>>.Right(entries));

        // Act
        var result = await _sut.GetByEntityAsync("Order", "123");

        // Assert
        result.ShouldBeSuccess().Count.ShouldBe(1);
    }

    [Fact]
    public async Task GetByUserAsync_DelegatesToInner()
    {
        // Arrange
        var entries = new List<AuditEntry> { CreateAuditEntry() };
        _inner.GetByUserAsync("user-1", null, null, Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, IReadOnlyList<AuditEntry>>.Right(entries));

        // Act
        var result = await _sut.GetByUserAsync("user-1", null, null);

        // Assert
        result.ShouldBeSuccess().Count.ShouldBe(1);
    }

    [Fact]
    public async Task GetByCorrelationIdAsync_DelegatesToInner()
    {
        // Arrange
        var entries = new List<AuditEntry> { CreateAuditEntry() };
        _inner.GetByCorrelationIdAsync("corr-1", Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, IReadOnlyList<AuditEntry>>.Right(entries));

        // Act
        var result = await _sut.GetByCorrelationIdAsync("corr-1");

        // Assert
        result.ShouldBeSuccess().Count.ShouldBe(1);
    }

    [Fact]
    public async Task QueryAsync_DelegatesToInner()
    {
        // Arrange
        var query = new AuditQuery();
        var entries = new List<AuditEntry> { CreateAuditEntry() };
        var paged = PagedResult<AuditEntry>.Create(entries, 1, 1, 10);
        _inner.QueryAsync(query, Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, PagedResult<AuditEntry>>.Right(paged));

        // Act
        var result = await _sut.QueryAsync(query);

        // Assert
        result.ShouldBeSuccess().Items.Count.ShouldBe(1);
    }

    [Fact]
    public async Task PurgeEntriesAsync_DelegatesToInner()
    {
        // Arrange
        var olderThan = DateTime.UtcNow.AddDays(-30);
        _inner.PurgeEntriesAsync(olderThan, Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, int>.Right(5));

        // Act
        var result = await _sut.PurgeEntriesAsync(olderThan);

        // Assert
        result.ShouldBeSuccess().ShouldBe(5);
    }

    [Fact]
    public async Task GetByEntityAsync_WhenInnerFails_ReturnsError()
    {
        // Arrange
        var error = EncinaError.New("query failed");
        _inner.GetByEntityAsync("Order", null, Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, IReadOnlyList<AuditEntry>>.Left(error));

        // Act
        var result = await _sut.GetByEntityAsync("Order", null);

        // Assert
        result.ShouldBeError();
    }

    private static AuditEntry CreateAuditEntry()
    {
        var now = DateTimeOffset.UtcNow;
        return new AuditEntry
        {
            Id = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid().ToString(),
            EntityType = "Order",
            EntityId = Guid.NewGuid().ToString(),
            Action = "Created",
            UserId = "user-1",
            Outcome = AuditOutcome.Success,
            TimestampUtc = now.UtcDateTime,
            StartedAtUtc = now,
            CompletedAtUtc = now
        };
    }
}
