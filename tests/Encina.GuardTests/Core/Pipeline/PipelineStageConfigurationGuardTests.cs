using Microsoft.Extensions.DependencyInjection;

namespace Encina.GuardTests.Core.Pipeline;

/// <summary>
/// Guard tests for the stage-aware overloads and the registration helpers added to <see cref="EncinaConfiguration"/>
/// for #2184 (Phase 4).
/// </summary>
public sealed class PipelineStageConfigurationGuardTests
{
    private static readonly (string Name, string Parameter, Action<EncinaConfiguration, Type?> Invoke)[] TypeOverloads =
    [
        ("AddPipelineBehavior", "pipelineBehaviorType", (c, t) => c.AddPipelineBehavior(t!, PipelineStage.Audit, 1)),
        ("AddRequestPreProcessor", "processorType", (c, t) => c.AddRequestPreProcessor(t!, PipelineStage.Audit, 1)),
        ("AddRequestPostProcessor", "processorType", (c, t) => c.AddRequestPostProcessor(t!, PipelineStage.Audit, 1)),
        ("AddRequestErrorProcessor", "processorType", (c, t) => c.AddRequestErrorProcessor(t!)),
        ("AddRequestErrorProcessor(stage)", "processorType", (c, t) => c.AddRequestErrorProcessor(t!, PipelineStage.Audit, 1)),
        ("AddRequestPostCommitProcessor", "processorType", (c, t) => c.AddRequestPostCommitProcessor(t!)),
        ("AddRequestPostCommitProcessor(stage)", "processorType", (c, t) => c.AddRequestPostCommitProcessor(t!, PipelineStage.Transaction, -1)),
        ("AddStreamPipelineBehavior", "streamBehaviorType", (c, t) => c.AddStreamPipelineBehavior(t!)),
        ("AddStreamPipelineBehavior(stage)", "streamBehaviorType", (c, t) => c.AddStreamPipelineBehavior(t!, PipelineStage.Audit, 1)),
    ];

    public static TheoryData<string> Names
    {
        get
        {
            var data = new TheoryData<string>();
            foreach (var overload in TypeOverloads)
            {
                data.Add(overload.Name);
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(Names))]
    public void TypeOverload_NullType_ThrowsArgumentNullExceptionNamingTheParameter(string name)
    {
        var overload = TypeOverloads.Single(o => o.Name == name);

        Should.Throw<ArgumentNullException>(() => overload.Invoke(new EncinaConfiguration(), null))
            .ParamName.ShouldBe(overload.Parameter);
    }

    [Theory]
    [MemberData(nameof(Names))]
    public void TypeOverload_AbstractType_ThrowsArgumentException(string name)
    {
        var overload = TypeOverloads.Single(o => o.Name == name);

        Should.Throw<ArgumentException>(() => overload.Invoke(new EncinaConfiguration(), typeof(AbstractType)))
            .ParamName.ShouldBe(overload.Parameter);
    }

    [Theory]
    [MemberData(nameof(Names))]
    public void TypeOverload_TypeWithoutTheInterface_ThrowsAndRecordsNoDeclaration(string name)
    {
        var overload = TypeOverloads.Single(o => o.Name == name);
        var configuration = new EncinaConfiguration();

        Should.Throw<ArgumentException>(() => overload.Invoke(configuration, typeof(Unrelated)))
            .ParamName.ShouldBe(overload.Parameter);
        configuration.StageDeclarations.ShouldBeEmpty();
    }

    [Fact]
    public void RegisterConfigured_NullServices_ThrowsArgumentNullException()
    {
        var configuration = new EncinaConfiguration();

        Should.Throw<ArgumentNullException>(() => configuration.RegisterConfiguredRequestErrorProcessors(null!));
        Should.Throw<ArgumentNullException>(() => configuration.RegisterConfiguredRequestPostCommitProcessors(null!));
        Should.Throw<ArgumentNullException>(() => configuration.RegisterConfiguredStreamPipelineBehaviors(null!));
    }

    [Fact]
    public void ApplyStageDeclarations_NullRegistry_ThrowsArgumentNullException()
        => Should.Throw<ArgumentNullException>(() => new EncinaConfiguration().ApplyStageDeclarations(null!));

    [Fact]
    public void RegisterConfigured_NothingConfigured_LeavesTheCollectionUntouched()
    {
        var services = new ServiceCollection();
        var configuration = new EncinaConfiguration();

        configuration.RegisterConfiguredRequestErrorProcessors(services);
        configuration.RegisterConfiguredRequestPostCommitProcessors(services);
        configuration.RegisterConfiguredStreamPipelineBehaviors(services);

        services.ShouldBeEmpty();
    }

    [Fact]
    public void Inspector_NullScopeFactory_ThrowsArgumentNullException()
        => Should.Throw<ArgumentNullException>(() => new EncinaPipelineInspector(
                null!,
                new PipelineOrderResolver(new PipelineStageRegistry(), Microsoft.Extensions.Logging.Abstractions.NullLogger<PipelineOrderResolver>.Instance)))
            .ParamName.ShouldBe("scopeFactory");

    [Fact]
    public void Inspector_NullResolver_ThrowsArgumentNullException()
        => Should.Throw<ArgumentNullException>(() => new EncinaPipelineInspector(
                new ServiceCollection().BuildServiceProvider().GetRequiredService<IServiceScopeFactory>(),
                null!))
            .ParamName.ShouldBe("resolver");

    [Fact]
    public void AddEncina_StageRegistryRegisteredWithoutAnInstance_ThrowsInsteadOfLosingDeclarations()
    {
        var services = new ServiceCollection();
        services.AddSingleton<PipelineStageRegistry>();

        Should.Throw<InvalidOperationException>(() => services.AddEncina())
            .Message.ShouldContain("PipelineStageRegistry");
    }

    private abstract class AbstractType;

    private sealed class Unrelated;
}
