using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace Encina;

/// <summary>
/// The rules of the pipeline order validation that are enforced for a resolver.
/// </summary>
[Flags]
internal enum PipelineOrderRuleSet
{
    /// <summary>No rule.</summary>
    None = 0,

    /// <summary>R1: every stage is a defined <see cref="PipelineStage"/> member.</summary>
    StageDefined = 1 << 0,

    /// <summary>R3: a built-in step declares its stage.</summary>
    BuiltInStageDeclared = 1 << 1,

    /// <summary>R4: nothing is registered under a pipeline behavior service type the builder never resolves.</summary>
    NoDeadRegistrations = 1 << 2,

    /// <summary>R5: two built-in steps of one pipeline do not share a declared position.</summary>
    BuiltInPositionsUnique = 1 << 3,

    /// <summary>R6: no type has two different registration-time positions.</summary>
    OverridesConsistent = 1 << 4,

    /// <summary>R7: a post-commit processor runs after every transaction behavior returned.</summary>
    PostCommitAfterTransaction = 1 << 5,

    /// <summary>The rules that hold for every step the pipeline can contain today.</summary>
    Staged = StageDefined | BuiltInPositionsUnique | OverridesConsistent | PostCommitAfterTransaction,

    /// <summary>Every rule, R3 and R4 included.</summary>
    All = Staged | BuiltInStageDeclared | NoDeadRegistrations
}

/// <summary>
/// One step of a closed pipeline with its resolved position, in execution order.
/// </summary>
/// <param name="StepType">The type the step stands for.</param>
/// <param name="Kind">Kind of the step.</param>
/// <param name="Stage">Resolved stage.</param>
/// <param name="Order">Resolved order inside the stage.</param>
/// <param name="Source">Where the position came from.</param>
internal readonly record struct ResolvedStep(
    Type StepType,
    PipelineStepKind Kind,
    PipelineStage Stage,
    int Order,
    PipelineStepSource Source);

/// <summary>
/// Two registration-time declarations that placed one type at different positions.
/// </summary>
/// <param name="StepType">The type, as keyed by the registry (the open definition for a generic type).</param>
/// <param name="FirstStage">Stage of the first declaration.</param>
/// <param name="FirstOrder">Order of the first declaration.</param>
/// <param name="SecondStage">Stage of the later declaration.</param>
/// <param name="SecondOrder">Order of the later declaration.</param>
internal readonly record struct PipelineStageConflict(
    Type StepType,
    PipelineStage FirstStage,
    int FirstOrder,
    PipelineStage SecondStage,
    int SecondOrder);

