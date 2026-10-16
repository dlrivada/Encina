using System.Reflection;
using System.Reflection.Emit;

namespace Encina.UnitTests.Pipeline.Ordering;

/// <summary>
/// Rules R1-R7 of <see cref="PipelineOrderValidator"/>, each with a passing and a failing case.
/// </summary>
public sealed class PipelineOrderValidatorTests
{
    private static readonly Type Key = typeof(IPipelineBehavior<Req, string>);

    // Types of the core assembly are built-in steps; types of this test assembly are not
    private static readonly Type BuiltInA = typeof(EncinaMetrics);
    private static readonly Type BuiltInB = typeof(PipelineOrderResolver);

    private static ResolvedStep Step(
        Type type,
        PipelineStage stage,
        int order = 0,
        PipelineStepKind kind = PipelineStepKind.Behavior,
        PipelineStepSource source = PipelineStepSource.Attribute)
        => new(type, kind, stage, order, source);

    private static void Validate(PipelineOrderRuleSet rules, params ResolvedStep[] steps)
        => PipelineOrderValidator.Validate(Key, steps, rules);

    #region R1

    [Fact]
    public void R1_DefinedStages_Pass()
    {
        Should.NotThrow(() => Validate(PipelineOrderRuleSet.StageDefined, Step(typeof(Custom), PipelineStage.Observability)));
    }

    [Fact]
    public void R1_CastIntegerThatIsNotAMember_FailsNamingTheRuleThePipelineAndTheStep()
    {
        var exception = Should.Throw<InvalidOperationException>(
            () => Validate(PipelineOrderRuleSet.StageDefined, Step(typeof(Custom), (PipelineStage)7)));

        exception.Message.ShouldContain("R1");
        exception.Message.ShouldContain("Custom");
        exception.Message.ShouldContain("IPipelineBehavior<Req, String>");
        exception.Message.ShouldContain("7");
    }

    #endregion

    #region R3

    [Fact]
    public void R3_BuiltInWithExplicitStage_Passes()
    {
        Should.NotThrow(() => Validate(PipelineOrderRuleSet.BuiltInStageDeclared, Step(BuiltInA, PipelineStage.Audit)));
    }

    [Fact]
    public void R3_CustomStepOnTheDefault_Passes()
    {
        Should.NotThrow(() => Validate(
            PipelineOrderRuleSet.BuiltInStageDeclared,
            Step(typeof(Custom), PipelineStage.Application, source: PipelineStepSource.Default)));
    }

    [Fact]
    public void R3_BuiltInOnTheDefault_FailsNamingTheRuleAndTheType()
    {
        var exception = Should.Throw<InvalidOperationException>(() => Validate(
            PipelineOrderRuleSet.BuiltInStageDeclared,
            Step(BuiltInA, PipelineStage.Application, source: PipelineStepSource.Default),
            Step(BuiltInB, PipelineStage.Application, source: PipelineStepSource.Default)));

        exception.Message.ShouldContain("R3");
        exception.Message.ShouldContain("EncinaMetrics");
        exception.Message.ShouldContain("PipelineOrderResolver");
    }

    [Fact]
    public void R3_BuiltInInTheListedPackages_IsRecognizedByAssemblyNameNotByPrefix()
    {
        PipelineOrderValidator.IsBuiltIn(BuiltInA).ShouldBeTrue();
        PipelineOrderValidator.IsBuiltIn(typeof(PipelineOrderValidatorTests)).ShouldBeFalse(
            "a test project named Encina.* is not a built-in package");
        PipelineOrderValidator.BuiltInAssemblies.ShouldContain("Encina.Security");
        PipelineOrderValidator.BuiltInAssemblies.ShouldNotContain("Encina.UnitTests");
    }

    [Fact]
    public void R3_AssemblyMarkedWithTheAttribute_IsBuiltIn()
    {
        var marker = new CustomAttributeBuilder(typeof(EncinaBuiltInPipelineStepsAttribute).GetConstructor(Type.EmptyTypes)!, []);
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Dynamic.Marked.Steps"), AssemblyBuilderAccess.Run, [marker]);
        var type = assembly.DefineDynamicModule("module").DefineType("MarkedStep", TypeAttributes.Public).CreateType();

        PipelineOrderValidator.IsBuiltIn(type).ShouldBeTrue();
        Should.Throw<InvalidOperationException>(() => Validate(
            PipelineOrderRuleSet.BuiltInStageDeclared,
            Step(type, PipelineStage.Application, source: PipelineStepSource.Default))).Message.ShouldContain("MarkedStep");
    }

    #endregion

    #region R4

    [Fact]
    public void R4_OnlyPipelineBehaviorRegistrations_Passes()
    {
        Should.NotThrow(() => PipelineOrderValidator.ValidateRegistrations([], [], PipelineOrderRuleSet.All));
    }

