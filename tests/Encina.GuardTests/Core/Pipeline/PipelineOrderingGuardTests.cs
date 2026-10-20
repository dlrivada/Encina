using Encina.Modules;
using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.GuardTests.Core.Pipeline;

/// <summary>
/// Guard tests for the stage registry, the order resolver, the stream builder and the processor steps.
/// </summary>
public class PipelineOrderingGuardTests
{
    private static PipelineOrderResolver Resolver { get; } = new(new PipelineStageRegistry(), NullLogger<PipelineOrderResolver>.Instance);

    #region PipelineStageRegistry

    [Fact]
    public void Registry_Set_NullType_ThrowsArgumentNullException()
    {
        var registry = new PipelineStageRegistry();

        Should.Throw<ArgumentNullException>(() => registry.Set(null!, PipelineStage.Application, 0))
            .ParamName.ShouldBe("stepType");
    }

    [Fact]
    public void Registry_TryGet_NullType_ThrowsArgumentNullException()
    {
        var registry = new PipelineStageRegistry();

        Should.Throw<ArgumentNullException>(() => registry.TryGet(null!, out _))
            .ParamName.ShouldBe("stepType");
    }

    [Fact]
    public void Registry_TryGet_UnknownType_ReturnsFalse()
    {
        new PipelineStageRegistry().TryGet(typeof(string), out _).ShouldBeFalse();
    }

    [Fact]
    public void Registry_Set_ThenTryGet_ReturnsTheOverride()
    {
        var registry = new PipelineStageRegistry();
        registry.Set(typeof(List<>), PipelineStage.Caching, 4);

        registry.TryGet(typeof(List<int>), out var position).ShouldBeTrue();
        position.ShouldBe((PipelineStage.Caching, 4));
    }

    [Fact]
    public void Registry_Set_SameTypeAtTwoPositions_RecordsAConflictInsteadOfThrowing()
    {
        var registry = new PipelineStageRegistry();
        registry.Set(typeof(string), PipelineStage.Audit, 0);

        Should.NotThrow(() => registry.Set(typeof(string), PipelineStage.Caching, 1));

        registry.Conflicts.Count.ShouldBe(1);
    }

    [Fact]
    public void Registry_Set_AfterSeal_ThrowsInvalidOperationExceptionNamingTheType()
    {
        var registry = new PipelineStageRegistry();
        registry.Seal();

        Should.Throw<InvalidOperationException>(() => registry.Set(typeof(string), PipelineStage.Application, 0))
            .Message.ShouldContain("String");
    }

    #endregion

    #region PipelineOrderResolver

    [Fact]
    public void Resolver_Constructor_NullRegistry_Throws()
    {
        Should.Throw<ArgumentNullException>(() => new PipelineOrderResolver(null!, NullLogger<PipelineOrderResolver>.Instance))
            .ParamName.ShouldBe("registry");
    }

    [Fact]
    public void Resolver_Constructor_NullLogger_Throws()
    {
        Should.Throw<ArgumentNullException>(() => new PipelineOrderResolver(new PipelineStageRegistry(), null!))
            .ParamName.ShouldBe("logger");
    }

    [Fact]
    public void Resolver_Resolve_NullPipelineKey_Throws()
    {
        Should.Throw<ArgumentNullException>(() => Resolver.Resolve(null!, []))
            .ParamName.ShouldBe("pipelineKey");
    }

    [Fact]
    public void Resolver_Resolve_NullCandidates_Throws()
    {
        Should.Throw<ArgumentNullException>(() => Resolver.Resolve(typeof(string), null!))
            .ParamName.ShouldBe("steps");
    }

    [Fact]
    public void Resolver_Resolve_NoCandidates_ReturnsEmptyOrder()
    {
        Resolver.Resolve(typeof(IStreamPipelineBehavior<StreamCommand, int>), []).ShouldBeEmpty();
    }

    #endregion

    #region PipelineOrderResolver (startup validation)

    [Fact]
    public void Resolver_Warm_NullPipelineKey_Throws()
    {
        Should.Throw<ArgumentNullException>(() => Resolver.Warm(null!, []))
            .ParamName.ShouldBe("pipelineKey");
    }

    [Fact]
    public void Resolver_Warm_NullCandidates_Throws()
    {
        Should.Throw<ArgumentNullException>(() => Resolver.Warm(typeof(string), null!))
            .ParamName.ShouldBe("candidates");
    }

    #endregion

    #region PipelineOrderValidator and PipelineRegistrationSnapshot

    [Fact]
    public void Validator_Validate_NullPipelineKey_Throws()
    {
        Should.Throw<ArgumentNullException>(() => PipelineOrderValidator.Validate(null!, [], PipelineOrderRuleSet.All))
            .ParamName.ShouldBe("pipelineKey");
    }

    [Fact]
    public void Validator_ValidateRegistrations_NullConflicts_Throws()
    {
        Should.Throw<ArgumentNullException>(() => PipelineOrderValidator.ValidateRegistrations(null!, [], PipelineOrderRuleSet.All))
            .ParamName.ShouldBe("conflicts");
    }

    [Fact]
    public void Validator_ValidateRegistrations_NullDeadRegistrations_Throws()
    {
        Should.Throw<ArgumentNullException>(() => PipelineOrderValidator.ValidateRegistrations([], null!, PipelineOrderRuleSet.All))
            .ParamName.ShouldBe("deadRegistrations");
    }

    [Fact]
    public void Validator_IsBuiltIn_NullType_Throws()
    {
        Should.Throw<ArgumentNullException>(() => PipelineOrderValidator.IsBuiltIn(null!))
            .ParamName.ShouldBe("type");
    }

