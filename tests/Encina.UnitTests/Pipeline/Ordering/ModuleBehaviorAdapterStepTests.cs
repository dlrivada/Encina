using Encina.Modules;

namespace Encina.UnitTests.Pipeline.Ordering;

/// <summary>
/// Execution and step identity of <see cref="ModuleBehaviorAdapter{TModule, TRequest, TResponse}"/>.
/// </summary>
public sealed class ModuleBehaviorAdapterStepTests
{
    private readonly IModuleHandlerRegistry _registry = Substitute.For<IModuleHandlerRegistry>();

    [Fact]
    public async Task Handle_HandlerBelongsToTheModule_RunsTheWrappedBehavior()
    {
        _registry.BelongsToModule(typeof(IRequestHandler<Req, string>), "Orders").Returns(true);
        var inner = new InnerBehavior();

        var result = await CreateAdapter(inner).Handle(new Req(), RequestContext.Create(), Next, CancellationToken.None);

        inner.Calls.ShouldBe(1);
        result.IfRight(value => value.ShouldBe("inner"));
    }

    [Fact]
    public async Task Handle_HandlerBelongsToAnotherModule_SkipsTheWrappedBehavior()
    {
        _registry.BelongsToModule(typeof(IRequestHandler<Req, string>), "Orders").Returns(false);
        var inner = new InnerBehavior();

        var result = await CreateAdapter(inner).Handle(new Req(), RequestContext.Create(), Next, CancellationToken.None);

        inner.Calls.ShouldBe(0);
        result.IfRight(value => value.ShouldBe("next"));
    }

    [Fact]
    public void Identity_IsTheWrappedBehaviorType_WithItsDeclaredStage()
    {
        var adapter = CreateAdapter(new InnerBehavior());

        adapter.StepType.ShouldBe(typeof(InnerBehavior));
        adapter.Kind.ShouldBe(PipelineStepKind.ModuleBehavior);
        adapter.DeclaredStage.ShouldBe(PipelineStage.Compliance);
        adapter.DeclaredOrder.ShouldBe(3);
    }

    [Fact]
    public void Identity_WithoutTheAttribute_DeclaresNothing()
    {
        var adapter = CreateAdapter(new UndeclaredInnerBehavior());

        adapter.DeclaredStage.ShouldBeNull();
        adapter.DeclaredOrder.ShouldBeNull();
    }

    private static ValueTask<Either<EncinaError, string>> Next()
        => ValueTask.FromResult<Either<EncinaError, string>>("next");

    private ModuleBehaviorAdapter<OrdersModule, Req, string> CreateAdapter(IModulePipelineBehavior<OrdersModule, Req, string> inner)
        => new(inner, new OrdersModule(), _registry);

    private sealed record Req : IRequest<string>;

    private sealed class OrdersModule : IModule
    {
        public string Name => "Orders";

        public void ConfigureServices(IServiceCollection services)
        {
        }
    }

    [PipelineStage(PipelineStage.Compliance, Order = 3)]
    private sealed class InnerBehavior : IModulePipelineBehavior<OrdersModule, Req, string>
    {
        public int Calls { get; private set; }

        public ValueTask<Either<EncinaError, string>> Handle(
            Req request, IRequestContext context, RequestHandlerCallback<string> nextStep, CancellationToken cancellationToken)
        {
            Calls++;
            return ValueTask.FromResult<Either<EncinaError, string>>("inner");
        }
    }

    private sealed class UndeclaredInnerBehavior : IModulePipelineBehavior<OrdersModule, Req, string>
    {
        public ValueTask<Either<EncinaError, string>> Handle(
            Req request, IRequestContext context, RequestHandlerCallback<string> nextStep, CancellationToken cancellationToken)
            => nextStep();
    }
}