/// <summary>
/// Fail-closed rules R1-R7 of the pipeline order: an order that breaks one never runs.
/// </summary>
/// <remarks>
/// <para>
/// Rule R2 (a duplicated step identity is collapsed) is informational and never throws, so it has no check here.
/// Every other rule throws <see cref="InvalidOperationException"/> whose message starts with the rule id and names the
/// pipeline and the offending types, never an instance or an error message.
/// </para>
/// <para>
/// R5 compares only built-in steps whose position was declared (attribute or registry): a built-in on the default
/// position is the business of R3, which says it more precisely.
/// </para>
/// </remarks>
internal static class PipelineOrderValidator
{
    /// <summary>
    /// The packages built from <c>src/</c> of this repository. A step declared in one of them is a built-in step.
    /// The list is the directory names of <c>src/</c>, which are the assembly names.
    /// </summary>
    internal static readonly IReadOnlySet<string> BuiltInAssemblies = new HashSet<string>(StringComparer.Ordinal)
    {
        "Encina", "Encina.ADO.MySQL", "Encina.ADO.PostgreSQL", "Encina.ADO.SqlServer", "Encina.AmazonSQS",
        "Encina.Aspire.Testing", "Encina.AspNetCore", "Encina.AspNetCore.Blazor", "Encina.Audit.Marten", "Encina.AwsLambda",
        "Encina.AzureFunctions", "Encina.AzureServiceBus", "Encina.Caching", "Encina.Caching.Dragonfly", "Encina.Caching.Garnet",
        "Encina.Caching.Hybrid", "Encina.Caching.KeyDB", "Encina.Caching.Memory", "Encina.Caching.Redis", "Encina.Caching.Valkey",
        "Encina.Cdc", "Encina.Cdc.Debezium", "Encina.Cdc.MongoDb", "Encina.Cdc.MySql", "Encina.Cdc.PostgreSql",
        "Encina.Cdc.SqlServer", "Encina.Cli", "Encina.Compliance.AIAct", "Encina.Compliance.Anonymization",
        "Encina.Compliance.Attestation", "Encina.Compliance.BreachNotification", "Encina.Compliance.Consent",
        "Encina.Compliance.CrossBorderTransfer", "Encina.Compliance.DataResidency", "Encina.Compliance.DataSubjectRights",
        "Encina.Compliance.DPIA", "Encina.Compliance.GDPR", "Encina.Compliance.LawfulBasis", "Encina.Compliance.NIS2",
        "Encina.Compliance.PrivacyByDesign", "Encina.Compliance.ProcessorAgreements", "Encina.Compliance.Retention",
        "Encina.Dapper.MySQL", "Encina.Dapper.PostgreSQL", "Encina.Dapper.SqlServer", "Encina.DataAnnotations",
        "Encina.DistributedLock", "Encina.DistributedLock.InMemory", "Encina.DistributedLock.Redis",
        "Encina.DistributedLock.SqlServer", "Encina.DomainModeling", "Encina.EntityFrameworkCore",
        "Encina.Extensions.Resilience", "Encina.FluentValidation", "Encina.GraphQL", "Encina.gRPC", "Encina.GuardClauses",
        "Encina.Hangfire", "Encina.IdGeneration", "Encina.InMemory", "Encina.Kafka", "Encina.Marten", "Encina.Marten.GDPR",
        "Encina.Messaging", "Encina.Messaging.Encryption", "Encina.Messaging.Encryption.AwsKms",
        "Encina.Messaging.Encryption.AzureKeyVault", "Encina.Messaging.Encryption.DataProtection", "Encina.MiniValidator",
        "Encina.MongoDB", "Encina.MQTT", "Encina.NATS", "Encina.OpenTelemetry", "Encina.Polly", "Encina.Quartz",
        "Encina.RabbitMQ", "Encina.Redis.PubSub", "Encina.Refit", "Encina.Security", "Encina.Security.ABAC",
        "Encina.Security.ABAC.Analyzers", "Encina.Security.AntiTampering", "Encina.Security.Audit", "Encina.Security.BreakTheGlass", "Encina.Security.Encryption",
        "Encina.Security.PII", "Encina.Security.Sanitization", "Encina.Security.Secrets",
        "Encina.Security.Secrets.AwsSecretsManager", "Encina.Security.Secrets.AzureKeyVault",
        "Encina.Security.Secrets.GoogleCloudSecretManager", "Encina.Security.Secrets.HashiCorpVault", "Encina.SignalR",
        "Encina.Tenancy", "Encina.Tenancy.AspNetCore", "Encina.Testing", "Encina.Testing.Architecture", "Encina.Testing.Bogus",
        "Encina.Testing.Fakes", "Encina.Testing.FsCheck", "Encina.Testing.Pact", "Encina.Testing.Respawn",
        "Encina.Testing.Shouldly", "Encina.Testing.Testcontainers", "Encina.Testing.TUnit", "Encina.Testing.Verify",
        "Encina.Testing.WireMock"
    };

