using System.Runtime.CompilerServices;
using Encina.Testing.Fakes;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using static LanguageExt.Prelude;

namespace Encina.ContractTests.Core;

/// <summary>
/// Contract every <see cref="IEncina"/> implementation honours for the explicit-context overloads
/// added by issue #1147: they behave like the ambient overloads, reject a <c>null</c> context
/// eagerly and never change the caller's ambient <see cref="IRequestContextAccessor"/> value.
/// </summary>
public abstract class EncinaExplicitContextContract
{
    protected sealed record ContractPing(string Text) : IRequest<string>;

    protected sealed record ContractNotification : INotification;

    protected sealed record ContractNumbers : IStreamRequest<int>;

    /// <summary>Creates the implementation under test, answering <see cref="ContractPing"/> with its text upper-cased and streaming 1, 2, 3.</summary>
    protected abstract IEncina CreateSut();

    private static IRequestContext JobContext()
        => RequestContext.CreateForTest(userId: "job-user", tenantId: "job-tenant", correlationId: "job-correlation");

    [Fact]
    public async Task Send_WithExplicitContext_ReturnsTheSameOutcomeAsTheAmbientOverload()
    {
        var sut = CreateSut();

        var ambient = await sut.Send(new ContractPing("pong"));
        var explicitResult = await sut.Send(new ContractPing("pong"), JobContext());

        ambient.ShouldBeSuccess().ShouldBe("PONG");
        explicitResult.ShouldBeSuccess().ShouldBe("PONG");
    }

    [Fact]
    public async Task Send_WithNullContext_ThrowsArgumentNullException()
    {
        var sut = CreateSut();

        var ex = await Should.ThrowAsync<ArgumentNullException>(async () => await sut.Send(new ContractPing("x"), null!));

        ex.ParamName.ShouldBe("context");
    }

    [Fact]
    public async Task Publish_WithExplicitContext_ReturnsTheSameOutcomeAsTheAmbientOverload()
    {
        var sut = CreateSut();

        var ambient = await sut.Publish(new ContractNotification());
        var explicitResult = await sut.Publish(new ContractNotification(), JobContext());

        ambient.IsRight.ShouldBeTrue();
        explicitResult.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task Publish_WithNullContext_ThrowsArgumentNullException()
    {
        var sut = CreateSut();

        var ex = await Should.ThrowAsync<ArgumentNullException>(async () => await sut.Publish(new ContractNotification(), null!));

        ex.ParamName.ShouldBe("context");
    }

    [Fact]
    public async Task Stream_WithExplicitContext_YieldsTheSameItemsAsTheAmbientOverload()
    {
        var sut = CreateSut();

        var ambient = await CollectAsync(sut.Stream(new ContractNumbers()));
        var explicitItems = await CollectAsync(sut.Stream(new ContractNumbers(), JobContext()));

        ambient.ShouldBe([1, 2, 3]);
        explicitItems.ShouldBe([1, 2, 3]);
    }

    [Fact]
    public void Stream_WithNullContext_ThrowsArgumentNullExceptionBeforeEnumeration()
    {
        var sut = CreateSut();

        Should.Throw<ArgumentNullException>(() => sut.Stream(new ContractNumbers(), null!))
            .ParamName.ShouldBe("context");
    }

    [Fact]
    public async Task ExplicitOverloads_LeaveTheCallersAmbientContextUntouched()
    {
        var sut = CreateSut();
        var accessor = new RequestContextAccessor();
        var callerContext = RequestContext.CreateForTest(userId: "caller");
        accessor.RequestContext = callerContext;

        await sut.Send(new ContractPing("a"), JobContext());
        accessor.RequestContext.ShouldBeSameAs(callerContext);

        await sut.Publish(new ContractNotification(), JobContext());
        accessor.RequestContext.ShouldBeSameAs(callerContext);

        await CollectAsync(sut.Stream(new ContractNumbers(), JobContext()));
        accessor.RequestContext.ShouldBeSameAs(callerContext);
    }

    private static async Task<List<int>> CollectAsync(IAsyncEnumerable<Either<EncinaError, int>> stream)
    {
        var items = new List<int>();
        await foreach (var item in stream)
        {
            items.Add(item.ShouldBeSuccess());
        }

        return items;
    }
}

/// <summary>The default mediator, <see cref="Encina"/>.</summary>
public sealed class EncinaMediatorExplicitContextContractTests : EncinaExplicitContextContract, IDisposable
{
    private sealed class PingHandler : IRequestHandler<ContractPing, string>
    {
        public Task<Either<EncinaError, string>> Handle(ContractPing request, CancellationToken cancellationToken)
            => Task.FromResult(Right<EncinaError, string>(request.Text.ToUpperInvariant()));
    }