    [Fact]
    public void R4_CommandAndQueryBehaviorRegistrations_FailNamingTheRuleAndTheTypes()
    {
        var dead = new[]
        {
            ServiceDescriptor.Transient(typeof(ICommandPipelineBehavior<,>), typeof(DeadBehavior<,>)),
            ServiceDescriptor.Transient(typeof(IQueryPipelineBehavior<Query, string>), typeof(DeadQuery)),
            ServiceDescriptor.KeyedTransient(typeof(IQueryPipelineBehavior<Query, string>), "key", typeof(DeadQuery)),
            ServiceDescriptor.Singleton(typeof(ICommandPipelineBehavior<Command, string>), _ => new DeadCommand())
        };

        var exception = Should.Throw<InvalidOperationException>(
            () => PipelineOrderValidator.ValidateRegistrations([], dead, PipelineOrderRuleSet.NoDeadRegistrations));

        exception.Message.ShouldContain("R4");
        exception.Message.ShouldContain("DeadBehavior");
        exception.Message.ShouldContain("DeadQuery");
        exception.Message.ShouldContain("ICommandPipelineBehavior<Command, String>");
        exception.Message.ShouldContain("a factory or instance");
    }

    [Fact]
    public void R4_WhenNotEnforced_DoesNotThrow()
    {
        var dead = new[] { ServiceDescriptor.Transient(typeof(ICommandPipelineBehavior<,>), typeof(DeadBehavior<,>)) };

        Should.NotThrow(() => PipelineOrderValidator.ValidateRegistrations([], dead, PipelineOrderRuleSet.Staged));
    }

    #endregion

    #region R5

    [Fact]
    public void R5_BuiltInsAtDifferentPositions_Pass()
    {
        Should.NotThrow(() => Validate(
            PipelineOrderRuleSet.BuiltInPositionsUnique,
            Step(BuiltInA, PipelineStage.Transaction, 0),
            Step(BuiltInB, PipelineStage.Transaction, 10)));
    }

    [Fact]
    public void R5_CustomStepsSharingAPosition_Pass()
    {
        Should.NotThrow(() => Validate(
            PipelineOrderRuleSet.BuiltInPositionsUnique,
            Step(typeof(Custom), PipelineStage.Transaction),
            Step(typeof(OtherCustom), PipelineStage.Transaction)));
    }

    [Fact]
    public void R5_BuiltInsOnTheDefaultPosition_AreLeftToR3()
    {
        Should.NotThrow(() => Validate(
            PipelineOrderRuleSet.BuiltInPositionsUnique,
            Step(BuiltInA, PipelineStage.Application, source: PipelineStepSource.Default),
            Step(BuiltInB, PipelineStage.Application, source: PipelineStepSource.Default)));
    }

    [Fact]
    public void R5_TwoBuiltInsSharingStageAndOrder_FailNamingBoth()
    {
        var exception = Should.Throw<InvalidOperationException>(() => Validate(
            PipelineOrderRuleSet.BuiltInPositionsUnique,
            Step(BuiltInA, PipelineStage.Transaction, 5),
            Step(BuiltInB, PipelineStage.Transaction, 5, source: PipelineStepSource.Registration)));

        exception.Message.ShouldContain("R5");
        exception.Message.ShouldContain("EncinaMetrics");
        exception.Message.ShouldContain("PipelineOrderResolver");
        exception.Message.ShouldContain("Transaction");
    }

    [Fact]
    public void R5_SameBuiltInTypeTwice_ForTwoModules_Passes()
    {
        Should.NotThrow(() => Validate(
            PipelineOrderRuleSet.BuiltInPositionsUnique,
            Step(BuiltInA, PipelineStage.Transaction, 5, PipelineStepKind.ModuleBehavior),
            Step(BuiltInA, PipelineStage.Transaction, 5, PipelineStepKind.ModuleBehavior)));
    }

    #endregion

    #region R6

    [Fact]
    public void R6_NoConflicts_Passes()
    {
        Should.NotThrow(() => PipelineOrderValidator.ValidateRegistrations([], [], PipelineOrderRuleSet.OverridesConsistent));
    }

    [Fact]
    public void R6_TwoDifferentPositionsForOneType_FailNamingTheRuleAndTheType()
    {
        var conflict = new PipelineStageConflict(typeof(Custom), PipelineStage.Audit, 0, PipelineStage.Caching, 3);

        var exception = Should.Throw<InvalidOperationException>(
            () => PipelineOrderValidator.ValidateRegistrations([conflict], [], PipelineOrderRuleSet.OverridesConsistent));

        exception.Message.ShouldContain("R6");
        exception.Message.ShouldContain("Custom");
        exception.Message.ShouldContain("Audit/0 then Caching/3");
    }