    /// <summary>
    /// Checks the rules that depend on the steps of one closed pipeline (R1, R3, R5, R7).
    /// </summary>
    /// <param name="pipelineKey">The closed pipeline type the steps belong to.</param>
    /// <param name="steps">The steps in execution order, outermost first, collapsed duplicates already omitted.</param>
    /// <param name="rules">The rules to enforce.</param>
    /// <exception cref="InvalidOperationException">A rule is broken; the message names the rule, the pipeline and the step types.</exception>
    public static void Validate(Type pipelineKey, ReadOnlySpan<ResolvedStep> steps, PipelineOrderRuleSet rules)
    {
        ArgumentNullException.ThrowIfNull(pipelineKey);

        if (rules.HasFlag(PipelineOrderRuleSet.StageDefined))
        {
            CheckStagesDefined(pipelineKey, steps);
        }

        if (rules.HasFlag(PipelineOrderRuleSet.BuiltInStageDeclared))
        {
            CheckBuiltInsDeclareStage(pipelineKey, steps);
        }

        if (rules.HasFlag(PipelineOrderRuleSet.BuiltInPositionsUnique))
        {
            CheckBuiltInPositionsUnique(pipelineKey, steps);
        }

        if (rules.HasFlag(PipelineOrderRuleSet.PostCommitAfterTransaction))
        {
            CheckPostCommitAfterTransaction(pipelineKey, steps);
        }
    }

    /// <summary>
    /// Checks the rules that depend on the registrations of the whole host (R4, R6).
    /// </summary>
    /// <param name="conflicts">Registration-time declarations that placed one type at two positions.</param>
    /// <param name="deadRegistrations">Descriptors registered under a pipeline behavior service type the builder never resolves.</param>
    /// <param name="rules">The rules to enforce.</param>
    /// <exception cref="InvalidOperationException">A rule is broken; the message names the rule and the types.</exception>
    public static void ValidateRegistrations(
        IReadOnlyList<PipelineStageConflict> conflicts,
        IReadOnlyList<ServiceDescriptor> deadRegistrations,
        PipelineOrderRuleSet rules)
    {
        ArgumentNullException.ThrowIfNull(conflicts);
        ArgumentNullException.ThrowIfNull(deadRegistrations);

        if (rules.HasFlag(PipelineOrderRuleSet.OverridesConsistent) && conflicts.Count > 0)
        {
            throw Fail("R6", $"{conflicts.Count} type(s) have two different registration-time positions: "
                + string.Join(", ", conflicts.Select(Describe))
                + ". Declare the position of a type once.");
        }

        if (rules.HasFlag(PipelineOrderRuleSet.NoDeadRegistrations) && deadRegistrations.Count > 0)
        {
            throw Fail("R4", $"{deadRegistrations.Count} registration(s) use a service type the pipeline builder never resolves: "
                + string.Join(", ", deadRegistrations.Select(Describe))
                + ". Register behaviors as IPipelineBehavior<,> or IStreamPipelineBehavior<,>.");
        }
    }

    /// <summary>
    /// Returns whether a type is declared in a package shipped from this repository.
    /// </summary>
    /// <param name="type">The step type.</param>
    /// <returns><see langword="true"/> when its assembly is listed in <see cref="BuiltInAssemblies"/> or carries <see cref="EncinaBuiltInPipelineStepsAttribute"/>.</returns>
    public static bool IsBuiltIn(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        var assembly = type.Assembly;
        return BuiltInAssemblies.Contains(assembly.GetName().Name ?? string.Empty)
            || assembly.IsDefined(typeof(EncinaBuiltInPipelineStepsAttribute), inherit: false);
    }

    /// <summary>
    /// Formats a type for a message: its simple name with generic arguments, never its namespace.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns>For example <c>IPipelineBehavior&lt;Ping, string&gt;</c>.</returns>
    public static string NameOf(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        if (!type.IsGenericType)
        {
            return type.Name;
        }

        var name = type.Name;
        var tick = name.IndexOf('`', StringComparison.Ordinal);
        var builder = new StringBuilder(tick < 0 ? name : name[..tick]).Append('<');
        var arguments = type.GetGenericArguments();
        for (var i = 0; i < arguments.Length; i++)
        {
            builder.Append(i == 0 ? string.Empty : ", ").Append(type.IsGenericTypeDefinition ? string.Empty : NameOf(arguments[i]));
        }

        return builder.Append('>').ToString();
    }

    private static void CheckStagesDefined(Type pipelineKey, ReadOnlySpan<ResolvedStep> steps)
    {
        foreach (var step in steps)
        {
            if (!Enum.IsDefined(step.Stage))
            {
                throw Fail("R1", $"pipeline {NameOf(pipelineKey)}: step {NameOf(step.StepType)} has stage value {(int)step.Stage}, "
                    + "which is not a PipelineStage member.");
            }
        }
    }

