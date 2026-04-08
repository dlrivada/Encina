using Encina.Caching;
using Encina.Compliance.ProcessorAgreements;
using Encina.Compliance.ProcessorAgreements.Aggregates;
using Encina.Compliance.ProcessorAgreements.Model;
using Encina.Compliance.ProcessorAgreements.ReadModels;
using Encina.Compliance.ProcessorAgreements.Services;
using Encina.Marten;
using Encina.Marten.Projections;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.ProcessorAgreements;

/// <summary>
/// Extended unit tests for <see cref="DefaultDPAService"/> covering AmendDPAAsync,
/// AuditDPAAsync, RenewDPAAsync, and TerminateDPAAsync methods.
/// </summary>
public sealed class DefaultDPAServiceExtendedTests
{
    private readonly IAggregateRepository<DPAAggregate> _repository;
    private readonly IReadModelRepository<DPAReadModel> _readModelRepository;
    private readonly ICacheProvider _cache;
    private readonly FakeTimeProvider _timeProvider;
    private readonly IOptions<ProcessorAgreementOptions> _options;
    private readonly ILogger<DefaultDPAService> _logger;
    private readonly DefaultDPAService _sut;

    public DefaultDPAServiceExtendedTests()
    {
        _repository = Substitute.For<IAggregateRepository<DPAAggregate>>();
        _readModelRepository = Substitute.For<IReadModelRepository<DPAReadModel>>();
        _cache = Substitute.For<ICacheProvider>();
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 3, 25, 12, 0, 0, TimeSpan.Zero));
        _options = Options.Create(new ProcessorAgreementOptions { ExpirationWarningDays = 30 });
        _logger = NullLogger<DefaultDPAService>.Instance;

        _sut = new DefaultDPAService(
            _repository,
            _readModelRepository,
            _cache,
            _timeProvider,
            _options,
            _logger);
    }

    // ========================================================================
    // AmendDPAAsync tests
    // ========================================================================

