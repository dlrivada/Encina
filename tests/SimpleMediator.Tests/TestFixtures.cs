using System.Threading;
using System.Threading.Tasks;
using SimpleMediator;

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
    public Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
        => next();
}

internal sealed class ConcreteCommandBehavior : ICommandPipelineBehavior<PingCommand, string>
{
    public Task<string> Handle(PingCommand request, CancellationToken cancellationToken, RequestHandlerDelegate<string> next)
        => next();
}

internal sealed class ConcreteQueryBehavior : IQueryPipelineBehavior<PongQuery, string>
{
    public Task<string> Handle(PongQuery request, CancellationToken cancellationToken, RequestHandlerDelegate<string> next)
        => next();
}

internal sealed class SamplePreProcessor : IRequestPreProcessor<PingCommand>
{
    public Task Process(PingCommand request, CancellationToken cancellationToken)
        => Task.CompletedTask;
}

internal sealed class SamplePostProcessor : IRequestPostProcessor<PingCommand, string>
{
    public Task Process(PingCommand request, string response, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
