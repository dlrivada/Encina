#pragma warning disable CA2012

using Encina;
using Encina.Compliance.ProcessorAgreements;
using Encina.Compliance.ProcessorAgreements.Abstractions;
using Encina.Compliance.ProcessorAgreements.Aggregates;
using Encina.Compliance.ProcessorAgreements.Model;
using Encina.Compliance.ProcessorAgreements.Notifications;
using Encina.Compliance.ProcessorAgreements.ReadModels;
using Encina.Compliance.ProcessorAgreements.Scheduling;
using Encina.Marten;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.ProcessorAgreements;

/// <summary>
/// Unit tests for <see cref="CheckDPAExpirationHandler"/>.
/// </summary>
public class CheckDPAExpirationHandlerTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly FakeTimeProvider _timeProvider = new(FixedNow);
    private readonly IDPAService _dpaService = Substitute.For<IDPAService>();
    private readonly IProcessorService _processorService = Substitute.For<IProcessorService>();
    private readonly IAggregateRepository<DPAAggregate> _dpaRepository = Substitute.For<IAggregateRepository<DPAAggregate>>();
    private readonly IEncina _encina = Substitute.For<IEncina>();

    #region Handle - No Expiring

    [Fact]
    public async Task Handle_NoExpiringAgreements_ReturnsSuccess()
    {
        // Arrange
        var handler = CreateHandler();

        _dpaService.GetExpiringDPAsAsync(Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, IReadOnlyList<DPAReadModel>>>(
                Right<EncinaError, IReadOnlyList<DPAReadModel>>(
                    System.Array.Empty<DPAReadModel>().AsReadOnly())));

        // Act
        var result = await handler.Handle(new CheckDPAExpirationCommand(), CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        await _encina.DidNotReceive().Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Handle - Expired Agreements

    [Fact]
    public async Task Handle_ExpiredAgreement_UpdatesStatusAndPublishesNotification()
    {
        // Arrange
        var handler = CreateHandler();
        var dpaId = Guid.NewGuid();
        var processorId = Guid.NewGuid();
        var expiredDpa = CreateDPAReadModel(dpaId, processorId, DPAStatus.Active,
            expiresAtUtc: FixedNow.AddDays(-5));

        SetupExpiringAgreements([expiredDpa]);
        SetupProcessorLookup(processorId, "Stripe Inc.");
        SetupLoadAndSaveSuccess(dpaId);
        SetupPublishSuccess();

        // Act
        var result = await handler.Handle(new CheckDPAExpirationCommand(), CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();

        await _dpaRepository.Received(1).LoadAsync(dpaId, Arg.Any<CancellationToken>());
        await _dpaRepository.Received(1).SaveAsync(Arg.Any<DPAAggregate>(), Arg.Any<CancellationToken>());

        await _encina.Received(1).Publish(
            Arg.Is<DPAExpiredNotification>(n =>
                n.ProcessorId == processorId.ToString() &&
                n.DPAId == dpaId.ToString() &&
                n.ProcessorName == "Stripe Inc."),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Handle - Approaching Agreements

    [Fact]
    public async Task Handle_ApproachingAgreement_PublishesExpiringNotification()
    {
        // Arrange
        var handler = CreateHandler();
        var dpaId = Guid.NewGuid();
        var processorId = Guid.NewGuid();
        var approachingDpa = CreateDPAReadModel(dpaId, processorId, DPAStatus.Active,
            expiresAtUtc: FixedNow.AddDays(15));

        SetupExpiringAgreements([approachingDpa]);
        SetupProcessorLookup(processorId, "AWS");
        SetupPublishSuccess();

        // Act
        var result = await handler.Handle(new CheckDPAExpirationCommand(), CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();

        await _dpaRepository.DidNotReceive().LoadAsync(
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());

        await _encina.Received(1).Publish(
            Arg.Is<DPAExpiringNotification>(n =>
                n.ProcessorId == processorId.ToString() &&
                n.DPAId == dpaId.ToString() &&
                n.ProcessorName == "AWS" &&
                n.DaysUntilExpiration == 15),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Handle - Mixed Agreements

    [Fact]
    public async Task Handle_MixedAgreements_ProcessesBothCorrectly()
    {
        // Arrange
        var handler = CreateHandler();

        var dpaId1 = Guid.NewGuid();
        var processorId1 = Guid.NewGuid();
        var dpaId2 = Guid.NewGuid();
        var processorId2 = Guid.NewGuid();

        var expiredDpa = CreateDPAReadModel(dpaId1, processorId1, DPAStatus.Active,
            expiresAtUtc: FixedNow.AddDays(-2));
        var approachingDpa = CreateDPAReadModel(dpaId2, processorId2, DPAStatus.Active,
            expiresAtUtc: FixedNow.AddDays(20));

        SetupExpiringAgreements([expiredDpa, approachingDpa]);
        SetupProcessorLookup(processorId1, "Stripe Inc.");
        SetupProcessorLookup(processorId2, "AWS");
        SetupLoadAndSaveSuccess(dpaId1);
        SetupPublishSuccess();

        // Act
        var result = await handler.Handle(new CheckDPAExpirationCommand(), CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();

        await _dpaRepository.Received(1).LoadAsync(dpaId1, Arg.Any<CancellationToken>());
        await _dpaRepository.Received(1).SaveAsync(Arg.Any<DPAAggregate>(), Arg.Any<CancellationToken>());

        await _encina.Received(1).Publish(
            Arg.Any<DPAExpiredNotification>(), Arg.Any<CancellationToken>());

        await _encina.Received(1).Publish(
            Arg.Any<DPAExpiringNotification>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Handle - Error Scenarios

    [Fact]
    public async Task Handle_GetExpiringFails_ReturnsError()
    {
        // Arrange
        var handler = CreateHandler();
        var error = EncinaErrors.Create("store.failure", "Store unavailable");

        _dpaService.GetExpiringDPAsAsync(Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, IReadOnlyList<DPAReadModel>>>(
                Left<EncinaError, IReadOnlyList<DPAReadModel>>(error)));

        // Act
        var result = await handler.Handle(new CheckDPAExpirationCommand(), CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
        var returnedError = result.Match(Right: _ => default!, Left: e => e);
        returnedError.GetCode().IfNone("").ShouldBe("store.failure");
    }

    [Fact]
    public async Task Handle_UpdateExpiredFails_ContinuesProcessing()
    {
        // Arrange
        var handler = CreateHandler();

        var dpaId1 = Guid.NewGuid();
        var processorId1 = Guid.NewGuid();
        var dpaId2 = Guid.NewGuid();
        var processorId2 = Guid.NewGuid();

        var expiredDpa1 = CreateDPAReadModel(dpaId1, processorId1, DPAStatus.Active,
            expiresAtUtc: FixedNow.AddDays(-5));
        var expiredDpa2 = CreateDPAReadModel(dpaId2, processorId2, DPAStatus.Active,
            expiresAtUtc: FixedNow.AddDays(-3));

        SetupExpiringAgreements([expiredDpa1, expiredDpa2]);
        SetupProcessorLookup(processorId1, "Stripe");
        SetupProcessorLookup(processorId2, "AWS");
        SetupPublishSuccess();

        // First LoadAsync fails, second succeeds
        _dpaRepository.LoadAsync(dpaId1, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(
                Left<EncinaError, DPAAggregate>(EncinaErrors.Create("load.failed", "Load failed"))));

        var realAggregate2 = CreateRealDPAAggregate(dpaId2);
        _dpaRepository.LoadAsync(dpaId2, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, DPAAggregate>(realAggregate2)));

        _dpaRepository.SaveAsync(Arg.Any<DPAAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, Unit>(unit)));

        // Act
        var result = await handler.Handle(new CheckDPAExpirationCommand(), CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();

        // Second DPA should still be processed and notified
        await _encina.Received(1).Publish(
            Arg.Is<DPAExpiredNotification>(n => n.DPAId == dpaId2.ToString()),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Helpers

    private CheckDPAExpirationHandler CreateHandler(ProcessorAgreementOptions? options = null)
    {
        var opts = options ?? new ProcessorAgreementOptions
        {
            ExpirationWarningDays = 30
        };

        return new CheckDPAExpirationHandler(
            _dpaService,
            _processorService,
            _dpaRepository,
            _encina,
            Options.Create(opts),
            _timeProvider,
            NullLogger<CheckDPAExpirationHandler>.Instance);
    }

    private static DPAReadModel CreateDPAReadModel(
        Guid id,
        Guid processorId,
        DPAStatus status,
        DateTimeOffset? expiresAtUtc) => new()
        {
            Id = id,
            ProcessorId = processorId,
            Status = status,
            SignedAtUtc = FixedNow.AddYears(-1),
            ExpiresAtUtc = expiresAtUtc,
            MandatoryTerms = new DPAMandatoryTerms
            {
                ProcessOnDocumentedInstructions = true,
                ConfidentialityObligations = true,
                SecurityMeasures = true,
                SubProcessorRequirements = true,
                DataSubjectRightsAssistance = true,
                ComplianceAssistance = true,
                DataDeletionOrReturn = true,
                AuditRights = true
            },
            HasSCCs = true,
            ProcessingPurposes = ["Data processing"],
            CreatedAtUtc = FixedNow.AddYears(-1),
            LastModifiedAtUtc = FixedNow.AddMonths(-1)
        };

    private void SetupExpiringAgreements(DPAReadModel[] agreements)
    {
        _dpaService.GetExpiringDPAsAsync(Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, IReadOnlyList<DPAReadModel>>>(
                Right<EncinaError, IReadOnlyList<DPAReadModel>>(
                    agreements.ToList().AsReadOnly())));
    }

    private void SetupProcessorLookup(Guid processorId, string name)
    {
        var processor = new ProcessorReadModel
        {
            Id = processorId,
            Name = name,
            Country = "US",
            Depth = 0,
            AuthorizationType = SubProcessorAuthorizationType.General,
            CreatedAtUtc = FixedNow.AddYears(-1),
            LastModifiedAtUtc = FixedNow.AddYears(-1)
        };

        _processorService.GetProcessorAsync(processorId, Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, ProcessorReadModel>>(
                Right<EncinaError, ProcessorReadModel>(processor)));
    }

    private void SetupLoadAndSaveSuccess(Guid dpaId)
    {
        var realAggregate = CreateRealDPAAggregate(dpaId);
        _dpaRepository.LoadAsync(dpaId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, DPAAggregate>(realAggregate)));

        _dpaRepository.SaveAsync(Arg.Any<DPAAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, Unit>(unit)));
    }

    private static DPAAggregate CreateRealDPAAggregate(Guid dpaId) =>
        DPAAggregate.Execute(
            dpaId,
            Guid.NewGuid(),
            new DPAMandatoryTerms
            {
                ProcessOnDocumentedInstructions = true,
                ConfidentialityObligations = true,
                SecurityMeasures = true,
                SubProcessorRequirements = true,
                DataSubjectRightsAssistance = true,
                ComplianceAssistance = true,
                DataDeletionOrReturn = true,
                AuditRights = true
            },
            hasSCCs: true,
            processingPurposes: ["Data processing"],
            signedAtUtc: FixedNow.AddYears(-1),
            expiresAtUtc: FixedNow.AddDays(-1),
            occurredAtUtc: FixedNow.AddYears(-1));

    private void SetupPublishSuccess()
    {
        _encina.Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Unit>>(unit));
    }

    #endregion
}
