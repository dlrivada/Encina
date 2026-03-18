using BenchmarkDotNet.Attributes;
using Encina.Compliance.Retention.Abstractions;
using Encina.Compliance.Retention.Model;
using Encina.Compliance.Retention.ReadModels;
using LanguageExt;
using NSubstitute;
using static LanguageExt.Prelude;

namespace Encina.Benchmarks.Compliance.Retention;

/// <summary>
/// Benchmarks for the retention compliance service operations.
/// Measures throughput and allocations for each service method across the retention lifecycle:
/// - Policy creation (fast path — single aggregate write)
/// - Entity tracking (record creation + expiration calculation)
/// - Record expiration marking (single state transition)
/// - Record deletion marking (terminal state transition)
/// - Legal hold placement (cross-aggregate coordination)
/// - Legal hold check (read-only query)
/// - Retention period lookup (cached query with fallback)
/// </summary>
/// <remarks>
/// <para>
/// Retention operations execute on every data lifecycle event where retention tracking is enabled.
/// Policy lookups happen on every tracked entity creation, making them hot-path operations
/// requiring performance characterization.
/// </para>
/// <para>
/// Run via:
/// <code>
/// cd tests/Encina.BenchmarkTests/Encina.Benchmarks
/// dotnet run -c Release -- --filter "*RetentionServiceBenchmarks*"
///
/// # Quick validation:
/// dotnet run -c Release -- --filter "*RetentionServiceBenchmarks*" --job short
///
/// # List available benchmarks:
/// dotnet run -c Release -- --list flat --filter "*RetentionService*"
/// </code>
/// </para>
/// </remarks>
[MemoryDiagnoser]
[RankColumn]
public class RetentionServiceBenchmarks
{
    private IRetentionPolicyService _policyService = null!;
    private IRetentionRecordService _recordService = null!;
    private ILegalHoldService _holdService = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _policyService = BuildMockedPolicyService();
        _recordService = BuildMockedRecordService();
        _holdService = BuildMockedLegalHoldService();
    }

    // ────────────────────────────────────────────────────────────
    //  Policy Operations
    // ────────────────────────────────────────────────────────────

    [Benchmark(Baseline = true, Description = "Policy: create (fast path)")]
    public async Task<Guid> Policy_Create()
    {
        var result = await _policyService.CreatePolicyAsync(
            "customer-data", TimeSpan.FromDays(365), true,
            RetentionPolicyType.TimeBased, "GDPR compliance", "Art. 5(1)(e)");
        return result.Match(Right: id => id, Left: _ => Guid.Empty);
    }

    [Benchmark(Description = "Policy: get retention period (cached lookup)")]
    public async Task<TimeSpan> Policy_GetRetentionPeriod()
    {
        var result = await _policyService.GetRetentionPeriodAsync("customer-data");
        return result.Match(Right: period => period, Left: _ => TimeSpan.Zero);
    }

    [Benchmark(Description = "Policy: get by ID")]
    public async Task<RetentionPolicyReadModel?> Policy_GetById()
    {
        var result = await _policyService.GetPolicyAsync(Guid.NewGuid());
        return result.Match(Right: p => p, Left: _ => null);
    }

    [Benchmark(Description = "Policy: deactivate")]
    public async Task<Unit> Policy_Deactivate()
    {
        var result = await _policyService.DeactivatePolicyAsync(
            Guid.NewGuid(), "No longer needed");
        return result.Match(Right: u => u, Left: _ => Unit.Default);
    }

    // ────────────────────────────────────────────────────────────
    //  Record Operations
    // ────────────────────────────────────────────────────────────

    [Benchmark(Description = "Record: track entity")]
    public async Task<Guid> Record_TrackEntity()
    {
        var result = await _recordService.TrackEntityAsync(
            "customer-12345", "customer-data",
            Guid.NewGuid(), TimeSpan.FromDays(365));
        return result.Match(Right: id => id, Left: _ => Guid.Empty);
    }

    [Benchmark(Description = "Record: mark expired")]
    public async Task<Unit> Record_MarkExpired()
    {
        var result = await _recordService.MarkExpiredAsync(Guid.NewGuid());
        return result.Match(Right: u => u, Left: _ => Unit.Default);
    }

    [Benchmark(Description = "Record: mark deleted (terminal)")]
    public async Task<Unit> Record_MarkDeleted()
    {
        var result = await _recordService.MarkDeletedAsync(Guid.NewGuid());
        return result.Match(Right: u => u, Left: _ => Unit.Default);
    }

    [Benchmark(Description = "Record: mark anonymized (terminal)")]
    public async Task<Unit> Record_MarkAnonymized()
    {
        var result = await _recordService.MarkAnonymizedAsync(Guid.NewGuid());
        return result.Match(Right: u => u, Left: _ => Unit.Default);
    }

    // ────────────────────────────────────────────────────────────
    //  Legal Hold Operations
    // ────────────────────────────────────────────────────────────

    [Benchmark(Description = "Legal hold: place (cross-aggregate)")]
    public async Task<Guid> LegalHold_Place()
    {
        var result = await _holdService.PlaceHoldAsync(
            "customer-12345", "Ongoing litigation - Case #54321",
            "legal-counsel-1");
        return result.Match(Right: id => id, Left: _ => Guid.Empty);
    }

    [Benchmark(Description = "Legal hold: lift")]
    public async Task<Unit> LegalHold_Lift()
    {
        var result = await _holdService.LiftHoldAsync(
            Guid.NewGuid(), "legal-counsel-2");
        return result.Match(Right: u => u, Left: _ => Unit.Default);
    }

    [Benchmark(Description = "Legal hold: has active holds (read-only)")]
    public async Task<bool> LegalHold_HasActiveHolds()
    {
        var result = await _holdService.HasActiveHoldsAsync("customer-12345");
        return result.Match(Right: has => has, Left: _ => false);
    }

    // ────────────────────────────────────────────────────────────
    //  Infrastructure — Mocked Services
    // ────────────────────────────────────────────────────────────

    private static IRetentionPolicyService BuildMockedPolicyService()
    {
        var service = Substitute.For<IRetentionPolicyService>();

#pragma warning disable CA2012
        service.CreatePolicyAsync(
                Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<bool>(),
                Arg.Any<RetentionPolicyType>(), Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, Guid>>(Right<EncinaError, Guid>(Guid.NewGuid())));

        service.GetRetentionPeriodAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, TimeSpan>>(
                Right<EncinaError, TimeSpan>(TimeSpan.FromDays(365))));

        service.GetPolicyAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var id = callInfo.ArgAt<Guid>(0);
                return new ValueTask<Either<EncinaError, RetentionPolicyReadModel>>(
                    Right<EncinaError, RetentionPolicyReadModel>(new RetentionPolicyReadModel
                    {
                        Id = id,
                        DataCategory = "customer-data",
                        RetentionPeriod = TimeSpan.FromDays(365),
                        AutoDelete = true,
                        PolicyType = RetentionPolicyType.TimeBased,
                        IsActive = true,
                        CreatedAtUtc = DateTimeOffset.UtcNow,
                        LastModifiedAtUtc = DateTimeOffset.UtcNow
                    }));
            });

        service.UpdatePolicyAsync(
                Arg.Any<Guid>(), Arg.Any<TimeSpan>(), Arg.Any<bool>(),
                Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, Unit>>(Right<EncinaError, Unit>(Unit.Default)));

        service.DeactivatePolicyAsync(
                Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, Unit>>(Right<EncinaError, Unit>(Unit.Default)));