#pragma warning disable CA2012
    [Fact]
    public async Task AmendDPAAsync_WhenAggregateFoundAndSaveSucceeds_ShouldReturnRight()
    {
        // Arrange
        var dpaId = Guid.NewGuid();
        var aggregate = CreateActiveDPAAggregate(dpaId);
        var updatedTerms = CreateFullyCompliantTerms();

        _repository.LoadAsync(dpaId, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, DPAAggregate>(aggregate));
        _repository.SaveAsync(Arg.Any<DPAAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));

        // Act
        var result = await _sut.AmendDPAAsync(
            dpaId, updatedTerms, true, ["analytics", "reporting"], "Updated purposes");

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task AmendDPAAsync_WhenNotFound_ShouldReturnDPANotFoundError()
    {
        // Arrange
        var dpaId = Guid.NewGuid();
        var error = EncinaErrors.Create("not_found", "Not found");
        _repository.LoadAsync(dpaId, Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, DPAAggregate>(error));

        // Act
        var result = await _sut.AmendDPAAsync(
            dpaId, CreateFullyCompliantTerms(), true, ["analytics"], "Reason");

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.Match(Right: _ => "", Left: e => e.GetCode().IfNone("")).ShouldBe("processor.dpa_not_found");
    }

    [Fact]
    public async Task AmendDPAAsync_WhenSaveFails_ShouldReturnError()
    {
        // Arrange
        var dpaId = Guid.NewGuid();
        var aggregate = CreateActiveDPAAggregate(dpaId);
        var saveError = EncinaErrors.Create("save.error", "Save failed");

        _repository.LoadAsync(dpaId, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, DPAAggregate>(aggregate));
        _repository.SaveAsync(Arg.Any<DPAAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, Unit>(saveError));

        // Act
        var result = await _sut.AmendDPAAsync(
            dpaId, CreateFullyCompliantTerms(), true, ["analytics"], "Reason");

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task AmendDPAAsync_WhenInvalidOperationThrown_ShouldReturnValidationError()
    {
        // Arrange
        var dpaId = Guid.NewGuid();
        _repository.LoadAsync(dpaId, Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Cannot amend terminated DPA"));

        // Act
        var result = await _sut.AmendDPAAsync(
            dpaId, CreateFullyCompliantTerms(), true, ["analytics"], "Reason");

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.Match(Right: _ => "", Left: e => e.GetCode().IfNone("")).ShouldBe("processor.validation_failed");
    }

    // ========================================================================
    // AuditDPAAsync tests
    // ========================================================================

    [Fact]
    public async Task AuditDPAAsync_WhenAggregateFoundAndSaveSucceeds_ShouldReturnRight()
    {
        // Arrange
        var dpaId = Guid.NewGuid();
        var aggregate = CreateActiveDPAAggregate(dpaId);

        _repository.LoadAsync(dpaId, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, DPAAggregate>(aggregate));
        _repository.SaveAsync(Arg.Any<DPAAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));

        // Act
        var result = await _sut.AuditDPAAsync(dpaId, "auditor-001", "All terms compliant");

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task AuditDPAAsync_WhenNotFound_ShouldReturnDPANotFoundError()
    {
        // Arrange
        var dpaId = Guid.NewGuid();
        var error = EncinaErrors.Create("not_found", "Not found");
        _repository.LoadAsync(dpaId, Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, DPAAggregate>(error));

        // Act
        var result = await _sut.AuditDPAAsync(dpaId, "auditor-001", "Findings");

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.Match(Right: _ => "", Left: e => e.GetCode().IfNone("")).ShouldBe("processor.dpa_not_found");
    }

    [Fact]
    public async Task AuditDPAAsync_WhenSaveFails_ShouldReturnError()
    {
        // Arrange
        var dpaId = Guid.NewGuid();
        var aggregate = CreateActiveDPAAggregate(dpaId);
        var saveError = EncinaErrors.Create("save.error", "Save failed");

        _repository.LoadAsync(dpaId, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, DPAAggregate>(aggregate));
        _repository.SaveAsync(Arg.Any<DPAAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, Unit>(saveError));

        // Act
        var result = await _sut.AuditDPAAsync(dpaId, "auditor-001", "Findings");

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task AuditDPAAsync_WhenInvalidOperationThrown_ShouldReturnValidationError()
    {
        // Arrange
        var dpaId = Guid.NewGuid();
        _repository.LoadAsync(dpaId, Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Cannot audit terminated DPA"));

        // Act
        var result = await _sut.AuditDPAAsync(dpaId, "auditor-001", "Findings");

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.Match(Right: _ => "", Left: e => e.GetCode().IfNone("")).ShouldBe("processor.validation_failed");
    }

    // ========================================================================
    // RenewDPAAsync tests
    // ========================================================================

    [Fact]
    public async Task RenewDPAAsync_WhenAggregateFoundAndSaveSucceeds_ShouldReturnRight()
    {
        // Arrange
        var dpaId = Guid.NewGuid();
        var aggregate = CreateActiveDPAAggregate(dpaId);
        var newExpiration = _timeProvider.GetUtcNow().AddYears(1);

        _repository.LoadAsync(dpaId, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, DPAAggregate>(aggregate));
        _repository.SaveAsync(Arg.Any<DPAAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));

        // Act
        var result = await _sut.RenewDPAAsync(dpaId, newExpiration);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task RenewDPAAsync_WhenNotFound_ShouldReturnDPANotFoundError()
    {
        // Arrange
        var dpaId = Guid.NewGuid();
        var error = EncinaErrors.Create("not_found", "Not found");
        _repository.LoadAsync(dpaId, Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, DPAAggregate>(error));

        // Act
        var result = await _sut.RenewDPAAsync(dpaId, _timeProvider.GetUtcNow().AddYears(1));

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.Match(Right: _ => "", Left: e => e.GetCode().IfNone("")).ShouldBe("processor.dpa_not_found");
    }

    [Fact]
    public async Task RenewDPAAsync_WhenSaveFails_ShouldReturnError()
    {
        // Arrange
        var dpaId = Guid.NewGuid();
        var aggregate = CreateActiveDPAAggregate(dpaId);
        var saveError = EncinaErrors.Create("save.error", "Save failed");

        _repository.LoadAsync(dpaId, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, DPAAggregate>(aggregate));
        _repository.SaveAsync(Arg.Any<DPAAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, Unit>(saveError));

        // Act
        var result = await _sut.RenewDPAAsync(dpaId, _timeProvider.GetUtcNow().AddYears(1));

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task RenewDPAAsync_WhenInvalidOperationThrown_ShouldReturnValidationError()
    {
        // Arrange
        var dpaId = Guid.NewGuid();
        _repository.LoadAsync(dpaId, Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Cannot renew terminated DPA"));

        // Act
        var result = await _sut.RenewDPAAsync(dpaId, _timeProvider.GetUtcNow().AddYears(1));

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.Match(Right: _ => "", Left: e => e.GetCode().IfNone("")).ShouldBe("processor.validation_failed");
    }

    // ========================================================================
    // TerminateDPAAsync tests
    // ========================================================================

    [Fact]
    public async Task TerminateDPAAsync_WhenAggregateFoundAndSaveSucceeds_ShouldReturnRight()
    {
        // Arrange
        var dpaId = Guid.NewGuid();
        var aggregate = CreateActiveDPAAggregate(dpaId);

        _repository.LoadAsync(dpaId, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, DPAAggregate>(aggregate));
        _repository.SaveAsync(Arg.Any<DPAAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));

        // Act
        var result = await _sut.TerminateDPAAsync(dpaId, "Contract breach");

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task TerminateDPAAsync_WhenNotFound_ShouldReturnDPANotFoundError()
    {
        // Arrange
        var dpaId = Guid.NewGuid();
        var error = EncinaErrors.Create("not_found", "Not found");
        _repository.LoadAsync(dpaId, Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, DPAAggregate>(error));

        // Act
        var result = await _sut.TerminateDPAAsync(dpaId, "No longer needed");

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.Match(Right: _ => "", Left: e => e.GetCode().IfNone("")).ShouldBe("processor.dpa_not_found");
    }

    [Fact]
    public async Task TerminateDPAAsync_WhenSaveFails_ShouldReturnError()
    {
        // Arrange
        var dpaId = Guid.NewGuid();
        var aggregate = CreateActiveDPAAggregate(dpaId);
        var saveError = EncinaErrors.Create("save.error", "Save failed");

        _repository.LoadAsync(dpaId, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, DPAAggregate>(aggregate));
        _repository.SaveAsync(Arg.Any<DPAAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, Unit>(saveError));

        // Act
        var result = await _sut.TerminateDPAAsync(dpaId, "Contract breach");

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task TerminateDPAAsync_WhenInvalidOperationThrown_ShouldReturnValidationError()
    {
        // Arrange
        var dpaId = Guid.NewGuid();
        _repository.LoadAsync(dpaId, Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Already terminated"));

        // Act
        var result = await _sut.TerminateDPAAsync(dpaId, "Reason");

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.Match(Right: _ => "", Left: e => e.GetCode().IfNone("")).ShouldBe("processor.validation_failed");
    }
#pragma warning restore CA2012

    // ========================================================================
    // Helpers
    // ========================================================================

    private static DPAMandatoryTerms CreateFullyCompliantTerms() => new()
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

    private DPAAggregate CreateActiveDPAAggregate(Guid dpaId)
    {
        var processorId = Guid.NewGuid();
        var terms = CreateFullyCompliantTerms();
        var occurredAtUtc = _timeProvider.GetUtcNow();

        return DPAAggregate.Execute(
            dpaId, processorId, terms, true, ["analytics"],
            occurredAtUtc, occurredAtUtc.AddYears(1), occurredAtUtc);
    }
}
