using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.Tests.Fixtures;

internal sealed record PingCommand(string Value) : ICommand<string>;

internal sealed class PingCommandHandler : ICommandHandler<PingCommand, string>
{
    public Task<Either<MediatorError, string>> Handle(PingCommand request, CancellationToken cancellationToken)
        => Task.FromResult(Right<MediatorError, string>(request.Value));
}

internal sealed record PongQuery(int Id) : IQuery<string>;

internal sealed class PongQueryHandler : IQueryHandler<PongQuery, string>
{
    public Task<Either<MediatorError, string>> Handle(PongQuery request, CancellationToken cancellationToken)
        => Task.FromResult(Right<MediatorError, string>($"pong:{request.Id}"));
}

internal sealed record DomainNotification(int Value) : INotification;

internal sealed class DomainNotificationAlphaHandler : INotificationHandler<DomainNotification>
{
    public Task<Either<MediatorError, Unit>> Handle(DomainNotification notification, CancellationToken cancellationToken)
        => Task.FromResult(Right<MediatorError, Unit>(Unit.Default));
}

internal sealed class DomainNotificationBetaHandler : INotificationHandler<DomainNotification>
{
    public Task<Either<MediatorError, Unit>> Handle(DomainNotification notification, CancellationToken cancellationToken)
        => Task.FromResult(Right<MediatorError, Unit>(Unit.Default));
}

internal sealed class PassThroughPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public ValueTask<Either<MediatorError, TResponse>> Handle(TRequest request, IRequestContext context, RequestHandlerCallback<TResponse> nextStep, CancellationToken cancellationToken)
        => nextStep();
}

internal sealed class ConcreteCommandBehavior : ICommandPipelineBehavior<PingCommand, string>
{
    public ValueTask<Either<MediatorError, string>> Handle(PingCommand request, IRequestContext context, RequestHandlerCallback<string> nextStep, CancellationToken cancellationToken)
        => nextStep();
}

internal sealed class ConcreteQueryBehavior : IQueryPipelineBehavior<PongQuery, string>
{
    public ValueTask<Either<MediatorError, string>> Handle(PongQuery request, IRequestContext context, RequestHandlerCallback<string> nextStep, CancellationToken cancellationToken)
        => nextStep();
}

internal sealed class SamplePreProcessor : IRequestPreProcessor<PingCommand>
{
    public Task Process(PingCommand request, IRequestContext context, CancellationToken cancellationToken)
        => Task.CompletedTask;
}

internal sealed class SamplePostProcessor : IRequestPostProcessor<PingCommand, string>
{
    public Task Process(PingCommand request, IRequestContext context, Either<MediatorError, string> response, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
