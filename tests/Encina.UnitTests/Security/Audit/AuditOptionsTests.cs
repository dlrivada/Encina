using Encina.Security.Audit;
using FluentAssertions;

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
        options.AuditAllCommands.Should().BeTrue();
        options.AuditAllQueries.Should().BeFalse();
        options.IncludePayloadHash.Should().BeTrue();
        options.RetentionDays.Should().Be(2555); // ~7 years for SOX compliance
    }

    [Fact]
    public void ExcludedTypes_ShouldBeEmptyByDefault()
    {
        // Act
        var options = new AuditOptions();

        // Assert
        options.ExcludedTypes.Should().BeEmpty();
    }

    [Fact]
    public void IncludedQueryTypes_ShouldBeEmptyByDefault()
    {
        // Act
        var options = new AuditOptions();

        // Assert
        options.IncludedQueryTypes.Should().BeEmpty();
    }

    [Fact]
    public void ExcludeType_Generic_ShouldAddToExcludedTypes()
    {
        // Arrange
        var options = new AuditOptions();

        // Act
        options.ExcludeType<TestCommand>();

        // Assert
        options.ExcludedTypes.Should().Contain(typeof(TestCommand));
        options.IsExcluded(typeof(TestCommand)).Should().BeTrue();
    }

    [Fact]
    public void ExcludeType_NonGeneric_ShouldAddToExcludedTypes()
    {
        // Arrange
        var options = new AuditOptions();

        // Act
        options.ExcludeType(typeof(TestCommand));

        // Assert
        options.ExcludedTypes.Should().Contain(typeof(TestCommand));
        options.IsExcluded(typeof(TestCommand)).Should().BeTrue();
    }

    [Fact]
    public void ExcludeType_Generic_ShouldReturnOptionsForChaining()
    {
        // Arrange
        var options = new AuditOptions();

        // Act
        var result = options.ExcludeType<TestCommand>();

        // Assert
        result.Should().BeSameAs(options);
    }

    [Fact]
    public void ExcludeType_ShouldSupportFluentChaining()
    {
        // Arrange & Act
        var options = new AuditOptions()
            .ExcludeType<TestCommand>()
            .ExcludeType<TestQuery>();

        // Assert
        options.ExcludedTypes.Should().HaveCount(2);
        options.IsExcluded(typeof(TestCommand)).Should().BeTrue();
        options.IsExcluded(typeof(TestQuery)).Should().BeTrue();
    }

    [Fact]
    public void IncludeQueryType_Generic_ShouldAddToIncludedQueryTypes()
    {
        // Arrange
        var options = new AuditOptions();

        // Act
        options.IncludeQueryType<TestQuery>();

        // Assert
        options.IncludedQueryTypes.Should().Contain(typeof(TestQuery));
        options.IsQueryIncluded(typeof(TestQuery)).Should().BeTrue();
    }

    [Fact]
    public void IncludeQueryType_NonGeneric_ShouldAddToIncludedQueryTypes()
    {
        // Arrange
        var options = new AuditOptions();

        // Act
        options.IncludeQueryType(typeof(TestQuery));

        // Assert
        options.IncludedQueryTypes.Should().Contain(typeof(TestQuery));
        options.IsQueryIncluded(typeof(TestQuery)).Should().BeTrue();
    }

    [Fact]
    public void IncludeQueryType_Generic_ShouldReturnOptionsForChaining()
    {
        // Arrange
        var options = new AuditOptions();

        // Act
        var result = options.IncludeQueryType<TestQuery>();

        // Assert
        result.Should().BeSameAs(options);
    }

    [Fact]
    public void IsExcluded_WhenTypeNotExcluded_ShouldReturnFalse()
    {
        // Arrange
        var options = new AuditOptions();

        // Act & Assert
        options.IsExcluded(typeof(TestCommand)).Should().BeFalse();
    }

    [Fact]
    public void IsQueryIncluded_WhenTypeNotIncluded_ShouldReturnFalse()
    {
        // Arrange
        var options = new AuditOptions();

        // Act & Assert
        options.IsQueryIncluded(typeof(TestQuery)).Should().BeFalse();
    }

    [Fact]
    public void ExcludeType_NonGeneric_WithNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = new AuditOptions();

        // Act
        var act = () => options.ExcludeType(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("requestType");
    }

    [Fact]
    public void IncludeQueryType_NonGeneric_WithNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = new AuditOptions();

        // Act
        var act = () => options.IncludeQueryType(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("queryType");
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
        options.ExcludedTypes.Should().HaveCount(1);
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
        options.AuditAllCommands.Should().BeFalse();
        options.AuditAllQueries.Should().BeTrue();
        options.IncludePayloadHash.Should().BeFalse();
        options.RetentionDays.Should().Be(365);
    }

    private sealed class TestCommand { }
    private sealed class TestQuery { }
}
