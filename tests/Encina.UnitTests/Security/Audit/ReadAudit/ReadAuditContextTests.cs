using Encina.Security.Audit;
using FluentAssertions;

namespace Encina.UnitTests.Security.Audit.ReadAudit;

/// <summary>
/// Unit tests for <see cref="ReadAuditContext"/>.
/// </summary>
public class ReadAuditContextTests
{
    #region Purpose Tests

    [Fact]
    public void Purpose_InitialValue_ShouldBeNull()
    {
        // Arrange & Act
        var context = new ReadAuditContext();

        // Assert
        context.Purpose.Should().BeNull();
    }

    [Fact]
    public void WithPurpose_ValidPurpose_ShouldSetPurpose()
    {
        // Arrange
        var context = new ReadAuditContext();

        // Act
        context.WithPurpose("Patient care review");

        // Assert
        context.Purpose.Should().Be("Patient care review");
    }

    [Fact]
    public void WithPurpose_ShouldReturnSameInstanceForFluentChaining()
    {
        // Arrange
        var context = new ReadAuditContext();

        // Act
        var result = context.WithPurpose("Some purpose");

        // Assert
        result.Should().BeSameAs(context);
    }

    [Fact]
    public void WithPurpose_CalledTwice_ShouldOverwritePreviousPurpose()
    {
        // Arrange
        var context = new ReadAuditContext();

        // Act
        context.WithPurpose("First purpose");
        context.WithPurpose("Second purpose");

        // Assert
        context.Purpose.Should().Be("Second purpose");
    }

    [Fact]
    public void WithPurpose_NullPurpose_ShouldThrowArgumentException()
    {
        // Arrange
        var context = new ReadAuditContext();

        // Act
        var act = () => context.WithPurpose(null!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void WithPurpose_EmptyPurpose_ShouldThrowArgumentException()
    {
        // Arrange
        var context = new ReadAuditContext();

        // Act
        var act = () => context.WithPurpose(string.Empty);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void WithPurpose_WhitespacePurpose_ShouldThrowArgumentException()
    {
        // Arrange
        var context = new ReadAuditContext();

        // Act
        var act = () => context.WithPurpose("   ");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    #endregion
}
