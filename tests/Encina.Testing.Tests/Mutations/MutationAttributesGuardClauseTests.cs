using Encina.Testing.Mutations;
using Shouldly;
using Xunit;

namespace Encina.Testing.Tests.Mutations;

/// <summary>
/// Guard clause tests for <see cref="NeedsMutationCoverageAttribute"/> and <see cref="MutationKillerAttribute"/>.
/// Verifies that null, empty, and whitespace inputs are properly rejected.
/// </summary>
public sealed class MutationAttributesGuardClauseTests
{
    #region NeedsMutationCoverageAttribute Guard Clauses

    [Fact]
    public void NeedsMutationCoverageAttribute_NullReason_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        var act = () => new NeedsMutationCoverageAttribute(null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("reason");
    }

    [Fact]
    public void NeedsMutationCoverageAttribute_EmptyReason_ShouldThrowArgumentException()
    {
        // Arrange & Act
        var act = () => new NeedsMutationCoverageAttribute(string.Empty);

        // Assert
        var ex = Should.Throw<ArgumentException>(act);
        ex.ParamName.ShouldBe("reason");
    }

    [Fact]
    public void NeedsMutationCoverageAttribute_WhitespaceReason_ShouldThrowArgumentException()
    {
        // Arrange & Act
        var act = () => new NeedsMutationCoverageAttribute("   ");

        // Assert
        var ex = Should.Throw<ArgumentException>(act);
        ex.ParamName.ShouldBe("reason");
    }

    [Theory]
    [InlineData("\t")]
    [InlineData("\n")]
    [InlineData("\r\n")]
    [InlineData("  \t  ")]
    public void NeedsMutationCoverageAttribute_VariousWhitespace_ShouldThrowArgumentException(string whitespace)
    {
        // Arrange & Act
        var act = () => new NeedsMutationCoverageAttribute(whitespace);

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    #endregion

    #region MutationKillerAttribute Guard Clauses

    [Fact]
    public void MutationKillerAttribute_NullMutationType_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        var act = () => new MutationKillerAttribute(null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("mutationType");
    }

    [Fact]
    public void MutationKillerAttribute_EmptyMutationType_ShouldThrowArgumentException()
    {
        // Arrange & Act
        var act = () => new MutationKillerAttribute(string.Empty);

        // Assert
        var ex = Should.Throw<ArgumentException>(act);
        ex.ParamName.ShouldBe("mutationType");
    }

    [Fact]
    public void MutationKillerAttribute_WhitespaceMutationType_ShouldThrowArgumentException()
    {
        // Arrange & Act
        var act = () => new MutationKillerAttribute("   ");

        // Assert
        var ex = Should.Throw<ArgumentException>(act);
        ex.ParamName.ShouldBe("mutationType");
    }

    [Theory]
    [InlineData("\t")]
    [InlineData("\n")]
    [InlineData("\r\n")]
    [InlineData("  \t  ")]
    public void MutationKillerAttribute_VariousWhitespace_ShouldThrowArgumentException(string whitespace)
    {
        // Arrange & Act
        var act = () => new MutationKillerAttribute(whitespace);

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void NeedsMutationCoverageAttribute_SingleCharacterReason_ShouldSucceed()
    {
        // Arrange & Act
        var attribute = new NeedsMutationCoverageAttribute("X");

        // Assert
        attribute.Reason.ShouldBe("X");
    }

    [Fact]
    public void MutationKillerAttribute_SingleCharacterMutationType_ShouldSucceed()
    {
        // Arrange & Act
        var attribute = new MutationKillerAttribute("X");

        // Assert
        attribute.MutationType.ShouldBe("X");
    }

    [Fact]
    public void NeedsMutationCoverageAttribute_VeryLongReason_ShouldSucceed()
    {
        // Arrange
        var longReason = new string('X', 10000);

        // Act
        var attribute = new NeedsMutationCoverageAttribute(longReason);

        // Assert
        attribute.Reason.ShouldBe(longReason);
        attribute.Reason.Length.ShouldBe(10000);
    }

    [Fact]
    public void MutationKillerAttribute_VeryLongMutationType_ShouldSucceed()
    {
        // Arrange
        var longType = new string('Y', 10000);

        // Act
        var attribute = new MutationKillerAttribute(longType);

        // Assert
        attribute.MutationType.ShouldBe(longType);
        attribute.MutationType.Length.ShouldBe(10000);
    }

    [Fact]
    public void NeedsMutationCoverageAttribute_ReasonWithLeadingWhitespace_ShouldPreserve()
    {
        // Arrange - leading/trailing whitespace in reason is valid (content + whitespace)
        var reason = "  Reason with spaces  ";

        // Act
        var attribute = new NeedsMutationCoverageAttribute(reason);

        // Assert
        attribute.Reason.ShouldBe(reason);
    }

    [Fact]
    public void MutationKillerAttribute_MutationTypeWithLeadingWhitespace_ShouldPreserve()
    {
        // Arrange - leading/trailing whitespace in mutation type is valid (content + whitespace)
        var mutationType = "  EqualityMutation  ";

        // Act
        var attribute = new MutationKillerAttribute(mutationType);

        // Assert
        attribute.MutationType.ShouldBe(mutationType);
    }

    [Fact]
    public void NeedsMutationCoverageAttribute_SpecialCharacters_ShouldSucceed()
    {
        // Arrange
        var reason = "Mutation at line 45: if (x >= 0) → if (x > 0) — árithmetic µtation";

        // Act
        var attribute = new NeedsMutationCoverageAttribute(reason);

        // Assert
        attribute.Reason.ShouldBe(reason);
    }

    [Fact]
    public void MutationKillerAttribute_SpecialCharacters_ShouldSucceed()
    {
        // Arrange
        var mutationType = "Égality_Mutation-2025";

        // Act
        var attribute = new MutationKillerAttribute(mutationType);

        // Assert
        attribute.MutationType.ShouldBe(mutationType);
    }

    #endregion
}
