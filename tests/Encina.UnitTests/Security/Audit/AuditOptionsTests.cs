using Encina.Security.Audit;
using Shouldly;

namespace Encina.UnitTests.Security.Audit;

/// <summary>
/// Unit tests for <see cref="AuditOptions"/>.
/// </summary>
public class AuditOptionsTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        // Act
        var options = new AuditOptions();

        // Assert
        options.AuditAllCommands.ShouldBeTrue();
        options.AuditAllQueries.ShouldBeFalse();
        options.IncludePayloadHash.ShouldBeTrue();
        options.IncludeRequestPayload.ShouldBeFalse();
        options.IncludeResponsePayload.ShouldBeFalse();
        options.MaxPayloadSizeBytes.ShouldBe(65536); // 64 KB
        options.GlobalSensitiveFields.ShouldBeNull();
        options.EnableAutoPurge.ShouldBeFalse();
        options.PurgeIntervalHours.ShouldBe(24);
        options.RetentionDays.ShouldBe(2555); // ~7 years for SOX compliance
    }

    [Fact]
    public void ExcludedTypes_ShouldBeEmptyByDefault()
    {
        // Act
        var options = new AuditOptions();

        // Assert
        options.ExcludedTypes.ShouldBeEmpty();
    }

    [Fact]
    public void IncludedQueryTypes_ShouldBeEmptyByDefault()
    {
        // Act
        var options = new AuditOptions();

        // Assert
        options.IncludedQueryTypes.ShouldBeEmpty();
    }

    [Fact]
    public void ExcludeType_Generic_ShouldAddToExcludedTypes()
    {
        // Arrange
        var options = new AuditOptions();

        // Act
        options.ExcludeType<TestCommand>();

        // Assert
        options.ExcludedTypes.ShouldContain(typeof(TestCommand));
        options.IsExcluded(typeof(TestCommand)).ShouldBeTrue();
    }

    [Fact]
    public void ExcludeType_NonGeneric_ShouldAddToExcludedTypes()
    {
        // Arrange
        var options = new AuditOptions();

        // Act
        options.ExcludeType(typeof(TestCommand));

        // Assert
        options.ExcludedTypes.ShouldContain(typeof(TestCommand));
        options.IsExcluded(typeof(TestCommand)).ShouldBeTrue();
    }

    [Fact]
    public void ExcludeType_Generic_ShouldReturnOptionsForChaining()
    {
        // Arrange
        var options = new AuditOptions();

        // Act
        var result = options.ExcludeType<TestCommand>();

        // Assert
        result.ShouldBeSameAs(options);
    }

    [Fact]
    public void ExcludeType_ShouldSupportFluentChaining()
    {
        // Arrange & Act
        var options = new AuditOptions()
            .ExcludeType<TestCommand>()
            .ExcludeType<TestQuery>();

        // Assert
        options.ExcludedTypes.Count.ShouldBe(2);
        options.IsExcluded(typeof(TestCommand)).ShouldBeTrue();
        options.IsExcluded(typeof(TestQuery)).ShouldBeTrue();
    }

    [Fact]
    public void IncludeQueryType_Generic_ShouldAddToIncludedQueryTypes()
    {
        // Arrange
        var options = new AuditOptions();

        // Act
        options.IncludeQueryType<TestQuery>();

        // Assert
        options.IncludedQueryTypes.ShouldContain(typeof(TestQuery));
        options.IsQueryIncluded(typeof(TestQuery)).ShouldBeTrue();
    }

    [Fact]
    public void IncludeQueryType_NonGeneric_ShouldAddToIncludedQueryTypes()
    {
        // Arrange
        var options = new AuditOptions();

        // Act
        options.IncludeQueryType(typeof(TestQuery));

        // Assert
        options.IncludedQueryTypes.ShouldContain(typeof(TestQuery));
        options.IsQueryIncluded(typeof(TestQuery)).ShouldBeTrue();
    }

    [Fact]
    public void IncludeQueryType_Generic_ShouldReturnOptionsForChaining()
    {
        // Arrange
        var options = new AuditOptions();

        // Act
        var result = options.IncludeQueryType<TestQuery>();

        // Assert
        result.ShouldBeSameAs(options);
    }

    [Fact]
    public void IsExcluded_WhenTypeNotExcluded_ShouldReturnFalse()
    {
        // Arrange
        var options = new AuditOptions();

        // Act & Assert
        options.IsExcluded(typeof(TestCommand)).ShouldBeFalse();
    }

    [Fact]
    public void IsQueryIncluded_WhenTypeNotIncluded_ShouldReturnFalse()
    {
        // Arrange
        var options = new AuditOptions();

        // Act & Assert
        options.IsQueryIncluded(typeof(TestQuery)).ShouldBeFalse();
    }

    [Fact]
    public void ExcludeType_NonGeneric_WithNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = new AuditOptions();

        // Act
        var act = () => options.ExcludeType(null!);

        // Assert
        Should.Throw<ArgumentNullException>(act)
                .ParamName.ShouldBe("requestType");
    }

    [Fact]
    public void IncludeQueryType_NonGeneric_WithNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = new AuditOptions();

        // Act
        var act = () => options.IncludeQueryType(null!);

        // Assert
        Should.Throw<ArgumentNullException>(act)
                .ParamName.ShouldBe("queryType");
    }

    [Fact]
    public void ExcludeType_SameTypeTwice_ShouldNotDuplicate()
    {
        // Arrange
        var options = new AuditOptions();

        // Act
        options.ExcludeType<TestCommand>();
        options.ExcludeType<TestCommand>();

        // Assert - HashSet semantics prevent duplicates
        options.ExcludedTypes.Count.ShouldBe(1);
    }

    [Fact]
    public void Properties_ShouldBeSettable()
    {
        // Arrange & Act
        var options = new AuditOptions
        {
            AuditAllCommands = false,
            AuditAllQueries = true,
            IncludePayloadHash = false,
            RetentionDays = 365
        };

        // Assert
        options.AuditAllCommands.ShouldBeFalse();
        options.AuditAllQueries.ShouldBeTrue();
        options.IncludePayloadHash.ShouldBeFalse();
        options.RetentionDays.ShouldBe(365);
    }

    [Fact]
    public void EnableAutoPurge_ShouldBeSettable()
    {
        // Arrange & Act
        var options = new AuditOptions
        {
            EnableAutoPurge = true,
            PurgeIntervalHours = 12
        };

        // Assert
        options.EnableAutoPurge.ShouldBeTrue();
        options.PurgeIntervalHours.ShouldBe(12);
    }

    [Fact]
    public void PayloadSettings_ShouldBeSettable()
    {
        // Arrange & Act
        var options = new AuditOptions
        {
            IncludeRequestPayload = true,
            IncludeResponsePayload = true,
            MaxPayloadSizeBytes = 131072 // 128 KB
        };

        // Assert
        options.IncludeRequestPayload.ShouldBeTrue();
        options.IncludeResponsePayload.ShouldBeTrue();
        options.MaxPayloadSizeBytes.ShouldBe(131072);
    }

    [Fact]
    public void GlobalSensitiveFields_ShouldBeSettable()
    {
        // Arrange & Act
        var options = new AuditOptions
        {
            GlobalSensitiveFields = ["customField", "dateOfBirth", "taxId"]
        };

        // Assert
        options.GlobalSensitiveFields.ShouldNotBeNull();
        options.GlobalSensitiveFields.Count.ShouldBe(3);
        options.GlobalSensitiveFields.ShouldContain("customField");
        options.GlobalSensitiveFields.ShouldContain("dateOfBirth");
        options.GlobalSensitiveFields.ShouldContain("taxId");
    }

    private sealed class TestCommand { }
    private sealed class TestQuery { }
}
