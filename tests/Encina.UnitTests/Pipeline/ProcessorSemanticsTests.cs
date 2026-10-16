using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Encina.UnitTests.Pipeline;

/// <summary>
/// Semantics of the pre- and post-processor contracts: a pre-processor <c>Left</c> aborts before the handler,
/// a post-processor runs only on <c>Right</c>, and a post-processor <c>Left</c> fails the request.
/// </summary>
public sealed class ProcessorSemanticsTests
{
    [Fact]
    public async Task PreProcessorLeft_AbortsAndHandlerIsNotCalled()
    {
        var calls = new List<string>();
        var failure = EncinaError.New("pre failed");

        var result = await SendAsync(calls, pre: new FixedPre(calls, failure));

        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.Message.ShouldBe("pre failed"));
        calls.ShouldBe(["pre"]);
    }

    [Fact]
    public async Task PreProcessorRight_ContinuesToHandler()
    {
        var calls = new List<string>();

        var result = await SendAsync(calls, pre: new FixedPre(calls, null));

        result.IsRight.ShouldBeTrue();
        calls.ShouldBe(["pre", "handler"]);
    }

    [Fact]
    public async Task PostProcessor_RunsOnRight_ReceivingTheResponse()
    {
        var calls = new List<string>();
        var post = new FixedPost(calls, null);

        var result = await SendAsync(calls, post: post);

        result.IsRight.ShouldBeTrue();
        calls.ShouldBe(["handler", "post"]);
        post.Received.ShouldBe("handled");
    }

    [Fact]
    public async Task PostProcessor_DoesNotRunWhenHandlerReturnsLeft()
    {
        var calls = new List<string>();
        var post = new FixedPost(calls, null);

        var result = await SendAsync(calls, post: post, handlerFails: true);

        result.IsLeft.ShouldBeTrue();
        calls.ShouldBe(["handler"]);
        post.Received.ShouldBeNull();
    }

    [Fact]
    public async Task PostProcessorLeft_FailsTheRequest()
    {
        var calls = new List<string>();

        var result = await SendAsync(calls, post: new FixedPost(calls, EncinaError.New("post failed")));

        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.Message.ShouldBe("post failed"));
        calls.ShouldBe(["handler", "post"]);
    }

    [Fact]
    public async Task PreAndPostProcessors_RunAroundTheHandler()
    {
        var calls = new List<string>();

        var result = await SendAsync(calls, pre: new FixedPre(calls, null), post: new FixedPost(calls, null));

        result.IsRight.ShouldBeTrue();
        calls.ShouldBe(["pre", "handler", "post"]);
    }

    [Fact]
    public async Task PreProcessorLeft_SkipsLaterPreProcessorsAndBehaviors()
    {
        var calls = new List<string>();
        var services = new ServiceCollection();
        services.AddEncina();
        services.AddScoped<IRequestHandler<Req, string>>(_ => new Handler(calls, false));
        services.AddScoped<IRequestPreProcessor<Req>>(_ => new FixedPre(calls, EncinaError.New("first")));
        services.AddScoped<IRequestPreProcessor<Req>>(_ => new SecondPre(calls));
        services.AddScoped<IPipelineBehavior<Req, string>>(_ => new RecordingBehavior(calls));

        await using var provider = services.BuildServiceProvider();
        var result = await provider.GetRequiredService<IEncina>().Send(new Req(), CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        calls.ShouldBe(["pre"]);
    }

    [Fact]
    public async Task PostProcessorLeft_SkipsLaterPostProcessors()
    {
        var calls = new List<string>();
        var services = new ServiceCollection();
        services.AddEncina();
        services.AddScoped<IRequestHandler<Req, string>>(_ => new Handler(calls, false));
        services.AddScoped<IRequestPostProcessor<Req, string>>(_ => new FixedPost(calls, EncinaError.New("first")));
        services.AddScoped<IRequestPostProcessor<Req, string>>(_ => new SecondPost(calls));

        await using var provider = services.BuildServiceProvider();
        var result = await provider.GetRequiredService<IEncina>().Send(new Req(), CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.Message.ShouldBe("first"));
        // FixedPost sorts before SecondPost by name, so it is the outer step and runs last on the way out;
        // SecondPost (inner) runs first and succeeds, then FixedPost fails the request.
        calls.ShouldBe(["handler", "second-post", "post"]);
    }

    [Fact]
    public async Task PostProcessors_OfDistinctTypes_RunInTheReverseOfTheirStepOrder()
    {
        var calls = new List<string>();
        var services = new ServiceCollection();
        services.AddEncina();
        services.AddScoped<IRequestHandler<Req, string>>(_ => new Handler(calls, false));
        services.AddScoped<IRequestPostProcessor<Req, string>>(_ => new SecondPost(calls));
        services.AddScoped<IRequestPostProcessor<Req, string>>(_ => new FixedPost(calls, null));

        await using var provider = services.BuildServiceProvider();
        var result = await provider.GetRequiredService<IEncina>().Send(new Req(), CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        calls.ShouldBe(["handler", "second-post", "post"]);
    }

    private sealed class SecondPre(List<string> calls) : IRequestPreProcessor<Req>
    {
        public ValueTask<Either<EncinaError, Unit>> Process(Req request, IRequestContext context, CancellationToken cancellationToken)
        {
            calls.Add("second-pre");
            return ValueTask.FromResult<Either<EncinaError, Unit>>(Unit.Default);
        }
    }

    private sealed class SecondPost(List<string> calls) : IRequestPostProcessor<Req, string>
    {
        public ValueTask<Either<EncinaError, Unit>> Process(Req request, IRequestContext context, string response, CancellationToken cancellationToken)
        {
            calls.Add("second-post");
            return ValueTask.FromResult<Either<EncinaError, Unit>>(Unit.Default);
        }
    }

    private sealed class RecordingBehavior(List<string> calls) : IPipelineBehavior<Req, string>
    {
        public ValueTask<Either<EncinaError, string>> Handle(
            Req request, IRequestContext context, RequestHandlerCallback<string> nextStep, CancellationToken cancellationToken)
        {
            calls.Add("behavior");
            return nextStep();
        }
    }

    private static async Task<Either<EncinaError, string>> SendAsync(
        List<string> calls,
        FixedPre? pre = null,
        FixedPost? post = null,
        bool handlerFails = false)
    {
        var services = new ServiceCollection();
        services.AddEncina();
        services.AddScoped<IRequestHandler<Req, string>>(_ => new Handler(calls, handlerFails));
        if (pre is not null)
        {
            services.AddScoped<IRequestPreProcessor<Req>>(_ => pre);
        }

        if (post is not null)
        {
            services.AddScoped<IRequestPostProcessor<Req, string>>(_ => post);
        }

        await using var provider = services.BuildServiceProvider();
        var encina = provider.GetRequiredService<IEncina>();
        return await encina.Send(new Req(), CancellationToken.None);
    }

    private sealed record Req : IRequest<string>;

    private sealed class Handler(List<string> calls, bool fails) : IRequestHandler<Req, string>
    {
        public Task<Either<EncinaError, string>> Handle(Req request, CancellationToken cancellationToken)
        {
            calls.Add("handler");
            return Task.FromResult<Either<EncinaError, string>>(
                fails
                    ? Either<EncinaError, string>.Left(EncinaError.New("handler failed"))
                    : Either<EncinaError, string>.Right("handled"));
        }
    }

    private sealed class FixedPre(List<string> calls, EncinaError? failure) : IRequestPreProcessor<Req>
    {
        public ValueTask<Either<EncinaError, Unit>> Process(Req request, IRequestContext context, CancellationToken cancellationToken)
        {
            calls.Add("pre");
            return ValueTask.FromResult<Either<EncinaError, Unit>>(
                failure is { } error ? error : Unit.Default);
        }
    }

    private sealed class FixedPost(List<string> calls, EncinaError? failure) : IRequestPostProcessor<Req, string>
    {
        public string? Received { get; private set; }

        public ValueTask<Either<EncinaError, Unit>> Process(Req request, IRequestContext context, string response, CancellationToken cancellationToken)
        {
            calls.Add("post");
            Received = response;
            return ValueTask.FromResult<Either<EncinaError, Unit>>(
                failure is { } error ? error : Unit.Default);
        }
    }
}
