using BenchmarkDotNet.Attributes;
using Encina.Compliance.ProcessorAgreements.Abstractions;
using Encina.Compliance.ProcessorAgreements.Model;
using Encina.Compliance.ProcessorAgreements.ReadModels;
using LanguageExt;
using NSubstitute;
using static LanguageExt.Prelude;

namespace Encina.Benchmarks.Compliance.ProcessorAgreements;

/// <summary>
/// Shared setup and mocked services for the Processor Agreements service benchmarks.
/// </summary>
/// <remarks>
/// The service benchmarks are split into several classes so that each one stays well under the
/// 60-minute job limit of the weekly Benchmarks matrix (#2510); each concrete class becomes its
/// own matrix job. This base carries no benchmark attributes of its own, so the class
/// discovery and BenchmarkDotNet do not treat it as a benchmark class.
/// </remarks>
public abstract class ProcessorAgreementBenchmarkBase
{
    protected IDPAService DpaService { get; private set; } = null!;
    protected IProcessorService ProcessorService { get; private set; } = null!;
    protected Guid ExistingProcessorId { get; private set; }
    protected Guid ExistingDPAId { get; private set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        ExistingProcessorId = Guid.NewGuid();
        ExistingDPAId = Guid.NewGuid();
        DpaService = BuildMockedDPAService();
        ProcessorService = BuildMockedProcessorService();
    }

    protected static DPAMandatoryTerms FullyCompliantTerms() => new()
    {
        ProcessOnDocumentedInstructions = true,
        ConfidentialityObligations = true,
        SecurityMeasures = true,
        SubProcessorRequirements = true,
        DataSubjectRightsAssistance = true,
        ComplianceAssistance = true,
        DataDeletionOrReturn = true,
        AuditRights = true
    };

    private IDPAService BuildMockedDPAService()
    {
        var service = Substitute.For<IDPAService>();

        var readModel = new DPAReadModel
        {
            Id = ExistingDPAId,
            ProcessorId = ExistingProcessorId,
            Status = DPAStatus.Active,
            MandatoryTerms = FullyCompliantTerms(),
            HasSCCs = true,
            ProcessingPurposes = ["data-analytics", "payment-processing"],
            SignedAtUtc = DateTimeOffset.UtcNow.AddDays(-60),
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(300),
            CreatedAtUtc = DateTimeOffset.UtcNow.AddDays(-60),
            LastModifiedAtUtc = DateTimeOffset.UtcNow,
            Version = 3
        };

        var validationResult = new DPAValidationResult
        {
            ProcessorId = ExistingProcessorId.ToString(),
            DPAId = ExistingDPAId.ToString(),
            IsValid = true,
            MissingTerms = [],
            Warnings = [],
            DaysUntilExpiration = 300,
            ValidatedAtUtc = DateTimeOffset.UtcNow
        };

#pragma warning disable CA2012
        service.HasValidDPAAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, bool>(true)));

        service.ValidateDPAAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, DPAValidationResult>(validationResult)));

        service.ExecuteDPAAsync(
            Arg.Any<Guid>(), Arg.Any<DPAMandatoryTerms>(), Arg.Any<bool>(),
            Arg.Any<IReadOnlyList<string>>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset?>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Guid>(Guid.NewGuid())));

        service.AmendDPAAsync(
            Arg.Any<Guid>(), Arg.Any<DPAMandatoryTerms>(), Arg.Any<bool>(),
            Arg.Any<IReadOnlyList<string>>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Unit>(Unit.Default)));

        service.AuditDPAAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Unit>(Unit.Default)));

        service.RenewDPAAsync(
            Arg.Any<Guid>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Unit>(Unit.Default)));

        service.TerminateDPAAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Unit>(Unit.Default)));

        service.GetDPAAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, DPAReadModel>(readModel)));

        service.GetActiveDPAByProcessorIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, DPAReadModel>(readModel)));

        service.GetExpiringDPAsAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, IReadOnlyList<DPAReadModel>>(
                new List<DPAReadModel> { readModel }.AsReadOnly())));
#pragma warning restore CA2012

        return service;
    }

    private IProcessorService BuildMockedProcessorService()
    {
        var service = Substitute.For<IProcessorService>();

        var processorReadModel = new ProcessorReadModel
        {
            Id = ExistingProcessorId,
            Name = "Stripe",
            Country = "US",
            ContactEmail = "dpo@stripe.com",
            Depth = 0,
            AuthorizationType = SubProcessorAuthorizationType.Specific,
            SubProcessorCount = 2,
            CreatedAtUtc = DateTimeOffset.UtcNow.AddDays(-90),
            LastModifiedAtUtc = DateTimeOffset.UtcNow,
            Version = 3
        };

        var subProcessors = new List<ProcessorReadModel>
        {
            new()
            {
                Id = Guid.NewGuid(), Name = "Stripe Treasury", Country = "US",
                ParentProcessorId = ExistingProcessorId, Depth = 1,
                AuthorizationType = SubProcessorAuthorizationType.Specific,
                CreatedAtUtc = DateTimeOffset.UtcNow, LastModifiedAtUtc = DateTimeOffset.UtcNow, Version = 1
            },
            new()
            {
                Id = Guid.NewGuid(), Name = "Stripe Atlas", Country = "IE",
                ParentProcessorId = ExistingProcessorId, Depth = 1,
                AuthorizationType = SubProcessorAuthorizationType.Specific,
                CreatedAtUtc = DateTimeOffset.UtcNow, LastModifiedAtUtc = DateTimeOffset.UtcNow, Version = 1
            }
        }.AsReadOnly();

#pragma warning disable CA2012
        service.RegisterProcessorAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<Guid?>(),
            Arg.Any<int>(), Arg.Any<SubProcessorAuthorizationType>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Guid>(Guid.NewGuid())));

        service.GetProcessorAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, ProcessorReadModel>(processorReadModel)));

        service.GetFullSubProcessorChainAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, IReadOnlyList<ProcessorReadModel>>(subProcessors)));
#pragma warning restore CA2012

        return service;
    }
}