    private sealed class NotificationHandler : INotificationHandler<ContractNotification>
    {
        public Task<Either<EncinaError, Unit>> Handle(ContractNotification notification, CancellationToken cancellationToken)
            => Task.FromResult(Right<EncinaError, Unit>(Unit.Default));
    }

    private sealed class NumbersHandler : IStreamRequestHandler<ContractNumbers, int>
    {
        public async IAsyncEnumerable<Either<EncinaError, int>> Handle(
            ContractNumbers request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            for (var i = 1; i <= 3; i++)
            {
                await Task.Yield();
                yield return i;
            }
        }
    }

    private sealed class ContextRecorder
    {
        public IRequestContext? Seen { get; set; }
    }

    private sealed class RecordingBehavior(ContextRecorder recorder) : IPipelineBehavior<ContractPing, string>
    {
        public ValueTask<Either<EncinaError, string>> Handle(
            ContractPing request,
            IRequestContext context,
            RequestHandlerCallback<string> nextStep,
            CancellationToken cancellationToken)
        {
            recorder.Seen = context;
            return nextStep();
        }
    }

    private ServiceProvider? _provider;

    public void Dispose() => _provider?.Dispose();

    protected override IEncina CreateSut()
    {
        var services = new ServiceCollection();
        services.AddEncina();
        services.AddSingleton<ContextRecorder>();
        services.AddScoped<IRequestHandler<ContractPing, string>, PingHandler>();
        services.AddScoped<IPipelineBehavior<ContractPing, string>, RecordingBehavior>();
        services.AddScoped<INotificationHandler<ContractNotification>, NotificationHandler>();
        services.AddScoped<IStreamRequestHandler<ContractNumbers, int>, NumbersHandler>();
        _provider?.Dispose();
        _provider = services.BuildServiceProvider();
        return _provider.GetRequiredService<IEncina>();
    }

    [Fact]
    public async Task Send_WithExplicitContext_ThePipelineReceivesThatContext()
    {
        var sut = CreateSut();
        var context = RequestContext.CreateForTest(userId: "explicit-user", tenantId: "explicit-tenant");

        await sut.Send(new ContractPing("x"), context);

        _provider!.GetRequiredService<ContextRecorder>().Seen.ShouldBeSameAs(context);
    }
}

/// <summary>The test double shipped in <c>Encina.Testing.Fakes</c>.</summary>
public sealed class FakeEncinaExplicitContextContractTests : EncinaExplicitContextContract
{
    protected override IEncina CreateSut()
    {
        var fake = new FakeEncina();
        fake.SetupResponse<ContractPing, string>(request => request.Text.ToUpperInvariant());
        fake.SetupStream<ContractNumbers, int>([1, 2, 3]);
        return fake;
    }

    [Fact]
    public async Task ExplicitOverloads_AreRecordedLikeTheAmbientOnes()
    {
        var fake = (FakeEncina)CreateSut();
        var context = RequestContext.CreateForTest(userId: "u");

        await fake.Send(new ContractPing("a"), context);
        await fake.Publish(new ContractNotification(), context);
        await foreach (var _ in fake.Stream(new ContractNumbers(), context))
        {
        }

        fake.WasSent<ContractPing>().ShouldBeTrue();
        fake.WasPublished<ContractNotification>().ShouldBeTrue();
        fake.WasStreamed<ContractNumbers>().ShouldBeTrue();
    }
}
