#pragma warning disable CA2012

using Encina;
using Encina.Compliance.ProcessorAgreements;
using Encina.Compliance.ProcessorAgreements.Model;
using Encina.Compliance.ProcessorAgreements.Notifications;
using Encina.Compliance.ProcessorAgreements.Scheduling;

using FluentAssertions;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.ProcessorAgreements;

/// <summary>
/// Unit tests for <see cref="CheckDPAExpirationHandler"/>.
/// </summary>
public class CheckDPAExpirationHandlerTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly FakeTimeProvider _timeProvider = new(FixedNow);
    private readonly IDPAStore _dpaStore = Substitute.For<IDPAStore>();
    private readonly IProcessorRegistry _registry = Substitute.For<IProcessorRegistry>();
    private readonly IProcessorAuditStore _auditStore = Substitute.For<IProcessorAuditStore>();
    private readonly IEncina _encina = Substitute.For<IEncina>();

    #region Handle - No Expiring

    [Fact]
    public async Task Handle_NoExpiringAgreements_ReturnsSuccess()
    {
        // Arrange
        var handler = CreateHandler();

        _dpaStore.GetExpiringAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Right<EncinaError, IReadOnlyList<DataProcessingAgreement>>(
                    System.Array.Empty<DataProcessingAgreement>().AsReadOnly())));

        // Act
        var result = await handler.Handle(new CheckDPAExpirationCommand(), CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();
        await _encina.DidNotReceive().Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Handle - Expired Agreements

    [Fact]
    public async Task Handle_ExpiredAgreement_UpdatesStatusAndPublishesNotification()
    {
        // Arrange
        var handler = CreateHandler();
        var expiredDpa = CreateDPA("dpa-1", "proc-1", DPAStatus.Active,
            expiresAtUtc: FixedNow.AddDays(-5));

        SetupExpiringAgreements([expiredDpa]);
        SetupProcessorLookup("proc-1", "Stripe Inc.");
        SetupUpdateSuccess();
        SetupPublishSuccess();

        // Act
        var result = await handler.Handle(new CheckDPAExpirationCommand(), CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();

        await _dpaStore.Received(1).UpdateAsync(
            Arg.Is<DataProcessingAgreement>(d => d.Status == DPAStatus.Expired && d.Id == "dpa-1"),
            Arg.Any<CancellationToken>());

        await _encina.Received(1).Publish(
            Arg.Is<DPAExpiredNotification>(n =>
                n.ProcessorId == "proc-1" &&
                n.DPAId == "dpa-1" &&
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
        var approachingDpa = CreateDPA("dpa-2", "proc-2", DPAStatus.Active,
            expiresAtUtc: FixedNow.AddDays(15));

        SetupExpiringAgreements([approachingDpa]);
        SetupProcessorLookup("proc-2", "AWS");
        SetupPublishSuccess();

        // Act
        var result = await handler.Handle(new CheckDPAExpirationCommand(), CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();

        await _dpaStore.DidNotReceive().UpdateAsync(
            Arg.Any<DataProcessingAgreement>(),
            Arg.Any<CancellationToken>());

        await _encina.Received(1).Publish(
            Arg.Is<DPAExpiringNotification>(n =>
                n.ProcessorId == "proc-2" &&
                n.DPAId == "dpa-2" &&
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

        var expiredDpa = CreateDPA("dpa-1", "proc-1", DPAStatus.Active,
            expiresAtUtc: FixedNow.AddDays(-2));
        var approachingDpa = CreateDPA("dpa-2", "proc-2", DPAStatus.Active,
            expiresAtUtc: FixedNow.AddDays(20));

        SetupExpiringAgreements([expiredDpa, approachingDpa]);
        SetupProcessorLookup("proc-1", "Stripe Inc.");
        SetupProcessorLookup("proc-2", "AWS");
        SetupUpdateSuccess();
        SetupPublishSuccess();

        // Act
        var result = await handler.Handle(new CheckDPAExpirationCommand(), CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();

        await _dpaStore.Received(1).UpdateAsync(
            Arg.Is<DataProcessingAgreement>(d => d.Id == "dpa-1" && d.Status == DPAStatus.Expired),
            Arg.Any<CancellationToken>());

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

        _dpaStore.GetExpiringAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Left<EncinaError, IReadOnlyList<DataProcessingAgreement>>(error)));

        // Act
        var result = await handler.Handle(new CheckDPAExpirationCommand(), CancellationToken.None);

        // Assert
        result.IsLeft.Should().BeTrue();
        var returnedError = result.Match(Right: _ => default!, Left: e => e);
        returnedError.GetCode().IfNone("").Should().Be("store.failure");
    }

    [Fact]
    public async Task Handle_UpdateExpiredFails_ContinuesProcessing()
    {
        // Arrange
        var handler = CreateHandler();

        var expiredDpa1 = CreateDPA("dpa-1", "proc-1", DPAStatus.Active,
            expiresAtUtc: FixedNow.AddDays(-5));
        var expiredDpa2 = CreateDPA("dpa-2", "proc-2", DPAStatus.Active,
            expiresAtUtc: FixedNow.AddDays(-3));

        SetupExpiringAgreements([expiredDpa1, expiredDpa2]);
        SetupProcessorLookup("proc-1", "Stripe");
        SetupProcessorLookup("proc-2", "AWS");
        SetupPublishSuccess();

        // First update fails, second succeeds
        _dpaStore.UpdateAsync(
            Arg.Is<DataProcessingAgreement>(d => d.Id == "dpa-1"),
            Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Left<EncinaError, Unit>(EncinaErrors.Create("update.failed", "Update failed"))));

        _dpaStore.UpdateAsync(
            Arg.Is<DataProcessingAgreement>(d => d.Id == "dpa-2"),
            Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Unit>(unit)));

        // Act
        var result = await handler.Handle(new CheckDPAExpirationCommand(), CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();

        // Second DPA should still be processed and notified
        await _encina.Received(1).Publish(
            Arg.Is<DPAExpiredNotification>(n => n.DPAId == "dpa-2"),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Handle - Audit Trail

    [Fact]
    public async Task Handle_AuditTrailEnabled_RecordsAuditEntry()
    {
        // Arrange
        var options = new ProcessorAgreementOptions
        {
            ExpirationWarningDays = 30,
            TrackAuditTrail = true
        };
        var handler = CreateHandler(options);

        var expiredDpa = CreateDPA("dpa-1", "proc-1", DPAStatus.Active,
            expiresAtUtc: FixedNow.AddDays(-1));

        SetupExpiringAgreements([expiredDpa]);
        SetupProcessorLookup("proc-1", "Stripe Inc.");
        SetupUpdateSuccess();
        SetupPublishSuccess();

        // Act
        await handler.Handle(new CheckDPAExpirationCommand(), CancellationToken.None);

        // Assert
        await _auditStore.Received().RecordAsync(
            Arg.Is<ProcessorAgreementAuditEntry>(e =>
                e.ProcessorId == "proc-1" &&
                e.DPAId == "dpa-1" &&
                e.Action == "DPAExpired"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AuditTrailDisabled_SkipsAudit()
    {
        // Arrange
        var options = new ProcessorAgreementOptions
        {
            ExpirationWarningDays = 30,
            TrackAuditTrail = false
        };
        var handler = CreateHandler(options);

        var expiredDpa = CreateDPA("dpa-1", "proc-1", DPAStatus.Active,
            expiresAtUtc: FixedNow.AddDays(-1));

        SetupExpiringAgreements([expiredDpa]);
        SetupProcessorLookup("proc-1", "Stripe Inc.");
        SetupUpdateSuccess();
        SetupPublishSuccess();

        // Act
        await handler.Handle(new CheckDPAExpirationCommand(), CancellationToken.None);

        // Assert
        await _auditStore.DidNotReceive().RecordAsync(
            Arg.Any<ProcessorAgreementAuditEntry>(),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Helpers

    private CheckDPAExpirationHandler CreateHandler(ProcessorAgreementOptions? options = null)
    {
        var opts = options ?? new ProcessorAgreementOptions
        {
            ExpirationWarningDays = 30,
            TrackAuditTrail = true
        };

        return new CheckDPAExpirationHandler(
            _dpaStore,
            _registry,
            _auditStore,
            _encina,
            Options.Create(opts),
            _timeProvider,
            NullLogger<CheckDPAExpirationHandler>.Instance);
    }

    private static DataProcessingAgreement CreateDPA(
        string id,
        string processorId,
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
        LastUpdatedAtUtc = FixedNow.AddMonths(-1)
    };

    private void SetupExpiringAgreements(DataProcessingAgreement[] agreements)
    {
        _dpaStore.GetExpiringAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Right<EncinaError, IReadOnlyList<DataProcessingAgreement>>(
                    agreements.ToList().AsReadOnly())));
    }

    private void SetupProcessorLookup(string processorId, string name)
    {
        var processor = new Processor
        {
            Id = processorId,
            Name = name,
            Country = "US",
            Depth = 0,
            SubProcessorAuthorizationType = SubProcessorAuthorizationType.General,
            CreatedAtUtc = FixedNow.AddYears(-1),
            LastUpdatedAtUtc = FixedNow.AddYears(-1)
        };

        _registry.GetProcessorAsync(processorId, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Right<EncinaError, Option<Processor>>(Some(processor))));
    }

    private void SetupUpdateSuccess()
    {
        _dpaStore.UpdateAsync(Arg.Any<DataProcessingAgreement>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Unit>(unit)));
    }

    private void SetupPublishSuccess()
    {
        _encina.Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Unit>>(unit));
    }

    #endregion
}
