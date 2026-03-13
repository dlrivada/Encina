#pragma warning disable CA2012

using Encina;
using Encina.Compliance.ProcessorAgreements;
using Encina.Compliance.ProcessorAgreements.Model;

using FluentAssertions;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.ProcessorAgreements;

/// <summary>
/// Unit tests for <see cref="DefaultDPAValidator"/>.
/// </summary>
public class DefaultDPAValidatorTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly FakeTimeProvider _timeProvider = new(FixedNow);
    private readonly IProcessorRegistry _registry = Substitute.For<IProcessorRegistry>();
    private readonly IDPAStore _dpaStore = Substitute.For<IDPAStore>();
    private readonly DefaultDPAValidator _sut;

    public DefaultDPAValidatorTests()
    {
        _sut = new DefaultDPAValidator(
            _registry,
            _dpaStore,
            _timeProvider,
            NullLogger<DefaultDPAValidator>.Instance);
    }

    #region ValidateAsync

    [Fact]
    public async Task ValidateAsync_ProcessorNotFound_ReturnsNotValid()
    {
        // Arrange
        _registry.GetProcessorAsync("proc-1", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Right<EncinaError, Option<Processor>>(Option<Processor>.None)));

        // Act
        var result = await _sut.ValidateAsync("proc-1");

        // Assert
        result.IsRight.Should().BeTrue();
        var value = result.Match(Right: x => x, Left: _ => default!);
        value.IsValid.Should().BeFalse();
        value.ProcessorId.Should().Be("proc-1");
        value.Warnings.Should().Contain(w => w.Contains("Processor not found"));
    }

    [Fact]
    public async Task ValidateAsync_NoDPA_ReturnsNotValid()
    {
        // Arrange
        var processor = CreateProcessor("proc-1");
        _registry.GetProcessorAsync("proc-1", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Right<EncinaError, Option<Processor>>(Some(processor))));

        _dpaStore.GetActiveByProcessorIdAsync("proc-1", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Right<EncinaError, Option<DataProcessingAgreement>>(Option<DataProcessingAgreement>.None)));

        // Act
        var result = await _sut.ValidateAsync("proc-1");

        // Assert
        result.IsRight.Should().BeTrue();
        var value = result.Match(Right: x => x, Left: _ => default!);
        value.IsValid.Should().BeFalse();
        value.Warnings.Should().Contain(w => w.Contains("No active Data Processing Agreement"));
    }

    [Fact]
    public async Task ValidateAsync_ActiveCompliantDPA_ReturnsValid()
    {
        // Arrange
        var processor = CreateProcessor("proc-1");
        var dpa = CreateDPA("dpa-1", "proc-1", DPAStatus.Active,
            expiresAtUtc: FixedNow.AddYears(1),
            fullyCompliant: true,
            hasSCCs: true);

        SetupRegistryAndStore("proc-1", processor, dpa);

        // Act
        var result = await _sut.ValidateAsync("proc-1");

        // Assert
        result.IsRight.Should().BeTrue();
        var value = result.Match(Right: x => x, Left: _ => default!);
        value.IsValid.Should().BeTrue();
        value.DPAId.Should().Be("dpa-1");
        value.Status.Should().Be(DPAStatus.Active);
    }

    [Fact]
    public async Task ValidateAsync_ExpiredDPA_ReturnsNotValid()
    {
        // Arrange
        var processor = CreateProcessor("proc-1");
        var dpa = CreateDPA("dpa-1", "proc-1", DPAStatus.Expired,
            expiresAtUtc: FixedNow.AddDays(-10),
            fullyCompliant: true,
            hasSCCs: true);

        SetupRegistryAndStore("proc-1", processor, dpa);

        // Act
        var result = await _sut.ValidateAsync("proc-1");

        // Assert
        result.IsRight.Should().BeTrue();
        var value = result.Match(Right: x => x, Left: _ => default!);
        value.IsValid.Should().BeFalse();
        value.Warnings.Should().Contain(w => w.Contains("not active"));
    }

    [Fact]
    public async Task ValidateAsync_IncompleteMandatoryTerms_ReturnsNotValid()
    {
        // Arrange
        var processor = CreateProcessor("proc-1");
        var dpa = CreateDPA("dpa-1", "proc-1", DPAStatus.Active,
            expiresAtUtc: FixedNow.AddYears(1),
            fullyCompliant: false,
            hasSCCs: true);

        SetupRegistryAndStore("proc-1", processor, dpa);

        // Act
        var result = await _sut.ValidateAsync("proc-1");

        // Assert
        result.IsRight.Should().BeTrue();
        var value = result.Match(Right: x => x, Left: _ => default!);
        value.IsValid.Should().BeFalse();
        value.Warnings.Should().Contain(w => w.Contains("mandatory term"));
    }

    [Fact]
    public async Task ValidateAsync_NearingExpiration_ReturnsValidWithWarning()
    {
        // Arrange
        var processor = CreateProcessor("proc-1");
        var dpa = CreateDPA("dpa-1", "proc-1", DPAStatus.Active,
            expiresAtUtc: FixedNow.AddDays(15),
            fullyCompliant: true,
            hasSCCs: true);

        SetupRegistryAndStore("proc-1", processor, dpa);

        // Act
        var result = await _sut.ValidateAsync("proc-1");

        // Assert
        result.IsRight.Should().BeTrue();
        var value = result.Match(Right: x => x, Left: _ => default!);
        value.IsValid.Should().BeTrue();
        value.DaysUntilExpiration.Should().Be(15);
        value.Warnings.Should().Contain(w => w.Contains("expires in"));
    }

    [Fact]
    public async Task ValidateAsync_NoSCCs_ReturnsValidWithWarning()
    {
        // Arrange
        var processor = CreateProcessor("proc-1");
        var dpa = CreateDPA("dpa-1", "proc-1", DPAStatus.Active,
            expiresAtUtc: FixedNow.AddYears(1),
            fullyCompliant: true,
            hasSCCs: false);

        SetupRegistryAndStore("proc-1", processor, dpa);

        // Act
        var result = await _sut.ValidateAsync("proc-1");

        // Assert
        result.IsRight.Should().BeTrue();
        var value = result.Match(Right: x => x, Left: _ => default!);
        value.IsValid.Should().BeTrue();
        value.Warnings.Should().Contain(w => w.Contains("Standard Contractual Clauses"));
    }

    [Fact]
    public async Task ValidateAsync_NullProcessorId_ThrowsArgumentNullException()
    {
        // Act
        var act = async () => await _sut.ValidateAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("processorId");
    }

    [Fact]
    public async Task ValidateAsync_RegistryError_ReturnsError()
    {
        // Arrange
        var error = EncinaErrors.Create("registry.failure", "Registry unavailable");
        _registry.GetProcessorAsync("proc-1", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Left<EncinaError, Option<Processor>>(error)));

        // Act
        var result = await _sut.ValidateAsync("proc-1");

        // Assert
        result.IsLeft.Should().BeTrue();
        var returnedError = result.Match(Right: _ => default!, Left: e => e);
        returnedError.GetCode().IfNone("").Should().Be("registry.failure");
    }

    #endregion

    #region HasValidDPAAsync

    [Fact]
    public async Task HasValidDPAAsync_ValidDPA_ReturnsTrue()
    {
        // Arrange
        var processor = CreateProcessor("proc-1");
        var dpa = CreateDPA("dpa-1", "proc-1", DPAStatus.Active,
            expiresAtUtc: FixedNow.AddYears(1),
            fullyCompliant: true,
            hasSCCs: true);

        SetupRegistryAndStore("proc-1", processor, dpa);

        // Act
        var result = await _sut.HasValidDPAAsync("proc-1");

        // Assert
        result.IsRight.Should().BeTrue();
        var value = result.Match(Right: x => x, Left: _ => false);
        value.Should().BeTrue();
    }

    [Fact]
    public async Task HasValidDPAAsync_NoDPA_ReturnsFalse()
    {
        // Arrange
        var processor = CreateProcessor("proc-1");
        _registry.GetProcessorAsync("proc-1", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Right<EncinaError, Option<Processor>>(Some(processor))));

        _dpaStore.GetActiveByProcessorIdAsync("proc-1", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Right<EncinaError, Option<DataProcessingAgreement>>(Option<DataProcessingAgreement>.None)));

        // Act
        var result = await _sut.HasValidDPAAsync("proc-1");

        // Assert
        result.IsRight.Should().BeTrue();
        var value = result.Match(Right: x => x, Left: _ => true);
        value.Should().BeFalse();
    }

    [Fact]
    public async Task HasValidDPAAsync_ProcessorNotFound_ReturnsFalse()
    {
        // Arrange
        _registry.GetProcessorAsync("proc-1", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Right<EncinaError, Option<Processor>>(Option<Processor>.None)));

        // Act
        var result = await _sut.HasValidDPAAsync("proc-1");

        // Assert
        result.IsRight.Should().BeTrue();
        var value = result.Match(Right: x => x, Left: _ => true);
        value.Should().BeFalse();
    }

    [Fact]
    public async Task HasValidDPAAsync_IncompleteDPA_ReturnsFalse()
    {
        // Arrange
        var processor = CreateProcessor("proc-1");
        var dpa = CreateDPA("dpa-1", "proc-1", DPAStatus.Active,
            expiresAtUtc: FixedNow.AddYears(1),
            fullyCompliant: false,
            hasSCCs: true);

        SetupRegistryAndStore("proc-1", processor, dpa);

        // Act
        var result = await _sut.HasValidDPAAsync("proc-1");

        // Assert
        result.IsRight.Should().BeTrue();
        var value = result.Match(Right: x => x, Left: _ => true);
        value.Should().BeFalse();
    }

    #endregion

    #region ValidateAllAsync

    [Fact]
    public async Task ValidateAllAsync_MultipleProcessors_ReturnsAllResults()
    {
        // Arrange
        var proc1 = CreateProcessor("proc-1");
        var proc2 = CreateProcessor("proc-2");

        _registry.GetAllProcessorsAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Right<EncinaError, IReadOnlyList<Processor>>(
                    new List<Processor> { proc1, proc2 }.AsReadOnly())));

        var dpa1 = CreateDPA("dpa-1", "proc-1", DPAStatus.Active,
            expiresAtUtc: FixedNow.AddYears(1), fullyCompliant: true, hasSCCs: true);
        SetupRegistryAndStore("proc-1", proc1, dpa1);

        _registry.GetProcessorAsync("proc-2", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Right<EncinaError, Option<Processor>>(Some(proc2))));
        _dpaStore.GetActiveByProcessorIdAsync("proc-2", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Right<EncinaError, Option<DataProcessingAgreement>>(Option<DataProcessingAgreement>.None)));

        // Act
        var result = await _sut.ValidateAllAsync();

        // Assert
        result.IsRight.Should().BeTrue();
        var results = result.Match(Right: x => x, Left: _ => default!);
        results.Should().HaveCount(2);
        results[0].IsValid.Should().BeTrue();
        results[1].IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAllAsync_RegistryError_ReturnsError()
    {
        // Arrange
        var error = EncinaErrors.Create("registry.failure", "Registry unavailable");
        _registry.GetAllProcessorsAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Left<EncinaError, IReadOnlyList<Processor>>(error)));

        // Act
        var result = await _sut.ValidateAllAsync();

        // Assert
        result.IsLeft.Should().BeTrue();
        var returnedError = result.Match(Right: _ => default!, Left: e => e);
        returnedError.GetCode().IfNone("").Should().Be("registry.failure");
    }

    #endregion

    #region Helpers

    private static Processor CreateProcessor(string id) => new()
    {
        Id = id,
        Name = $"Processor {id}",
        Country = "DE",
        Depth = 0,
        SubProcessorAuthorizationType = SubProcessorAuthorizationType.General,
        CreatedAtUtc = FixedNow.AddMonths(-6),
        LastUpdatedAtUtc = FixedNow.AddMonths(-6)
    };

    private static DataProcessingAgreement CreateDPA(
        string id,
        string processorId,
        DPAStatus status,
        DateTimeOffset? expiresAtUtc,
        bool fullyCompliant,
        bool hasSCCs) => new()
        {
            Id = id,
            ProcessorId = processorId,
            Status = status,
            SignedAtUtc = FixedNow.AddYears(-1),
            ExpiresAtUtc = expiresAtUtc,
            MandatoryTerms = CreateMandatoryTerms(fullyCompliant),
            HasSCCs = hasSCCs,
            ProcessingPurposes = ["Data processing"],
            CreatedAtUtc = FixedNow.AddYears(-1),
            LastUpdatedAtUtc = FixedNow.AddMonths(-1)
        };

    private static DPAMandatoryTerms CreateMandatoryTerms(bool allCompliant) => new()
    {
        ProcessOnDocumentedInstructions = allCompliant,
        ConfidentialityObligations = allCompliant,
        SecurityMeasures = allCompliant,
        SubProcessorRequirements = allCompliant,
        DataSubjectRightsAssistance = allCompliant,
        ComplianceAssistance = allCompliant,
        DataDeletionOrReturn = allCompliant,
        AuditRights = allCompliant
    };

    private void SetupRegistryAndStore(
        string processorId,
        Processor processor,
        DataProcessingAgreement dpa)
    {
        _registry.GetProcessorAsync(processorId, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Right<EncinaError, Option<Processor>>(Some(processor))));

        _dpaStore.GetActiveByProcessorIdAsync(processorId, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Right<EncinaError, Option<DataProcessingAgreement>>(Some(dpa))));
    }

    #endregion
}
