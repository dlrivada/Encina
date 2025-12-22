using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.MassTransit;

/// <summary>
/// MassTransit consumer that bridges incoming messages to Encina requests.
/// </summary>
/// <typeparam name="TRequest">The request type implementing IRequest{TResponse}.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public sealed class MassTransitRequestConsumer<TRequest, TResponse> : IConsumer<TRequest>
    where TRequest : class, IRequest<TResponse>
{
    private readonly IEncina _Encina;
    private readonly ILogger<MassTransitRequestConsumer<TRequest, TResponse>> _logger;
    private readonly EncinaMassTransitOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="MassTransitRequestConsumer{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="Encina">The Encina instance.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="options">The configuration options.</param>
    public MassTransitRequestConsumer(
        IEncina Encina,
        ILogger<MassTransitRequestConsumer<TRequest, TResponse>> logger,
        IOptions<EncinaMassTransitOptions> options)
    {
        ArgumentNullException.ThrowIfNull(Encina);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(options);

        _Encina = Encina;
        _logger = logger;
        _options = options.Value;
    }

    /// <summary>
    /// Consumes a MassTransit message and forwards it to Encina.
    /// </summary>
    /// <param name="context">The consume context containing the message.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task Consume(ConsumeContext<TRequest> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var requestType = typeof(TRequest).Name;

        Log.ConsumingRequest(_logger, requestType, context.MessageId);

        var result = await _Encina.Send(context.Message, context.CancellationToken)
            .ConfigureAwait(false);

        result.Match(
            Right: response =>
            {
                Log.ProcessedRequest(_logger, requestType, context.MessageId);
            },
            Left: error =>
            {
                Log.FailedToProcessRequest(_logger, requestType, context.MessageId, error.Message);

                if (_options.ThrowOnEncinaError)
                {
                    throw new EncinaConsumerException(error);
                }
            });
    }
}
