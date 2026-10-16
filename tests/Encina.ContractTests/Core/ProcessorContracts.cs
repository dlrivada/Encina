using LanguageExt;
using Shouldly;

namespace Encina.ContractTests.Core;

/// <summary>
/// Contracts of the four request processor interfaces, exercised through real implementations.
/// </summary>
public sealed class ProcessorContracts
{
    private static readonly IRequestContext Context = RequestContext.CreateForTest();

    [Fact]
    public async Task PreProcessor_ReturnsEitherUnit_ThatCanAbort()
    {
        IRequestPreProcessor<string> ok = new Pre(null);
        IRequestPreProcessor<string> abort = new Pre(EncinaError.New("stop"));

        (await ok.Process("r", Context, CancellationToken.None)).IsRight.ShouldBeTrue();
        (await abort.Process("r", Context, CancellationToken.None)).IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task PostProcessor_ReceivesTheResponseValue_AndCanFail()
    {
        var post = new Post(EncinaError.New("late"));
        IRequestPostProcessor<string, int> contract = post;

        var result = await contract.Process("r", Context, 42, CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        post.Received.ShouldBe(42);
    }

    [Fact]
    public async Task ErrorProcessor_ObservesTheError_AndReturnsNoResult()
    {
        var processor = new ErrorObserver();
        IRequestErrorProcessor<string, int> contract = processor;
        var failure = EncinaError.New("boom");

        await contract.Process("r", Context, failure, CancellationToken.None);

        processor.Observed.ShouldBe(failure);
    }

    [Fact]
    public async Task PostCommitProcessor_ReturnsEitherUnit_ThatCanFailTheRequest()
    {
        IRequestPostCommitProcessor<string, int> contract = new PostCommit(EncinaError.New("after commit"));

        var result = await contract.Process("r", Context, 7, CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void ErrorProcessor_ProcessReturnsNonGenericValueTask()
    {
        typeof(IRequestErrorProcessor<,>).GetMethod("Process")!.ReturnType.ShouldBe(typeof(ValueTask));
    }

    private sealed class Pre(EncinaError? failure) : IRequestPreProcessor<string>
    {
        public ValueTask<Either<EncinaError, Unit>> Process(string request, IRequestContext context, CancellationToken cancellationToken)
            => ValueTask.FromResult<Either<EncinaError, Unit>>(failure is { } error ? error : Unit.Default);
    }

    private sealed class Post(EncinaError? failure) : IRequestPostProcessor<string, int>
    {
        public int? Received { get; private set; }

        public ValueTask<Either<EncinaError, Unit>> Process(string request, IRequestContext context, int response, CancellationToken cancellationToken)
        {
            Received = response;
            return ValueTask.FromResult<Either<EncinaError, Unit>>(failure is { } error ? error : Unit.Default);
        }
    }

    private sealed class ErrorObserver : IRequestErrorProcessor<string, int>
    {
        public EncinaError? Observed { get; private set; }

        public ValueTask Process(string request, IRequestContext context, EncinaError failure, CancellationToken cancellationToken)
        {
            Observed = failure;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class PostCommit(EncinaError? failure) : IRequestPostCommitProcessor<string, int>
    {
        public ValueTask<Either<EncinaError, Unit>> Process(string request, IRequestContext context, int response, CancellationToken cancellationToken)
            => ValueTask.FromResult<Either<EncinaError, Unit>>(failure is { } error ? error : Unit.Default);
    }
}
