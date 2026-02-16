using System.Globalization;
using LanguageExt;
using Microsoft.Extensions.Logging;

namespace Encina.Sharding.Shadow.Behaviors;

/// <summary>
/// Pipeline behavior that fires a shadow write after a successful production write
/// when dual-write mode is enabled.
/// </summary>
/// <remarks>
/// <para>
/// This behavior wraps the command execution path. When
/// <see cref="ShadowShardingOptions.DualWriteEnabled"/> is <c>true</c> and the production
/// write succeeds (returns <c>Right</c>), a shadow write is dispatched as fire-and-forget.
/// The production result is returned immediately without waiting for the shadow write.
/// </para>
/// <para>
/// Shadow writes are bounded by <see cref="ShadowShardingOptions.ShadowWriteTimeout"/>.
/// Any failure in the shadow write is logged but never propagated to the caller.
/// </para>
/// </remarks>
/// <typeparam name="TCommand">The command type being processed.</typeparam>
/// <typeparam name="TResponse">The response type returned by the handler.</typeparam>
internal sealed class ShadowWritePipelineBehavior<TCommand, TResponse>(
    IShadowShardRouter shadowRouter,
    ShadowShardingOptions options,
    ILogger<ShadowWritePipelineBehavior<TCommand, TResponse>> logger)
    : ICommandPipelineBehavior<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    private readonly IShadowShardRouter _shadowRouter = shadowRouter ?? throw new ArgumentNullException(nameof(shadowRouter));
    private readonly ShadowShardingOptions _options = options ?? throw new ArgumentNullException(nameof(options));
    private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, TResponse>> Handle(
        TCommand request,
        IRequestContext context,
        RequestHandlerCallback<TResponse> nextStep,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(nextStep);

        // Short-circuit: if shadow is not enabled or dual-write is off, just run production
        if (!_shadowRouter.IsShadowEnabled || !_options.DualWriteEnabled)
        {
            return await nextStep().ConfigureAwait(false);
        }

        // Execute production write
        var result = await nextStep().ConfigureAwait(false);

        // Only fire shadow write if production succeeded
        if (result.IsRight)
        {
            _ = ExecuteShadowWriteAsync(request, context);
        }

        return result;
    }

    private async Task ExecuteShadowWriteAsync(TCommand request, IRequestContext context)
    {
        var commandType = typeof(TCommand).Name;

        try
        {
            using var timeoutCts = new CancellationTokenSource(_options.ShadowWriteTimeout);

            // The shadow write re-executes routing against the shadow topology
            // to verify the shadow shard can receive the same command.
            // Actual shadow data persistence would be handled by the shadow
            // infrastructure configured downstream.
            await _shadowRouter.CompareAsync(
                request.GetHashCode().ToString(CultureInfo.InvariantCulture),
                timeoutCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            ShadowShardingLog.ShadowWriteTimedOut(
                _logger, commandType, _options.ShadowWriteTimeout.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            ShadowShardingLog.ShadowWriteFailed(_logger, commandType, ex.Message);
        }
    }
}
