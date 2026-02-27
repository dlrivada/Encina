using Encina.Compliance.DataSubjectRights;
using FluentAssertions;
using LanguageExt;
using Microsoft.Extensions.Logging;
using NSubstitute;

using static LanguageExt.Prelude;

#pragma warning disable CA2012 // Use ValueTasks correctly

namespace Encina.UnitTests.Compliance.DataSubjectRights;

/// <summary>
/// Unit tests for <see cref="DefaultDataErasureExecutor"/>.
/// </summary>
public class DefaultDataErasureExecutorTests
{
    private readonly IPersonalDataLocator _locator;
    private readonly IDataErasureStrategy _strategy;
    private readonly ILogger<DefaultDataErasureExecutor> _logger;
    private readonly DefaultDataErasureExecutor _executor;

    public DefaultDataErasureExecutorTests()
    {
        _locator = Substitute.For<IPersonalDataLocator>();
        _strategy = Substitute.For<IDataErasureStrategy>();
        _logger = Substitute.For<ILogger<DefaultDataErasureExecutor>>();
        _executor = new DefaultDataErasureExecutor(_locator, _strategy, _logger);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullLocator_ShouldThrow()
    {
        var act = () => new DefaultDataErasureExecutor(null!, _strategy, _logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("locator");
    }

    [Fact]
    public void Constructor_NullStrategy_ShouldThrow()
    {
        var act = () => new DefaultDataErasureExecutor(_locator, null!, _logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("strategy");
    }

    [Fact]
    public void Constructor_NullLogger_ShouldThrow()
    {
        var act = () => new DefaultDataErasureExecutor(_locator, _strategy, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    #endregion

    #region EraseAsync Tests - No Data

    [Fact]
    public async Task EraseAsync_NoDataFound_ShouldReturnEmptyResult()
    {
        // Arrange
        SetupLocator("subject-1", []);
        var scope = new ErasureScope { Reason = ErasureReason.ConsentWithdrawn };

        // Act
        var result = await _executor.EraseAsync("subject-1", scope);

        // Assert
        result.IsRight.Should().BeTrue();
        var erasureResult = (ErasureResult)result;
        erasureResult.FieldsErased.Should().Be(0);
        erasureResult.FieldsRetained.Should().Be(0);
        erasureResult.FieldsFailed.Should().Be(0);
    }

    #endregion

    #region EraseAsync Tests - Erasable Data

    [Fact]
    public async Task EraseAsync_AllErasable_ShouldEraseAll()
    {
        // Arrange
        var locations = new List<PersonalDataLocation>
        {
            CreateLocation("Email", isErasable: true),
            CreateLocation("Phone", isErasable: true),
        };
        SetupLocator("subject-1", locations);
        SetupStrategySuccess();
        var scope = new ErasureScope { Reason = ErasureReason.ConsentWithdrawn };

        // Act
        var result = await _executor.EraseAsync("subject-1", scope);

        // Assert
        result.IsRight.Should().BeTrue();
        var erasureResult = (ErasureResult)result;
        erasureResult.FieldsErased.Should().Be(2);
        erasureResult.FieldsRetained.Should().Be(0);
        erasureResult.FieldsFailed.Should().Be(0);
    }

    [Fact]
    public async Task EraseAsync_MixedErasableAndRetained_ShouldPartitionCorrectly()
    {
        // Arrange
        var locations = new List<PersonalDataLocation>
        {
            CreateLocation("Email", isErasable: true),
            CreateLocation("TaxId", isErasable: false, hasLegalRetention: true),
            CreateLocation("Phone", isErasable: true),
        };
        SetupLocator("subject-1", locations);
        SetupStrategySuccess();
        var scope = new ErasureScope { Reason = ErasureReason.ConsentWithdrawn };

        // Act
        var result = await _executor.EraseAsync("subject-1", scope);

        // Assert
        result.IsRight.Should().BeTrue();
        var erasureResult = (ErasureResult)result;
        erasureResult.FieldsErased.Should().Be(2);
        erasureResult.FieldsRetained.Should().Be(1);
        erasureResult.RetentionReasons.Should().HaveCount(1);
        erasureResult.RetentionReasons[0].FieldName.Should().Be("TaxId");
    }

    #endregion

    #region EraseAsync Tests - Legal Retention

    [Fact]
    public async Task EraseAsync_LegalRetention_ShouldRetainAndDocumentReason()
    {
        // Arrange
        var locations = new List<PersonalDataLocation>
        {
            CreateLocation("InvoiceRef", isErasable: true, hasLegalRetention: true),
        };
        SetupLocator("subject-1", locations);
        var scope = new ErasureScope { Reason = ErasureReason.NoLongerNecessary };

        // Act
        var result = await _executor.EraseAsync("subject-1", scope);

        // Assert
        result.IsRight.Should().BeTrue();
        var erasureResult = (ErasureResult)result;
        erasureResult.FieldsErased.Should().Be(0);
        erasureResult.FieldsRetained.Should().Be(1);
        erasureResult.RetentionReasons.Should().ContainSingle();
        erasureResult.Exemptions.Should().Contain(ErasureExemption.LegalObligation);
    }

    [Fact]
    public async Task EraseAsync_NonErasableField_ShouldRetainAndDocumentReason()
    {
        // Arrange
        var locations = new List<PersonalDataLocation>
        {
            CreateLocation("SystemId", isErasable: false),
        };
        SetupLocator("subject-1", locations);
        var scope = new ErasureScope { Reason = ErasureReason.ConsentWithdrawn };

        // Act
        var result = await _executor.EraseAsync("subject-1", scope);

        // Assert
        result.IsRight.Should().BeTrue();
        var erasureResult = (ErasureResult)result;
        erasureResult.FieldsRetained.Should().Be(1);
        erasureResult.RetentionReasons[0].Reason.Should().Contain("not erasable");
    }

    #endregion

    #region EraseAsync Tests - Strategy Failure

    [Fact]
    public async Task EraseAsync_StrategyFailsForSomeFields_ShouldCountFailed()
    {
        // Arrange
        var locations = new List<PersonalDataLocation>
        {
            CreateLocation("Email", isErasable: true),
            CreateLocation("Phone", isErasable: true),
        };
        SetupLocator("subject-1", locations);

        // First field succeeds, second fails
        _strategy.EraseFieldAsync(Arg.Is<PersonalDataLocation>(l => l.FieldName == "Email"), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Unit>>(unit));
        _strategy.EraseFieldAsync(Arg.Is<PersonalDataLocation>(l => l.FieldName == "Phone"), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Unit>>(DSRErrors.ErasureFailed("subject-1", "DB error")));

        var scope = new ErasureScope { Reason = ErasureReason.ConsentWithdrawn };

        // Act
        var result = await _executor.EraseAsync("subject-1", scope);

        // Assert
        result.IsRight.Should().BeTrue();
        var erasureResult = (ErasureResult)result;
        erasureResult.FieldsErased.Should().Be(1);
        erasureResult.FieldsFailed.Should().Be(1);
    }

    #endregion

    #region EraseAsync Tests - Scope Filtering

    [Fact]
    public async Task EraseAsync_WithCategoryScope_ShouldFilterByCategory()
    {
        // Arrange
        var locations = new List<PersonalDataLocation>
        {
            CreateLocation("Email", isErasable: true, category: PersonalDataCategory.Contact),
            CreateLocation("SSN", isErasable: true, category: PersonalDataCategory.Identity),
        };
        SetupLocator("subject-1", locations);
        SetupStrategySuccess();
        var scope = new ErasureScope
        {
            Reason = ErasureReason.ConsentWithdrawn,
            Categories = [PersonalDataCategory.Contact]
        };

        // Act
        var result = await _executor.EraseAsync("subject-1", scope);

        // Assert
        result.IsRight.Should().BeTrue();
        var erasureResult = (ErasureResult)result;
        erasureResult.FieldsErased.Should().Be(1); // Only Contact field
    }

    [Fact]
    public async Task EraseAsync_WithSpecificFieldsScope_ShouldFilterByFieldName()
    {
        // Arrange
        var locations = new List<PersonalDataLocation>
        {
            CreateLocation("Email", isErasable: true),
            CreateLocation("Phone", isErasable: true),
            CreateLocation("Name", isErasable: true),
        };
        SetupLocator("subject-1", locations);
        SetupStrategySuccess();
        var scope = new ErasureScope
        {
            Reason = ErasureReason.ConsentWithdrawn,
            SpecificFields = ["Email", "Name"]
        };

        // Act
        var result = await _executor.EraseAsync("subject-1", scope);

        // Assert
        result.IsRight.Should().BeTrue();
        var erasureResult = (ErasureResult)result;
        erasureResult.FieldsErased.Should().Be(2); // Email and Name only
    }

    #endregion

    #region EraseAsync Tests - Locator Failure

    [Fact]
    public async Task EraseAsync_LocatorFails_ShouldReturnError()
    {
        // Arrange
        var error = DSRErrors.LocatorFailed("subject-1", "Connection timeout");
        _locator.LocateAllDataAsync("subject-1", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, IReadOnlyList<PersonalDataLocation>>>(error));

        var scope = new ErasureScope { Reason = ErasureReason.ConsentWithdrawn };

        // Act
        var result = await _executor.EraseAsync("subject-1", scope);

        // Assert
        result.IsLeft.Should().BeTrue();
        var resultError = result.LeftAsEnumerable().First();
        resultError.GetCode().Match(
            Some: code => code.Should().Be(DSRErrors.LocatorFailedCode),
            None: () => Assert.Fail("Expected error code"));
    }

    #endregion

    #region EraseAsync Tests - Exemptions

    [Fact]
    public async Task EraseAsync_WithExplicitExemptions_ShouldUseProvidedExemptions()
    {
        // Arrange
        var locations = new List<PersonalDataLocation>
        {
            CreateLocation("Email", isErasable: true),
        };
        SetupLocator("subject-1", locations);
        SetupStrategySuccess();
        var scope = new ErasureScope
        {
            Reason = ErasureReason.ConsentWithdrawn,
            ExemptionsToApply = [ErasureExemption.PublicHealth, ErasureExemption.LegalClaims]
        };

        // Act
        var result = await _executor.EraseAsync("subject-1", scope);

        // Assert
        result.IsRight.Should().BeTrue();
        var erasureResult = (ErasureResult)result;
        erasureResult.Exemptions.Should().HaveCount(2);
        erasureResult.Exemptions.Should().Contain(ErasureExemption.PublicHealth);
        erasureResult.Exemptions.Should().Contain(ErasureExemption.LegalClaims);
    }

    #endregion

    #region Helpers

    private void SetupLocator(string subjectId, List<PersonalDataLocation> locations)
    {
        IReadOnlyList<PersonalDataLocation> readOnly = locations.AsReadOnly();
        _locator.LocateAllDataAsync(subjectId, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, IReadOnlyList<PersonalDataLocation>>>(Right(readOnly)));
    }

    private void SetupStrategySuccess()
    {
        _strategy.EraseFieldAsync(Arg.Any<PersonalDataLocation>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Unit>>(unit));
    }

    private static PersonalDataLocation CreateLocation(
        string fieldName,
        bool isErasable = true,
        bool hasLegalRetention = false,
        bool isPortable = false,
        PersonalDataCategory category = PersonalDataCategory.Contact) =>
        new()
        {
            EntityType = typeof(object),
            EntityId = "entity-001",
            FieldName = fieldName,
            Category = category,
            IsErasable = isErasable,
            IsPortable = isPortable,
            HasLegalRetention = hasLegalRetention,
            CurrentValue = $"value-{fieldName}"
        };

    #endregion
}
