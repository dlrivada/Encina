#pragma warning disable CA2012 // Use ValueTasks correctly

using Encina.Compliance.PrivacyByDesign;
using Encina.Compliance.PrivacyByDesign.Model;

using FluentAssertions;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.PrivacyByDesign;

/// <summary>
/// Unit tests for <see cref="DefaultPrivacyByDesignValidator"/>.
/// </summary>
public class DefaultPrivacyByDesignValidatorTests
{
    private readonly IDataMinimizationAnalyzer _analyzer = Substitute.For<IDataMinimizationAnalyzer>();
    private readonly IPurposeRegistry _purposeRegistry = Substitute.For<IPurposeRegistry>();
    private readonly FakeTimeProvider _timeProvider = new(new DateTimeOffset(2026, 3, 14, 12, 0, 0, TimeSpan.Zero));
    private readonly DefaultPrivacyByDesignValidator _sut;

    public DefaultPrivacyByDesignValidatorTests()
    {
        _sut = new DefaultPrivacyByDesignValidator(
            _analyzer,
            _purposeRegistry,
            _timeProvider,
            NullLogger<DefaultPrivacyByDesignValidator>.Instance);

        // Clear static caches to avoid cross-test contamination.
        DefaultDataMinimizationAnalyzer.MetadataCache.Clear();
    }

    #region Test Types

    [EnforceDataMinimization(Purpose = "Order Processing")]
    public class OrderCommand
    {
        public string ProductId { get; set; } = "";
        public int Quantity { get; set; }

        [NotStrictlyNecessary(Reason = "Analytics only")]
        public string? ReferralSource { get; set; }

        [PurposeLimitation("Marketing")]
        public string? CampaignCode { get; set; }

        [PrivacyDefault(false)]
        public bool ShareData { get; set; }
    }

    public class SimpleCommand
    {
        public string Name { get; set; } = "";
    }

    #endregion

    #region ValidateAsync — Compliant Request

    [Fact]
    public async Task ValidateAsync_NoViolations_ShouldReturnRightWithCompliantResult()
    {
        // Arrange
        var request = new OrderCommand { ProductId = "P1", Quantity = 2 };

        _analyzer.AnalyzeAsync(Arg.Any<OrderCommand>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, MinimizationReport>(new MinimizationReport
            {
                RequestTypeName = "OrderCommand",
                NecessaryFields = [new PrivacyFieldInfo("ProductId", null, true)],
                UnnecessaryFields = [],
                MinimizationScore = 1.0,
                Recommendations = [],
                AnalyzedAtUtc = _timeProvider.GetUtcNow(),
            })));

