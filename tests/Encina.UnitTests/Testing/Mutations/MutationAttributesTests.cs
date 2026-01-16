using System.Reflection;
using Encina.Testing;
using Encina.Testing.Mutations;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.Testing.Mutations;

/// <summary>
/// Unit tests for <see cref="NeedsMutationCoverageAttribute"/> and <see cref="MutationKillerAttribute"/>.
/// </summary>
public sealed class MutationAttributesTests
{
    #region NeedsMutationCoverageAttribute Tests

    [Fact]
    public void NeedsMutationCoverageAttribute_WithValidReason_ShouldCreateInstance()
    {
        // Arrange & Act
        var attribute = new NeedsMutationCoverageAttribute("Boundary condition not verified");

        // Assert
        attribute.Reason.ShouldBe("Boundary condition not verified");
    }

    [Fact]
    public void NeedsMutationCoverageAttribute_ShouldHaveCorrectAttributeUsage()
    {
        // Arrange & Act
        var attributeUsage = typeof(NeedsMutationCoverageAttribute).GetCustomAttribute<AttributeUsageAttribute>();

        // Assert
        attributeUsage.ShouldNotBeNull();
        attributeUsage!.ValidOn.ShouldBe(AttributeTargets.Method);
        attributeUsage.AllowMultiple.ShouldBeTrue();
        attributeUsage.Inherited.ShouldBeFalse();
    }

    [Fact]
    public void NeedsMutationCoverageAttribute_ShouldBeSealed()
    {
        // Assert
        typeof(NeedsMutationCoverageAttribute).IsSealed.ShouldBeTrue();
    }

    [Fact]
    public void NeedsMutationCoverageAttribute_OptionalProperties_ShouldHaveDefaultNull()
    {
        // Arrange & Act
        var attribute = new NeedsMutationCoverageAttribute("Test reason");

        // Assert
        attribute.MutantId.ShouldBeNull();
        attribute.SourceFile.ShouldBeNull();
        attribute.Line.ShouldBeNull();
    }

    [Fact]
    public void NeedsMutationCoverageAttribute_WithOptionalProperties_ShouldSetValues()
    {
        // Arrange & Act
        var attribute = new NeedsMutationCoverageAttribute("Boundary condition")
        {
            MutantId = "280",
            SourceFile = "src/Calculator.cs",
            Line = 45
        };

        // Assert
        attribute.Reason.ShouldBe("Boundary condition");
        attribute.MutantId.ShouldBe("280");
        attribute.SourceFile.ShouldBe("src/Calculator.cs");
        attribute.Line.ShouldBe(45);
    }

    [Theory]
    [InlineData("Arithmetic mutation on line 45")]
    [InlineData("Missing boundary check for >= operator")]
    [InlineData("Null check mutation survived")]
    public void NeedsMutationCoverageAttribute_VariousReasons_ShouldBeValid(string reason)
    {
        // Arrange & Act
        var attribute = new NeedsMutationCoverageAttribute(reason);

        // Assert
        attribute.Reason.ShouldBe(reason);
    }

    #endregion

    #region MutationKillerAttribute Tests

    [Fact]
    public void MutationKillerAttribute_WithValidMutationType_ShouldCreateInstance()
    {
        // Arrange & Act
        var attribute = new MutationKillerAttribute("EqualityMutation");

        // Assert
        attribute.MutationType.ShouldBe("EqualityMutation");
    }

    [Fact]
    public void MutationKillerAttribute_ShouldHaveCorrectAttributeUsage()
    {
        // Arrange & Act
        var attributeUsage = typeof(MutationKillerAttribute).GetCustomAttribute<AttributeUsageAttribute>();

        // Assert
        attributeUsage.ShouldNotBeNull();
        attributeUsage!.ValidOn.ShouldBe(AttributeTargets.Method);
        attributeUsage.AllowMultiple.ShouldBeTrue();
        attributeUsage.Inherited.ShouldBeFalse();
    }

    [Fact]
    public void MutationKillerAttribute_ShouldBeSealed()
    {
        // Assert
        typeof(MutationKillerAttribute).IsSealed.ShouldBeTrue();
    }

    [Fact]
    public void MutationKillerAttribute_OptionalProperties_ShouldHaveDefaultNull()
    {
        // Arrange & Act
        var attribute = new MutationKillerAttribute("ArithmeticMutation");

        // Assert
        attribute.Description.ShouldBeNull();
        attribute.SourceFile.ShouldBeNull();
        attribute.TargetMethod.ShouldBeNull();
        attribute.Line.ShouldBeNull();
    }

