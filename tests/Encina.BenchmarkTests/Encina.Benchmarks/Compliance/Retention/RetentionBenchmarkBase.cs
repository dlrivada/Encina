using BenchmarkDotNet.Attributes;
using Encina.Compliance.Retention.Abstractions;
using Encina.Compliance.Retention.Model;
using Encina.Compliance.Retention.ReadModels;
using LanguageExt;
using NSubstitute;
using static LanguageExt.Prelude;

namespace Encina.Benchmarks.Compliance.Retention;

/// <summary>
/// Shared setup and mocked services for the retention compliance service benchmarks.
/// </summary>
/// <remarks>
/// The retention benchmarks are split into several classes so that each one stays well under the
/// 60-minute job limit of the scheduled Benchmarks matrix (#2554); each concrete class becomes its
/// own matrix job. This base carries no benchmark attributes of its own, so the class discovery
/// and BenchmarkDotNet do not treat it as a benchmark class.
/// </remarks>
public abstract class RetentionBenchmarkBase
{
    protected IRetentionPolicyService PolicyService { get; private set; } = null!;
    protected IRetentionRecordService RecordService { get; private set; } = null!;
    protected ILegalHoldService HoldService { get; private set; } = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        PolicyService = BuildMockedPolicyService();
        RecordService = BuildMockedRecordService();
        HoldService = BuildMockedLegalHoldService();
    }

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