    private static void CheckBuiltInsDeclareStage(Type pipelineKey, ReadOnlySpan<ResolvedStep> steps)
    {
        var offenders = new List<string>();
        foreach (var step in steps)
        {
            if (step.Source == PipelineStepSource.Default && IsBuiltIn(step.StepType))
            {
                offenders.Add(NameOf(step.StepType));
            }
        }

        if (offenders.Count > 0)
        {
            throw Fail("R3", $"pipeline {NameOf(pipelineKey)}: built-in step(s) {string.Join(", ", offenders)} declare no stage; "
                + "add [PipelineStage] to the type or a registration-time stage.");
        }
    }

    private static void CheckBuiltInPositionsUnique(Type pipelineKey, ReadOnlySpan<ResolvedStep> steps)
    {
        for (var i = 0; i < steps.Length; i++)
        {
            for (var j = i + 1; j < steps.Length; j++)
            {
                if (SharePosition(steps[i], steps[j]))
                {
                    throw Fail("R5", $"pipeline {NameOf(pipelineKey)}: built-in steps {NameOf(steps[i].StepType)} and "
                        + $"{NameOf(steps[j].StepType)} share stage {steps[i].Stage} and order {steps[i].Order}; "
                        + "register only one of the provider variants.");
                }
            }
        }
    }

    private static bool SharePosition(in ResolvedStep left, in ResolvedStep right)
        => left.Stage == right.Stage
        && left.Order == right.Order
        && left.StepType != right.StepType
        && IsDeclaredBuiltIn(left)
        && IsDeclaredBuiltIn(right);

    private static bool IsDeclaredBuiltIn(in ResolvedStep step)
        => step.Source != PipelineStepSource.Default && IsBuiltIn(step.StepType);

    private static void CheckPostCommitAfterTransaction(Type pipelineKey, ReadOnlySpan<ResolvedStep> steps)
    {
        foreach (var step in steps)
        {
            if (step.Kind == PipelineStepKind.PostCommitProcessor)
            {
                CheckPostCommit(pipelineKey, step, steps);
            }
        }
    }

    private static void CheckPostCommit(Type pipelineKey, in ResolvedStep postCommit, ReadOnlySpan<ResolvedStep> steps)
    {
        if (postCommit.Stage > PipelineStage.Transaction)
        {
            throw Fail("R7", $"pipeline {NameOf(pipelineKey)}: post-commit processor {NameOf(postCommit.StepType)} is in stage "
                + $"{postCommit.Stage}, inside the Transaction stage, so it would run before the commit.");
        }

        if (postCommit.Stage < PipelineStage.Transaction)
        {
            return;
        }

        foreach (var step in steps)
        {
            if (IsTransactionBehavior(step) && step.Order <= postCommit.Order)
            {
                throw Fail("R7", $"pipeline {NameOf(pipelineKey)}: post-commit processor {NameOf(postCommit.StepType)} has order "
                    + $"{postCommit.Order}, not lower than the order {step.Order} of transaction behavior {NameOf(step.StepType)}, "
                    + "so it would run before the commit.");
            }
        }
    }

    private static bool IsTransactionBehavior(in ResolvedStep step)
        => step.Stage == PipelineStage.Transaction
        && step.Kind is PipelineStepKind.Behavior or PipelineStepKind.ModuleBehavior;

    private static string Describe(PipelineStageConflict conflict)
        => $"{NameOf(conflict.StepType)} ({conflict.FirstStage}/{conflict.FirstOrder} then {conflict.SecondStage}/{conflict.SecondOrder})";

    private static string Describe(ServiceDescriptor descriptor)
    {
        var implementation = descriptor.IsKeyedService ? descriptor.KeyedImplementationType : descriptor.ImplementationType;
        return $"{NameOf(descriptor.ServiceType)} implemented by {(implementation is null ? "a factory or instance" : NameOf(implementation))}";
    }

    private static InvalidOperationException Fail(string ruleId, string detail)
        => new($"Pipeline order rule {ruleId} failed: {detail}");
}