    [Fact]
    public void MutationKillerAttribute_WithOptionalProperties_ShouldSetValues()
    {
        // Arrange & Act
        var attribute = new MutationKillerAttribute("EqualityMutation")
        {
            Description = "Verifies >= is not mutated to >",
            SourceFile = "src/Person.cs",
            TargetMethod = "IsAdult",
            Line = 25
        };

        // Assert
        attribute.MutationType.ShouldBe("EqualityMutation");
        attribute.Description.ShouldBe("Verifies >= is not mutated to >");
        attribute.SourceFile.ShouldBe("src/Person.cs");
        attribute.TargetMethod.ShouldBe("IsAdult");
        attribute.Line.ShouldBe(25);
    }

    [Theory]
    [InlineData("EqualityMutation")]
    [InlineData("ArithmeticMutation")]
    [InlineData("BooleanMutation")]
    [InlineData("NullCheckMutation")]
    [InlineData("StringMutation")]
    [InlineData("LinqMutation")]
    [InlineData("UnaryMutation")]
    [InlineData("BlockRemoval")]
    public void MutationKillerAttribute_VariousMutationTypes_ShouldBeValid(string mutationType)
    {
        // Arrange & Act
        var attribute = new MutationKillerAttribute(mutationType);

        // Assert
        attribute.MutationType.ShouldBe(mutationType);
    }

    #endregion

    #region Applied Attribute Tests

    [Fact]
    [NeedsMutationCoverage("Example test showing attribute usage")]
    public void ExampleTest_WithNeedsMutationCoverage_ShouldHaveAttribute()
    {
        // Arrange
        var method = typeof(MutationAttributesTests).GetMethod(nameof(ExampleTest_WithNeedsMutationCoverage_ShouldHaveAttribute));

        // Act
        var attribute = method!.GetCustomAttributes(typeof(NeedsMutationCoverageAttribute), false)
            .Cast<NeedsMutationCoverageAttribute>()
            .SingleOrDefault();

        // Assert
        attribute.ShouldNotBeNull();
        attribute.Reason.ShouldBe("Example test showing attribute usage");
    }

    [Fact]
    [MutationKiller("EqualityMutation", Description = "Boundary test example")]
    public void ExampleTest_WithMutationKiller_ShouldHaveAttribute()
    {
        // Arrange
        var method = typeof(MutationAttributesTests).GetMethod(nameof(ExampleTest_WithMutationKiller_ShouldHaveAttribute));

        // Act
        var attribute = method!.GetCustomAttributes(typeof(MutationKillerAttribute), false)
            .Cast<MutationKillerAttribute>()
            .SingleOrDefault();

        // Assert
        attribute.ShouldNotBeNull();
        attribute.MutationType.ShouldBe("EqualityMutation");
        attribute.Description.ShouldBe("Boundary test example");
    }

    [Fact]
    [NeedsMutationCoverage("First issue")]
    [NeedsMutationCoverage("Second issue")]
    public void Test_WithMultipleNeedsMutationCoverage_ShouldHaveBothAttributes()
    {
        // Arrange
        var method = typeof(MutationAttributesTests).GetMethod(nameof(Test_WithMultipleNeedsMutationCoverage_ShouldHaveBothAttributes));

        // Act
        var attributes = method!.GetCustomAttributes(typeof(NeedsMutationCoverageAttribute), false)
            .Cast<NeedsMutationCoverageAttribute>()
            .ToList();

        // Assert
        attributes.Count.ShouldBe(2);
        attributes.ShouldContain(a => a.Reason == "First issue");
        attributes.ShouldContain(a => a.Reason == "Second issue");
    }

    [Fact]
    [MutationKiller("EqualityMutation")]
    [MutationKiller("ArithmeticMutation")]
    public void Test_WithMultipleMutationKiller_ShouldHaveBothAttributes()
    {
        // Arrange
        var method = typeof(MutationAttributesTests).GetMethod(nameof(Test_WithMultipleMutationKiller_ShouldHaveBothAttributes));

        // Act
        var attributes = method!.GetCustomAttributes(typeof(MutationKillerAttribute), false)
            .Cast<MutationKillerAttribute>()
            .ToList();

        // Assert
        attributes.Count.ShouldBe(2);
        attributes.ShouldContain(a => a.MutationType == "EqualityMutation");
        attributes.ShouldContain(a => a.MutationType == "ArithmeticMutation");
    }

    #endregion
}