        _analyzer.InspectDefaultsAsync(Arg.Any<OrderCommand>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, IReadOnlyList<DefaultPrivacyFieldInfo>>(
                System.Array.Empty<DefaultPrivacyFieldInfo>())));

        _purposeRegistry.GetPurposeAsync("Order Processing", Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Option<PurposeDefinition>>(
                Some(new PurposeDefinition
                {
                    PurposeId = "test-id",
                    Name = "Order Processing",
                    Description = "Test",
                    LegalBasis = "Contract",
                    AllowedFields = ["ProductId", "Quantity", "ReferralSource", "CampaignCode", "ShareData"],
                    CreatedAtUtc = _timeProvider.GetUtcNow(),
                }))));

        // Act
        var result = await _sut.ValidateAsync(request);

        // Assert
        result.IsRight.Should().BeTrue();
        var validation = (PrivacyValidationResult)result;
        validation.IsCompliant.Should().BeTrue();
        validation.Violations.Should().BeEmpty();
    }

    #endregion

    #region ValidateAsync — Data Minimization Violations

    [Fact]
    public async Task ValidateAsync_UnnecessaryFieldsWithValues_ShouldReturnNonCompliantResult()
    {
        // Arrange
        var request = new OrderCommand
        {
            ProductId = "P1",
            Quantity = 2,
            ReferralSource = "google",
        };

        _analyzer.AnalyzeAsync(Arg.Any<OrderCommand>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, MinimizationReport>(new MinimizationReport
            {
                RequestTypeName = "OrderCommand",
                NecessaryFields = [new PrivacyFieldInfo("ProductId", null, true)],
                UnnecessaryFields =
                [
                    new UnnecessaryFieldInfo("ReferralSource", "Analytics only", true, MinimizationSeverity.Warning),
                ],
                MinimizationScore = 0.5,
                Recommendations = ["Consider removing 'ReferralSource'"],
                AnalyzedAtUtc = _timeProvider.GetUtcNow(),
            })));

        _analyzer.InspectDefaultsAsync(Arg.Any<OrderCommand>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, IReadOnlyList<DefaultPrivacyFieldInfo>>(
                System.Array.Empty<DefaultPrivacyFieldInfo>())));

        _purposeRegistry.GetPurposeAsync("Order Processing", Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Option<PurposeDefinition>>(
                Some(new PurposeDefinition
                {
                    PurposeId = "test-id",
                    Name = "Order Processing",
                    Description = "Test",
                    LegalBasis = "Contract",
                    AllowedFields = ["ProductId", "Quantity", "ReferralSource", "CampaignCode", "ShareData"],
                    CreatedAtUtc = _timeProvider.GetUtcNow(),
                }))));

        // Act
        var result = await _sut.ValidateAsync(request);

        // Assert
        result.IsRight.Should().BeTrue();
        var validation = (PrivacyValidationResult)result;
        validation.IsCompliant.Should().BeFalse();
        validation.Violations.Should().Contain(v =>
            v.ViolationType == PrivacyViolationType.DataMinimization
            && v.FieldName == "ReferralSource");
    }

    #endregion

    #region ValidateAsync — ModuleId Propagation

    [Fact]
    public async Task ValidateAsync_WithModuleId_ShouldPassModuleIdToRegistryAndSetOnResult()
    {
        // Arrange
        var request = new OrderCommand { ProductId = "P1", Quantity = 1 };
        SetupAnalyzerReturnsCompliantReport();
        SetupAnalyzerReturnsEmptyDefaults();

        _purposeRegistry.GetPurposeAsync("Order Processing", "sales-module", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Option<PurposeDefinition>>(
                Option<PurposeDefinition>.None)));

        // Act
        var result = await _sut.ValidateAsync(request, moduleId: "sales-module");

        // Assert
        result.IsRight.Should().BeTrue();
        var validation = (PrivacyValidationResult)result;
        validation.ModuleId.Should().Be("sales-module");
        await _purposeRegistry.Received(1).GetPurposeAsync("Order Processing", "sales-module", Arg.Any<CancellationToken>());
    }

    #endregion

    #region ValidateAsync — Exception Handling

    [Fact]
    public async Task ValidateAsync_AnalyzerThrowsException_ShouldReturnLeftWithStoreError()
    {
        // Arrange
        var request = new OrderCommand { ProductId = "P1" };

        _analyzer.AnalyzeAsync(Arg.Any<OrderCommand>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Analyzer failed"));

        // Act
        var result = await _sut.ValidateAsync(request);

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = (EncinaError)result;
        error.GetEncinaCode().Should().Be(PrivacyByDesignErrors.StoreErrorCode);
        error.Message.Should().Contain("Analyzer failed");
    }

    #endregion

    #region ValidateAsync — ValidatedAtUtc

    [Fact]
    public async Task ValidateAsync_ShouldSetValidatedAtUtcFromTimeProvider()
    {
        // Arrange
        var request = new OrderCommand { ProductId = "P1" };
        SetupAnalyzerReturnsCompliantReport();
        SetupAnalyzerReturnsEmptyDefaults();

        _purposeRegistry.GetPurposeAsync("Order Processing", Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Option<PurposeDefinition>>(
                Option<PurposeDefinition>.None)));

        // Act
        var result = await _sut.ValidateAsync(request);

        // Assert
        result.IsRight.Should().BeTrue();
        var validation = (PrivacyValidationResult)result;
        validation.ValidatedAtUtc.Should().Be(_timeProvider.GetUtcNow());
    }

    #endregion

    #region ValidateAsync — ModuleId on Result

    [Fact]
    public async Task ValidateAsync_WithoutModuleId_ShouldSetModuleIdToNull()
    {
        // Arrange
        var request = new OrderCommand { ProductId = "P1" };
        SetupAnalyzerReturnsCompliantReport();
        SetupAnalyzerReturnsEmptyDefaults();

        _purposeRegistry.GetPurposeAsync("Order Processing", Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Option<PurposeDefinition>>(
                Option<PurposeDefinition>.None)));

        // Act
        var result = await _sut.ValidateAsync(request);

        // Assert
        result.IsRight.Should().BeTrue();
        var validation = (PrivacyValidationResult)result;
        validation.ModuleId.Should().BeNull();
    }

    #endregion

    #region AnalyzeMinimizationAsync

    [Fact]
    public async Task AnalyzeMinimizationAsync_ShouldDelegateToAnalyzer()
    {
        // Arrange
        var request = new OrderCommand { ProductId = "P1" };
        SetupAnalyzerReturnsCompliantReport();

        // Act
        var result = await _sut.AnalyzeMinimizationAsync(request);

        // Assert
        result.IsRight.Should().BeTrue();
        await _analyzer.Received(1).AnalyzeAsync(request, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AnalyzeMinimizationAsync_NullRequest_ShouldThrowArgumentNullException()
    {
        // Act
        Func<Task> act = async () => await _sut.AnalyzeMinimizationAsync<OrderCommand>(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("request");
    }

    #endregion

    #region ValidatePurposeLimitationAsync — With Registered Purpose

    [Fact]
    public async Task ValidatePurposeLimitationAsync_WithRegisteredPurpose_ShouldValidateAgainstAllowedFields()
    {
        // Arrange
        var request = new OrderCommand
        {
            ProductId = "P1",
            Quantity = 2,
            CampaignCode = "CAMP-1",
        };

        _purposeRegistry.GetPurposeAsync("Order Processing", Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Option<PurposeDefinition>>(
                Some(new PurposeDefinition
                {
                    PurposeId = "test-id",
                    Name = "Order Processing",
                    Description = "Test",
                    LegalBasis = "Contract",
                    AllowedFields = ["ProductId", "Quantity"],
                    CreatedAtUtc = _timeProvider.GetUtcNow(),
                }))));

        // Act
        var result = await _sut.ValidatePurposeLimitationAsync(request, "Order Processing");

        // Assert
        result.IsRight.Should().BeTrue();
        var purposeResult = (PurposeValidationResult)result;
        purposeResult.ViolatingFields.Should().Contain("CampaignCode");
        purposeResult.AllowedFields.Should().Contain("ProductId");
        purposeResult.AllowedFields.Should().Contain("Quantity");
        purposeResult.IsValid.Should().BeFalse();
    }

    #endregion

    #region ValidatePurposeLimitationAsync — Without Registered Purpose (Attribute Fallback)

    [Fact]
    public async Task ValidatePurposeLimitationAsync_WithoutRegisteredPurpose_ShouldFallbackToAttributeValidation()
    {
        // Arrange
        var request = new OrderCommand
        {
            ProductId = "P1",
            CampaignCode = "CAMP-1",
        };

        _purposeRegistry.GetPurposeAsync("Order Processing", Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Option<PurposeDefinition>>(
                Option<PurposeDefinition>.None)));

        // Act
        var result = await _sut.ValidatePurposeLimitationAsync(request, "Order Processing");

        // Assert
        result.IsRight.Should().BeTrue();
        var purposeResult = (PurposeValidationResult)result;
        // CampaignCode has [PurposeLimitation("Marketing")] which != "Order Processing"
        // and it has a non-null value, so it is a violating field.
        purposeResult.ViolatingFields.Should().Contain("CampaignCode");
        purposeResult.IsValid.Should().BeFalse();
    }

    #endregion

    #region ValidatePurposeLimitationAsync — Null Guards

    [Fact]
    public async Task ValidatePurposeLimitationAsync_NullRequest_ShouldThrowArgumentNullException()
    {
        // Act
        Func<Task> act = async () => await _sut.ValidatePurposeLimitationAsync<OrderCommand>(null!, "Order Processing");

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("request");
    }

    [Fact]
    public async Task ValidatePurposeLimitationAsync_NullPurpose_ShouldThrowArgumentNullException()
    {
        // Arrange
        var request = new OrderCommand { ProductId = "P1" };

        // Act
        Func<Task> act = async () => await _sut.ValidatePurposeLimitationAsync(request, null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("purpose");
    }

    #endregion

    #region ValidateDefaultsAsync

    [Fact]
    public async Task ValidateDefaultsAsync_ShouldDelegateToAnalyzerInspectDefaultsAsync()
    {
        // Arrange
        var request = new OrderCommand { ProductId = "P1" };
        SetupAnalyzerReturnsEmptyDefaults();

        // Act
        var result = await _sut.ValidateDefaultsAsync(request);

        // Assert
        result.IsRight.Should().BeTrue();
        await _analyzer.Received(1).InspectDefaultsAsync(request, Arg.Any<CancellationToken>());
    }

    #endregion

    #region Helpers

    private void SetupAnalyzerReturnsCompliantReport()
    {
        _analyzer.AnalyzeAsync(Arg.Any<OrderCommand>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, MinimizationReport>(new MinimizationReport
            {
                RequestTypeName = "OrderCommand",
                NecessaryFields = [new PrivacyFieldInfo("ProductId", null, true)],
                UnnecessaryFields = [],
                MinimizationScore = 1.0,
                Recommendations = [],
                AnalyzedAtUtc = _timeProvider.GetUtcNow(),
            })));
    }

    private void SetupAnalyzerReturnsEmptyDefaults()
    {
        _analyzer.InspectDefaultsAsync(Arg.Any<OrderCommand>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, IReadOnlyList<DefaultPrivacyFieldInfo>>(
                System.Array.Empty<DefaultPrivacyFieldInfo>())));
    }

    #endregion
}