    [Fact]
    public void R6_RegistryRecordsAConflictOnlyWhenThePositionDiffers()
    {
        var registry = new PipelineStageRegistry();

        registry.Set(typeof(Custom), PipelineStage.Audit, 0);
        registry.Set(typeof(Custom), PipelineStage.Audit, 0);
        registry.Conflicts.ShouldBeEmpty();

        registry.Set(typeof(Custom), PipelineStage.Caching, 3);
        registry.Conflicts.ShouldBe([new PipelineStageConflict(typeof(Custom), PipelineStage.Audit, 0, PipelineStage.Caching, 3)]);
        registry.TryGet(typeof(Custom), out var position).ShouldBeTrue();
        position.ShouldBe((PipelineStage.Caching, 3));
    }

    #endregion

    #region R7

    [Fact]
    public void R7_PostCommitInTransactionWithLowerOrderThanEveryTransactionBehavior_Passes()
    {
        Should.NotThrow(() => Validate(
            PipelineOrderRuleSet.PostCommitAfterTransaction,
            Step(typeof(PostCommit), PipelineStage.Transaction, -100, PipelineStepKind.PostCommitProcessor),
            Step(typeof(Custom), PipelineStage.Transaction, 0),
            Step(typeof(OtherCustom), PipelineStage.Transaction, 20)));
    }

    [Fact]
    public void R7_PostCommitOutsideTransaction_OrWithoutATransactionBehavior_Passes()
    {
        Should.NotThrow(() => Validate(
            PipelineOrderRuleSet.PostCommitAfterTransaction,
            Step(typeof(PostCommit), PipelineStage.Authorization, 0, PipelineStepKind.PostCommitProcessor),
            Step(typeof(Custom), PipelineStage.Transaction, 0)));
        Should.NotThrow(() => Validate(
            PipelineOrderRuleSet.PostCommitAfterTransaction,
            Step(typeof(PostCommit), PipelineStage.Transaction, 0, PipelineStepKind.PostCommitProcessor)));
    }

    [Fact]
    public void R7_PostCommitJustBelowTheTransactionBehaviorOrder_Passes()
    {
        Should.NotThrow(() => Validate(
            PipelineOrderRuleSet.PostCommitAfterTransaction,
            Step(typeof(Custom), PipelineStage.Transaction, 0),
            Step(typeof(PostCommit), PipelineStage.Transaction, -1, PipelineStepKind.PostCommitProcessor)));
    }

    [Fact]
    public void R7_PostCommitInAStageInsideTransaction_FailsNamingTheRuleAndTheProcessor()
    {
        var exception = Should.Throw<InvalidOperationException>(() => Validate(
            PipelineOrderRuleSet.PostCommitAfterTransaction,
            Step(typeof(PostCommit), PipelineStage.Application, 0, PipelineStepKind.PostCommitProcessor)));

        exception.Message.ShouldContain("R7");
        exception.Message.ShouldContain("PostCommit");
        exception.Message.ShouldContain("Application");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    public void R7_PostCommitInTransactionNotLowerThanATransactionBehavior_FailsNamingBoth(int order)
    {
        var exception = Should.Throw<InvalidOperationException>(() => Validate(
            PipelineOrderRuleSet.PostCommitAfterTransaction,
            Step(typeof(Custom), PipelineStage.Transaction, 0),
            Step(typeof(PostCommit), PipelineStage.Transaction, order, PipelineStepKind.PostCommitProcessor)));

        exception.Message.ShouldContain("R7");
        exception.Message.ShouldContain("PostCommit");
        exception.Message.ShouldContain("Custom");
    }

    #endregion

    [Fact]
    public void RuleSet_NoneEnforced_AcceptsAnything()
    {
        Should.NotThrow(() => Validate(
            PipelineOrderRuleSet.None,
            Step(typeof(PostCommit), (PipelineStage)7, 0, PipelineStepKind.PostCommitProcessor)));
    }

    [Fact]
    public void NameOf_FormatsGenericArgumentsWithoutNamespaces()
    {
        PipelineOrderValidator.NameOf(typeof(string)).ShouldBe("String");
        PipelineOrderValidator.NameOf(typeof(IPipelineBehavior<Req, string>)).ShouldBe("IPipelineBehavior<Req, String>");
        PipelineOrderValidator.NameOf(typeof(ICommandPipelineBehavior<,>)).ShouldBe("ICommandPipelineBehavior<, >");
        PipelineOrderValidator.NameOf(typeof(Dictionary<string, List<int>>)).ShouldBe("Dictionary<String, List<Int32>>");
    }

    private sealed record Req : IRequest<string>;

    private sealed record Command : ICommand<string>;

    private sealed record Query : IQuery<string>;

    private sealed class Custom;

    private sealed class OtherCustom;

    private sealed class PostCommit;

    private sealed class DeadBehavior<TRequest, TResponse>;

    private sealed class DeadQuery;

    private sealed class DeadCommand;
}