    [Fact]
    public void Validator_NameOf_NullType_Throws()
    {
        Should.Throw<ArgumentNullException>(() => PipelineOrderValidator.NameOf(null!))
            .ParamName.ShouldBe("type");
    }

    [Fact]
    public void Snapshot_Constructor_NullServices_Throws()
    {
        Should.Throw<ArgumentNullException>(() => new PipelineRegistrationSnapshot(null!))
            .ParamName.ShouldBe("services");
    }

    [Fact]
    public void Snapshot_GetRegistrations_NullServiceType_Throws()
    {
        Should.Throw<ArgumentNullException>(() => new PipelineRegistrationSnapshot(new ServiceCollection()).GetRegistrations(null!))
            .ParamName.ShouldBe("closedServiceType");
    }

    [Fact]
    public void HostedService_Constructor_NullArguments_Throw()
    {
        var snapshot = new PipelineRegistrationSnapshot(new ServiceCollection());
        using var provider = new ServiceCollection().BuildServiceProvider();

        Should.Throw<ArgumentNullException>(() => new PipelineOrderValidationHostedService(null!, snapshot, Resolver))
            .ParamName.ShouldBe("serviceProvider");
        Should.Throw<ArgumentNullException>(() => new PipelineOrderValidationHostedService(provider, null!, Resolver))
            .ParamName.ShouldBe("snapshot");
        Should.Throw<ArgumentNullException>(() => new PipelineOrderValidationHostedService(provider, snapshot, null!))
            .ParamName.ShouldBe("resolver");
    }

    #endregion

    #region ModuleBehaviorAdapter

    [Fact]
    public void ModuleAdapter_Constructor_NullInnerBehavior_Throws()
    {
        Should.Throw<ArgumentNullException>(() => new ModuleBehaviorAdapter<AdapterModule, StreamCommandRequest, string>(
            null!, new AdapterModule(), Substitute.For<IModuleHandlerRegistry>()))
            .ParamName.ShouldBe("innerBehavior");
    }

    [Fact]
    public void ModuleAdapter_Constructor_NullModule_Throws()
    {
        Should.Throw<ArgumentNullException>(() => new ModuleBehaviorAdapter<AdapterModule, StreamCommandRequest, string>(
            Substitute.For<IModulePipelineBehavior<AdapterModule, StreamCommandRequest, string>>(), null!, Substitute.For<IModuleHandlerRegistry>()))
            .ParamName.ShouldBe("module");
    }

    [Fact]
    public void ModuleAdapter_Constructor_NullRegistry_Throws()
    {
        Should.Throw<ArgumentNullException>(() => new ModuleBehaviorAdapter<AdapterModule, StreamCommandRequest, string>(
            Substitute.For<IModulePipelineBehavior<AdapterModule, StreamCommandRequest, string>>(), new AdapterModule(), null!))
            .ParamName.ShouldBe("handlerRegistry");
    }

    public sealed class StreamCommandRequest : IRequest<string>;

    public sealed class AdapterModule : IModule
    {
        public string Name => "Adapter";

        public void ConfigureServices(IServiceCollection services)
        {
        }
    }

    #endregion

    #region StreamPipelineBuilder

    [Fact]
    public void StreamBuilder_Constructor_NullRequest_Throws()
    {
        Should.Throw<ArgumentNullException>(() => new StreamPipelineBuilder<StreamCommand, int>(
            null!, Handler, RequestContext.Create(), Resolver, CancellationToken.None))
            .ParamName.ShouldBe("request");
    }

    [Fact]
    public void StreamBuilder_Constructor_NullHandler_Throws()
    {
        Should.Throw<ArgumentNullException>(() => new StreamPipelineBuilder<StreamCommand, int>(
            new StreamCommand(), null!, RequestContext.Create(), Resolver, CancellationToken.None))
            .ParamName.ShouldBe("handler");
    }

    [Fact]
    public void StreamBuilder_Constructor_NullContext_Throws()
    {
        Should.Throw<ArgumentNullException>(() => new StreamPipelineBuilder<StreamCommand, int>(
            new StreamCommand(), Handler, null!, Resolver, CancellationToken.None))
            .ParamName.ShouldBe("context");
    }

    [Fact]
    public void StreamBuilder_Constructor_NullResolver_Throws()
    {
        Should.Throw<ArgumentNullException>(() => new StreamPipelineBuilder<StreamCommand, int>(
            new StreamCommand(), Handler, RequestContext.Create(), null!, CancellationToken.None))
            .ParamName.ShouldBe("resolver");
    }

    [Fact]
    public void StreamBuilder_Build_NullServiceProvider_Throws()
    {
        var builder = Create();

        Should.Throw<ArgumentNullException>(() => builder.Build(null!)).ParamName.ShouldBe("serviceProvider");
    }

    [Fact]
    public void StreamBuilder_Build_NoBehaviors_ReturnsCallback()
    {
        var builder = Create();

        builder.Build(new ServiceCollection().BuildServiceProvider()).ShouldNotBeNull();
    }

    private static IStreamRequestHandler<StreamCommand, int> Handler { get; } = Substitute.For<IStreamRequestHandler<StreamCommand, int>>();

    private static StreamPipelineBuilder<StreamCommand, int> Create()
        => new(new StreamCommand(), Handler, RequestContext.Create(), Resolver, CancellationToken.None);

    public sealed class StreamCommand : IStreamRequest<int>;

    #endregion
}
