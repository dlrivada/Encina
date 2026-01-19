using Encina.Messaging.Inbox;
using Encina.Messaging.Outbox;
using Encina.Messaging.Sagas;
using Encina.Messaging.Scheduling;
using FsCheck;
using LanguageExt;

namespace Encina.Testing.FsCheck;

/// <summary>
/// FsCheck arbitrary provider for automatic registration of Encina arbitraries.
/// </summary>
/// <remarks>
/// This class provides static properties for all Encina type arbitraries that can be
/// used with FsCheck's type-based lookup system for property-based testing.
/// </remarks>
public static class EncinaArbitraryProvider
{
    /// <summary>
    /// Provides an arbitrary for <see cref="EncinaError"/>.
    /// </summary>
    public static Arbitrary<EncinaError> EncinaError => EncinaArbitraries.EncinaError();

    /// <summary>
    /// Provides an arbitrary for <see cref="IRequestContext"/>.
    /// </summary>
    public static Arbitrary<IRequestContext> RequestContext => EncinaArbitraries.RequestContext();

    /// <summary>
    /// Provides an arbitrary for <see cref="IOutboxMessage"/>.
    /// </summary>
    public static Arbitrary<IOutboxMessage> OutboxMessage => EncinaArbitraries.OutboxMessage();

    /// <summary>
    /// Provides an arbitrary for <see cref="IInboxMessage"/>.
    /// </summary>
    public static Arbitrary<IInboxMessage> InboxMessage => EncinaArbitraries.InboxMessage();

    /// <summary>
    /// Provides an arbitrary for <see cref="ISagaState"/>.
    /// </summary>
    public static Arbitrary<ISagaState> SagaState => EncinaArbitraries.SagaState();

    /// <summary>
    /// Provides an arbitrary for <see cref="IScheduledMessage"/>.
    /// </summary>
    public static Arbitrary<IScheduledMessage> ScheduledMessage => EncinaArbitraries.ScheduledMessage();

    /// <summary>
    /// Provides an arbitrary for Either with EncinaError left and int right.
    /// </summary>
    public static Arbitrary<Either<EncinaError, int>> EitherInt => EncinaArbitraries.EitherOf<int>();

    /// <summary>
    /// Provides an arbitrary for Either with EncinaError left and string right.
    /// </summary>
    public static Arbitrary<Either<EncinaError, string>> EitherString => EncinaArbitraries.EitherOf<string>();

    /// <summary>
    /// Provides an arbitrary for Either with EncinaError left and Guid right.
    /// </summary>
    public static Arbitrary<Either<EncinaError, Guid>> EitherGuid => EncinaArbitraries.EitherOf<Guid>();
}
