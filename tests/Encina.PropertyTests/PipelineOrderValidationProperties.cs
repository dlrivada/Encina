using FsCheck;
using FsCheck.Xunit;

namespace Encina.PropertyTests;

/// <summary>
/// Invariants of the fail-closed pipeline order rules over random positions.
/// </summary>
public sealed class PipelineOrderValidationProperties
{
    private static readonly Type Key = typeof(IPipelineBehavior<Req, string>);
    private static readonly PipelineStage[] Stages = Enum.GetValues<PipelineStage>();

    private static bool Throws(IReadOnlyList<ResolvedStep> steps, PipelineOrderRuleSet rules)
    {
        try
        {
            PipelineOrderValidator.Validate(Key, steps.ToArray(), rules);
            return false;
        }
        catch (InvalidOperationException)
        {
            return true;
        }
    }

    [Property(MaxTest = 200)]
    public bool Validate_CustomStepsAtAnyDefinedPosition_NeverFailTheStagedRules(byte[] stageIndexes, int[] orders)
    {
        var steps = (stageIndexes ?? []).Zip(orders ?? [], (index, order) =>
                new ResolvedStep(typeof(Custom), PipelineStepKind.Behavior, Stages[index % Stages.Length], order, PipelineStepSource.Attribute))
            .ToList();

        return !Throws(steps, PipelineOrderRuleSet.Staged);
    }

    [Property(MaxTest = 200)]
    public bool Validate_AnyUndefinedStage_FailsR1(int rawStage, int order)
    {
        var stage = (PipelineStage)rawStage;
        if (Enum.IsDefined(stage))
        {
            return true;
        }

        var steps = new[] { new ResolvedStep(typeof(Custom), PipelineStepKind.Behavior, stage, order, PipelineStepSource.Registration) };

        return Throws(steps, PipelineOrderRuleSet.StageDefined);
    }

    [Property(MaxTest = 300)]
    public bool Validate_PostCommit_FailsExactlyWhenItCouldRunBeforeTheCommit(byte stageIndex, int postCommitOrder, int transactionOrder)
    {
        var stage = Stages[stageIndex % Stages.Length];
        var steps = new[]
        {
            new ResolvedStep(typeof(PostCommit), PipelineStepKind.PostCommitProcessor, stage, postCommitOrder, PipelineStepSource.Registration),
            new ResolvedStep(typeof(Custom), PipelineStepKind.Behavior, PipelineStage.Transaction, transactionOrder, PipelineStepSource.Attribute)
        };
        var runsBeforeTheCommit = stage > PipelineStage.Transaction
            || (stage == PipelineStage.Transaction && postCommitOrder >= transactionOrder);

        return Throws(steps, PipelineOrderRuleSet.PostCommitAfterTransaction) == runsBeforeTheCommit;
    }

    [Property(MaxTest = 200)]
    public bool Validate_TwoBuiltInsAtADeclaredPosition_FailExactlyWhenTheyShareIt(byte stageA, int orderA, byte stageB, int orderB)
    {
        var first = new ResolvedStep(typeof(EncinaMetrics), PipelineStepKind.Behavior, Stages[stageA % Stages.Length], orderA, PipelineStepSource.Attribute);
        var second = new ResolvedStep(typeof(PipelineOrderResolver), PipelineStepKind.Behavior, Stages[stageB % Stages.Length], orderB, PipelineStepSource.Registration);
        var shared = first.Stage == second.Stage && orderA == orderB;

        return Throws([first, second], PipelineOrderRuleSet.BuiltInPositionsUnique) == shared;
    }

    private sealed record Req : IRequest<string>;

    private sealed class Custom;

    private sealed class PostCommit;
}
