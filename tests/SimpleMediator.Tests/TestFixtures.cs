using LanguageExt;

namespace SimpleMediator.Tests.Fixtures;

internal sealed record PingCommand(string Value) : ICommand<string>;

internal sealed class PingCommandHandler : ICommandHandler<PingCommand, string>
{
    public Task<string> Handle(PingCommand request, CancellationToken cancellationToken)
        => Task.FromResult(request.Value);
}

internal sealed record PongQuery(int Id) : IQuery<string>;

internal sealed class PongQueryHandler : IQueryHandler<PongQuery, string>
{
    public Task<string> Handle(PongQuery request, CancellationToken cancellationToken)
        => Task.FromResult($"pong:{request.Id}");
}

internal sealed record DomainNotification(int Value) : INotification;

internal sealed class DomainNotificationAlphaHandler : INotificationHandler<DomainNotification>
{
    public Task Handle(DomainNotification notification, CancellationToken cancellationToken)
        => Task.CompletedTask;
}

internal sealed class DomainNotificationBetaHandler : INotificationHandler<DomainNotification>
{
    public Task Handle(DomainNotification notification, CancellationToken cancellationToken)
        => Task.CompletedTask;
}

internal sealed class PassThroughPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public ValueTask<Either<MediatorError, TResponse>> Handle(TRequest request, RequestHandlerCallback<TResponse> nextStep, CancellationToken cancellationToken)
        => nextStep();
}

internal sealed class ConcreteCommandBehavior : ICommandPipelineBehavior<PingCommand, string>
{
    public ValueTask<Either<MediatorError, string>> Handle(PingCommand request, RequestHandlerCallback<string> nextStep, CancellationToken cancellationToken)
        => nextStep();
}

internal sealed class ConcreteQueryBehavior : IQueryPipelineBehavior<PongQuery, string>
{
    public ValueTask<Either<MediatorError, string>> Handle(PongQuery request, RequestHandlerCallback<string> nextStep, CancellationToken cancellationToken)
        => nextStep();
}

internal sealed class SamplePreProcessor : IRequestPreProcessor<PingCommand>
{
    public Task Process(PingCommand request, CancellationToken cancellationToken)
        => Task.CompletedTask;
}

internal sealed class SamplePostProcessor : IRequestPostProcessor<PingCommand, string>
{
    public Task Process(PingCommand request, Either<MediatorError, string> response, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