#pragma warning restore CA2012

        return service;
    }

    private static IRetentionRecordService BuildMockedRecordService()
    {
        var service = Substitute.For<IRetentionRecordService>();

#pragma warning disable CA2012
        service.TrackEntityAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Guid>(),
                Arg.Any<TimeSpan>(), Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, Guid>>(Right<EncinaError, Guid>(Guid.NewGuid())));

        service.MarkExpiredAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, Unit>>(Right<EncinaError, Unit>(Unit.Default)));

        service.MarkDeletedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, Unit>>(Right<EncinaError, Unit>(Unit.Default)));

        service.MarkAnonymizedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, Unit>>(Right<EncinaError, Unit>(Unit.Default)));

        service.HoldRecordAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, Unit>>(Right<EncinaError, Unit>(Unit.Default)));

        service.ReleaseRecordAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, Unit>>(Right<EncinaError, Unit>(Unit.Default)));
#pragma warning restore CA2012

        return service;
    }

    private static ILegalHoldService BuildMockedLegalHoldService()
    {
        var service = Substitute.For<ILegalHoldService>();

#pragma warning disable CA2012
        service.PlaceHoldAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, Guid>>(Right<EncinaError, Guid>(Guid.NewGuid())));

        service.LiftHoldAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, Unit>>(Right<EncinaError, Unit>(Unit.Default)));

        service.HasActiveHoldsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, bool>>(Right<EncinaError, bool>(false)));

        service.GetAllActiveHoldsAsync(Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, IReadOnlyList<LegalHoldReadModel>>>(
                Right<EncinaError, IReadOnlyList<LegalHoldReadModel>>(
                    System.Array.Empty<LegalHoldReadModel>())));
#pragma warning restore CA2012

        return service;
    }
}
