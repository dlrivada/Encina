namespace Encina.Messaging.Outbox;

/// <summary>
/// Which component writes the notifications of an <see cref="IHasNotifications"/> request to the outbox.
/// </summary>
/// <remarks>
/// Interim switch while the providers move one family at a time (#718): ADO.NET uses
/// <see cref="TransactionalBehavior"/>; Dapper, EF Core and MongoDB keep <see cref="PostProcessor"/> until
/// their own slice lands. The enum is deleted together with <see cref="OutboxPostProcessor{TRequest, TResponse}"/>
/// when the last family migrates.
/// </remarks>
internal enum OutboxWritePath
{
    /// <summary>The post-processor writes after the pipeline, outside the business transaction.</summary>
    PostProcessor,

    /// <summary>The transactional behavior writes inside the request's transaction.</summary>
    TransactionalBehavior
}
