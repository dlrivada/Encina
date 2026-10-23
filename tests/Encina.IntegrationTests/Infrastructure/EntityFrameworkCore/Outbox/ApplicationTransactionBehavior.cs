using LanguageExt;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.Outbox;

/// <summary>Switches the application's own transaction on and records what happened to it.</summary>
public sealed class ApplicationTransactionState
{
    /// <summary>Gets or sets whether the transaction was still current when the request came back.</summary>
    public bool StillCurrentAfterRequest { get; set; }
}

/// <summary>
/// Application code that begins its own transaction on the request scope's context (Encina dispatches every
/// request in its own scope, so application code can only share a scope with Encina from inside the pipeline)
/// and commits it when the request succeeded and the transaction is still current. It does nothing unless an
/// <see cref="ApplicationTransactionState"/> is registered.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public sealed class ApplicationTransactionBehavior<TRequest, TResponse>(DbContext dbContext, IServiceProvider services)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, TResponse>> Handle(
        TRequest request, IRequestContext context, RequestHandlerCallback<TResponse> nextStep, CancellationToken cancellationToken)
    {
        var state = services.GetService<ApplicationTransactionState>();
        if (state is null)
        {
            return await nextStep();
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var result = await nextStep();
        state.StillCurrentAfterRequest = dbContext.Database.CurrentTransaction is not null;
        if (state.StillCurrentAfterRequest)
        {
            if (result.IsRight)
            {
                await transaction.CommitAsync(cancellationToken);
            }
            else
            {
                await transaction.RollbackAsync(cancellationToken);
            }
        }

        return result;
    }
}
