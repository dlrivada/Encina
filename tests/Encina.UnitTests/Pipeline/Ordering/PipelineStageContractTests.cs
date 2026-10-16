using System.Reflection;
using Shouldly;

namespace Encina.UnitTests.Pipeline.Ordering;

/// <summary>
/// Tests for the pipeline stage vocabulary: <see cref="PipelineStage"/>, <see cref="PipelineStageAttribute"/>
/// and <see cref="PipelineStepDescription"/>.
/// </summary>
public sealed class PipelineStageContractTests
{
    [Theory]
    [InlineData(PipelineStage.Observability, 100)]
    [InlineData(PipelineStage.Admission, 200)]
    [InlineData(PipelineStage.Audit, 300)]
    [InlineData(PipelineStage.Authentication, 400)]
    [InlineData(PipelineStage.Authorization, 500)]
    [InlineData(PipelineStage.Compliance, 600)]
    [InlineData(PipelineStage.Presentation, 700)]
    [InlineData(PipelineStage.InputPreparation, 800)]
    [InlineData(PipelineStage.Validation, 900)]
    [InlineData(PipelineStage.Caching, 1000)]
    [InlineData(PipelineStage.Resilience, 1100)]
    [InlineData(PipelineStage.DataRouting, 1200)]
    [InlineData(PipelineStage.Transaction, 1300)]
    [InlineData(PipelineStage.Idempotency, 1400)]
    [InlineData(PipelineStage.Application, 1500)]
    [InlineData(PipelineStage.HandlerPreparation, 1600)]
    public void PipelineStage_Member_HasContractValue(PipelineStage stage, int expected)
    {
        ((int)stage).ShouldBe(expected);
    }

    [Fact]
    public void PipelineStage_HasExactlySixteenMembers()
    {
        Enum.GetValues<PipelineStage>().Length.ShouldBe(16);
    }

    [Fact]
    public void PipelineStageAttribute_Constructor_SetsStageAndDefaultsOrderToZero()
    {
        var attribute = new PipelineStageAttribute(PipelineStage.Validation);

        attribute.Stage.ShouldBe(PipelineStage.Validation);
        attribute.Order.ShouldBe(0);
    }

    [Theory]
    [InlineData(-100)]
    [InlineData(0)]
    [InlineData(25)]
    public void PipelineStageAttribute_Order_AcceptsNegativeAndPositiveValues(int order)
    {
        var attribute = new PipelineStageAttribute(PipelineStage.Transaction) { Order = order };

        attribute.Order.ShouldBe(order);
    }

    [Fact]
    public void PipelineStageAttribute_Usage_IsClassOnlyNotInheritedAndSingle()
    {
        var usage = typeof(PipelineStageAttribute).GetCustomAttribute<AttributeUsageAttribute>();

        usage.ShouldNotBeNull();
        usage.ValidOn.ShouldBe(AttributeTargets.Class);
        usage.Inherited.ShouldBeFalse();
        usage.AllowMultiple.ShouldBeFalse();
    }

    [Fact]
    public void PipelineStageAttribute_OnType_IsReadable()
    {
        var attribute = typeof(DeclaredBehavior).GetCustomAttribute<PipelineStageAttribute>();

        attribute.ShouldNotBeNull();
        attribute.Stage.ShouldBe(PipelineStage.Authorization);
        attribute.Order.ShouldBe(-5);
    }

    [Fact]
    public void PipelineStepDescription_Constructor_ExposesAllComponents()
    {
        var description = new PipelineStepDescription(
            typeof(DeclaredBehavior), PipelineStepKind.Behavior, PipelineStage.Authorization, -5, PipelineStepSource.Attribute);

        description.StepType.ShouldBe(typeof(DeclaredBehavior));
        description.Kind.ShouldBe(PipelineStepKind.Behavior);
        description.Stage.ShouldBe(PipelineStage.Authorization);
        description.Order.ShouldBe(-5);
        description.Source.ShouldBe(PipelineStepSource.Attribute);
    }

    [Fact]
    public void PipelineStepDescription_WithSameComponents_AreEqual()
    {
        var first = new PipelineStepDescription(typeof(string), PipelineStepKind.PreProcessor, PipelineStage.Application, -100, PipelineStepSource.Default);
        var second = new PipelineStepDescription(typeof(string), PipelineStepKind.PreProcessor, PipelineStage.Application, -100, PipelineStepSource.Default);

        first.ShouldBe(second);
        (first with { Order = 0 }).ShouldNotBe(second);
    }

    [Fact]
    public void PipelineStepKind_HasSixMembers()
    {
        Enum.GetNames<PipelineStepKind>().ShouldBe(
            ["Behavior", "ModuleBehavior", "PreProcessor", "PostProcessor", "ErrorProcessor", "PostCommitProcessor"]);
    }

    [Fact]
    public void PipelineStepSource_HasThreeMembers()
    {
        Enum.GetNames<PipelineStepSource>().ShouldBe(["Attribute", "Registration", "Default"]);
    }

    [PipelineStage(PipelineStage.Authorization, Order = -5)]
    private sealed class DeclaredBehavior;
}
