using Encina.Compliance.Retention;
using Encina.Compliance.Retention.Abstractions;
using Encina.Compliance.Retention.Model;
using Encina.Compliance.Retention.ReadModels;

using Shouldly;

using LanguageExt;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.Retention;

/// <summary>
/// Unit tests for <see cref="RetentionEnforcementService"/>.
/// </summary>
public sealed class RetentionEnforcementServiceTests
{
    private readonly IRetentionRecordService _recordService = Substitute.For<IRetentionRecordService>();
    private readonly ILegalHoldService _legalHoldService = Substitute.For<ILegalHoldService>();

    private IServiceScopeFactory CreateScopeFactory()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_recordService);
        services.AddSingleton(_legalHoldService);
        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IServiceScopeFactory>();
    }

    [Fact]
    public async Task ExecuteAsync_DisabledEnforcement_DoesNotExecuteCycle()
    {
        var options = new RetentionOptions { EnableAutomaticEnforcement = false };
        var sut = new RetentionEnforcementService(
            CreateScopeFactory(),
            Options.Create(options),
            NullLogger<RetentionEnforcementService>.Instance);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

        await sut.StartAsync(cts.Token);
        await Task.Delay(100, CancellationToken.None);
        await sut.StopAsync(CancellationToken.None);

        await _recordService.DidNotReceive().GetExpiredRecordsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_EnabledEnforcement_ExecutesCycleImmediately()
    {
        var options = new RetentionOptions
        {
            EnableAutomaticEnforcement = true,
            EnforcementInterval = TimeSpan.FromHours(1)
        };

        _recordService.GetExpiredRecordsAsync(Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<RetentionRecordReadModel>>(
                (IReadOnlyList<RetentionRecordReadModel>)[]));

        var sut = new RetentionEnforcementService(
            CreateScopeFactory(),
            Options.Create(options),
            NullLogger<RetentionEnforcementService>.Instance);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        await sut.StartAsync(cts.Token);
        await Task.Delay(500, CancellationToken.None);
        await sut.StopAsync(CancellationToken.None);

        await _recordService.Received(1).GetExpiredRecordsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ExpiredRecordsWithNoHold_DeletesRecords()
    {
        var options = new RetentionOptions
        {
            EnableAutomaticEnforcement = true,
            EnforcementInterval = TimeSpan.FromHours(1),
            PublishNotifications = false
        };

        var expiredRecord = new RetentionRecordReadModel
        {
            Id = Guid.NewGuid(),
            EntityId = "entity-1",
            DataCategory = "test-data",
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(-1),
            Status = RetentionStatus.Active
        };

        _recordService.GetExpiredRecordsAsync(Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<RetentionRecordReadModel>>(
                new List<RetentionRecordReadModel> { expiredRecord }));

        _legalHoldService.HasActiveHoldsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, bool>(false));

        _recordService.MarkDeletedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(unit));

        var sut = new RetentionEnforcementService(
            CreateScopeFactory(),
            Options.Create(options),
            NullLogger<RetentionEnforcementService>.Instance);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        await sut.StartAsync(cts.Token);
        await Task.Delay(500, CancellationToken.None);
        await sut.StopAsync(CancellationToken.None);

        await _recordService.Received(1).MarkDeletedAsync(expiredRecord.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ExpiredRecordsUnderHold_HoldsRecords()
    {
        var options = new RetentionOptions
        {
            EnableAutomaticEnforcement = true,
            EnforcementInterval = TimeSpan.FromHours(1),
            PublishNotifications = false
        };

        var expiredRecord = new RetentionRecordReadModel
        {
            Id = Guid.NewGuid(),
            EntityId = "entity-held",
            DataCategory = "held-data",
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(-1),
            Status = RetentionStatus.Active
        };

        _recordService.GetExpiredRecordsAsync(Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<RetentionRecordReadModel>>(
                new List<RetentionRecordReadModel> { expiredRecord }));

        _legalHoldService.HasActiveHoldsAsync("entity-held", Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, bool>(true));

        _recordService.HoldRecordAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(unit));

        var sut = new RetentionEnforcementService(
            CreateScopeFactory(),
            Options.Create(options),
            NullLogger<RetentionEnforcementService>.Instance);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        await sut.StartAsync(cts.Token);
        await Task.Delay(500, CancellationToken.None);
        await sut.StopAsync(CancellationToken.None);

        await _recordService.Received(1).HoldRecordAsync(expiredRecord.Id, Guid.Empty, Arg.Any<CancellationToken>());
        await _recordService.DidNotReceive().MarkDeletedAsync(expiredRecord.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_GetExpiredReturnsError_DoesNotDelete()
    {
        var options = new RetentionOptions
        {
            EnableAutomaticEnforcement = true,
            EnforcementInterval = TimeSpan.FromHours(1)
        };

        _recordService.GetExpiredRecordsAsync(Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, IReadOnlyList<RetentionRecordReadModel>>(
                RetentionErrors.StoreError("GetExpired", "Connection failed")));

        var sut = new RetentionEnforcementService(
            CreateScopeFactory(),
            Options.Create(options),
            NullLogger<RetentionEnforcementService>.Instance);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        await sut.StartAsync(cts.Token);
        await Task.Delay(500, CancellationToken.None);
        await sut.StopAsync(CancellationToken.None);

        await _recordService.DidNotReceive().MarkDeletedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_RecordDeletionThrows_ContinuesWithNext()
    {
        var options = new RetentionOptions
        {
            EnableAutomaticEnforcement = true,
            EnforcementInterval = TimeSpan.FromHours(1),
            PublishNotifications = false
        };

        var record1 = new RetentionRecordReadModel
        {
            Id = Guid.NewGuid(),
            EntityId = "e1",
            DataCategory = "cat",
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(-1),
            Status = RetentionStatus.Active
        };
        var record2 = new RetentionRecordReadModel
        {
            Id = Guid.NewGuid(),
            EntityId = "e2",
            DataCategory = "cat",
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(-1),
            Status = RetentionStatus.Active
        };

        _recordService.GetExpiredRecordsAsync(Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<RetentionRecordReadModel>>(
                new List<RetentionRecordReadModel> { record1, record2 }));

        _legalHoldService.HasActiveHoldsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, bool>(false));

        // First deletion throws, second succeeds
#pragma warning disable CA2012 // NSubstitute mock setup for ValueTask-returning method
        _recordService.MarkDeletedAsync(record1.Id, Arg.Any<CancellationToken>())
            .Returns<ValueTask<Either<EncinaError, Unit>>>(_ =>
                throw new InvalidOperationException("DB error"));
#pragma warning restore CA2012
        _recordService.MarkDeletedAsync(record2.Id, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(unit));

        var sut = new RetentionEnforcementService(
            CreateScopeFactory(),
            Options.Create(options),
            NullLogger<RetentionEnforcementService>.Instance);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        await sut.StartAsync(cts.Token);
        await Task.Delay(500, CancellationToken.None);
        await sut.StopAsync(CancellationToken.None);

        // Both should have been attempted
        await _recordService.Received(1).MarkDeletedAsync(record1.Id, Arg.Any<CancellationToken>());
        await _recordService.Received(1).MarkDeletedAsync(record2.Id, Arg.Any<CancellationToken>());
    }
}
