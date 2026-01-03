using System.Globalization;
using Encina.Messaging.Inbox;
using Encina.Messaging.Outbox;
using Encina.Messaging.Sagas;
using Encina.Messaging.Scheduling;
using FsCheck;
using FsCheck.Fluent;
using LanguageExt;

namespace Encina.Testing.FsCheck;

/// <summary>
/// Provides FsCheck arbitraries for Encina types.
/// </summary>
/// <remarks>
/// <para>
/// This class contains pre-built arbitraries for generating random test data
/// for Encina core types and messaging entities. Use these arbitraries in
/// property-based tests to verify invariants across a wide range of inputs.
/// </para>
/// <para>
/// All generators produce deterministic output when used with the same seed.
/// Use <see cref="DefaultSeed"/> with FsCheck's replay functionality for reproducible tests.
/// </para>
/// </remarks>
public static class EncinaArbitraries
{
    /// <summary>
    /// Default seed for reproducible test data generation.
    /// Use this seed with FsCheck's Config.Replay to reproduce test results.
    /// </summary>
    /// <example>
    /// <code>
    /// var config = Config.Default.WithReplay(EncinaArbitraries.DefaultSeed, 0);
    /// Prop.ForAll(...).Check(config);
    /// </code>
    /// </example>
    public const int DefaultSeed = 12345;

    /// <summary>
    /// Base date for deterministic timestamp generation (2020-01-01 00:00:00 UTC).
    /// All generated timestamps are derived from this base to ensure reproducibility.
    /// </summary>
    private static readonly DateTime BaseDate = new(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static readonly string[] NotificationTypes =
    [
        "OrderCreated", "OrderCompleted", "PaymentReceived", "PaymentFailed",
        "ShipmentDispatched", "UserRegistered", "InventoryUpdated", "EmailSent"
    ];

    private static readonly string[] RequestTypes =
    [
        "CreateOrderCommand", "UpdateOrderCommand", "GetOrderQuery",
        "ProcessPaymentCommand", "SendEmailCommand", "GenerateReportQuery"
    ];

    private static readonly string[] SagaTypes =
    [
        "OrderProcessingSaga", "PaymentSaga", "FulfillmentSaga",
        "RegistrationSaga", "RefundSaga", "SubscriptionSaga"
    ];

    /// <summary>
    /// Valid saga status values used for generating saga state instances.
    /// </summary>
    public static readonly string[] SagaStatuses = ["Running", "Completed", "Compensating", "Failed"];

    private static readonly string[] CronExpressions =
    [
        "0 * * * *", "0 0 * * *", "0 0 * * 1", "0 0 1 * *", "*/5 * * * *"
    ];

    /// <summary>
    /// This method is retained for API compatibility only and performs no operation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// In FsCheck 3.x, arbitraries are automatically registered via <see cref="EncinaArbitraryProvider"/>.
    /// There is no need to call this method; it exists solely to prevent breaking changes for
    /// code that previously called <c>EncinaArbitraries.Register()</c>.
    /// </para>
    /// <para>
    /// To use Encina arbitraries, simply reference the <c>Encina.Testing.FsCheck</c> package
    /// and use <c>ArbMap.Default</c> or the individual arbitrary factory methods like
    /// <see cref="EncinaError()"/>, <see cref="OutboxMessage()"/>, etc.
    /// </para>
    /// </remarks>
    [Obsolete("This method is a no-op. Arbitraries are auto-registered via EncinaArbitraryProvider. Use ArbMap.Default or individual arbitrary factory methods instead.")]
    public static void Register()
    {
        // No-op: In FsCheck 3.x, arbitraries are auto-registered via EncinaArbitraryProvider
    }

    #region Core Types

    /// <summary>
    /// Creates an arbitrary for <see cref="EncinaError"/>.
    /// </summary>
    public static Arbitrary<EncinaError> EncinaError()
    {
        var gen = Gen.SelectMany(
            ArbMap.Default.GeneratorFor<NonEmptyString>(),
            s => Gen.Constant(global::Encina.EncinaError.New(s.Get)));
        return Arb.From(gen);
    }

    /// <summary>
    /// Creates an arbitrary for <see cref="EncinaError"/> with exception metadata.
    /// </summary>
    public static Arbitrary<EncinaError> EncinaErrorWithException()
    {
        var gen = Gen.SelectMany(
            ArbMap.Default.GeneratorFor<NonEmptyString>(),
            msg => Gen.Select(
                ArbMap.Default.GeneratorFor<NonEmptyString>(),
                exMsg => global::Encina.EncinaError.New(msg.Get, new InvalidOperationException(exMsg.Get))));
        return Arb.From(gen);
    }

    /// <summary>
    /// Creates an arbitrary for <see cref="IRequestContext"/>.
    /// </summary>
    public static Arbitrary<IRequestContext> RequestContext()
    {
        var gen = Gen.Select(
            ArbMap.Default.GeneratorFor<Guid>(),
            g => (IRequestContext)global::Encina.RequestContext.CreateForTest(
                correlationId: g.ToString("N", CultureInfo.InvariantCulture)));
        return Arb.From(gen);
    }

    /// <summary>
    /// Creates an arbitrary for Either with EncinaError left and T right.
    /// </summary>
    public static Arbitrary<Either<EncinaError, T>> EitherOf<T>()
    {
        var leftGen = Gen.Select(EncinaError().Generator, err => Either<EncinaError, T>.Left(err));
        var rightGen = Gen.Select(ArbMap.Default.GeneratorFor<T>(), val => Either<EncinaError, T>.Right(val));
        return Arb.From(Gen.OneOf(leftGen, rightGen));
    }

    /// <summary>
    /// Creates an arbitrary for successful Either values only.
    /// </summary>
    public static Arbitrary<Either<EncinaError, T>> SuccessEither<T>()
    {
        var gen = Gen.Select(ArbMap.Default.GeneratorFor<T>(), val => Either<EncinaError, T>.Right(val));
        return Arb.From(gen);
    }

    /// <summary>
    /// Creates an arbitrary for failed Either values only.
    /// </summary>
    public static Arbitrary<Either<EncinaError, T>> FailureEither<T>()
    {
        var gen = Gen.Select(EncinaError().Generator, err => Either<EncinaError, T>.Left(err));
        return Arb.From(gen);
    }

    #endregion

    #region Messaging Types

    /// <summary>
    /// Creates an arbitrary for <see cref="IOutboxMessage"/>.
    /// </summary>
    public static Arbitrary<IOutboxMessage> OutboxMessage()
    {
        var gen = Gen.SelectMany(
            ArbMap.Default.GeneratorFor<Guid>(),
            id => Gen.SelectMany(
                Gen.Choose(0, NotificationTypes.Length - 1),
                idx => Gen.SelectMany(
                    Gen.Choose(0, 365 * 24 * 60), // Minutes offset from base date for deterministic timestamps
                    minutesOffset => Gen.Select(
                        ArbMap.Default.GeneratorFor<bool>(),
                        processed => (IOutboxMessage)CreateOutboxMessage(
                            id,
                            NotificationTypes[idx],
                            "{}",
                            BaseDate.AddMinutes(minutesOffset),
                            processed,
                            0,
                            null)))));
        return Arb.From(gen);
    }

    /// <summary>
    /// Creates an arbitrary for pending (unprocessed) outbox messages.
    /// </summary>
    public static Arbitrary<IOutboxMessage> PendingOutboxMessage()
    {
        var gen = Gen.SelectMany(
            ArbMap.Default.GeneratorFor<Guid>(),
            id => Gen.SelectMany(
                Gen.Choose(0, NotificationTypes.Length - 1),
                idx => Gen.Select(
                    Gen.Choose(0, 365 * 24 * 60), // Minutes offset from base date for deterministic timestamps
                    minutesOffset => (IOutboxMessage)CreateOutboxMessage(
                        id,
                        NotificationTypes[idx],
                        "{}",
                        BaseDate.AddMinutes(minutesOffset),
                        false,
                        0,
                        null))));
        return Arb.From(gen);
    }

    /// <summary>
    /// Creates an arbitrary for failed outbox messages with retry information.
    /// </summary>
    public static Arbitrary<IOutboxMessage> FailedOutboxMessage()
    {
        var gen = Gen.SelectMany(
            ArbMap.Default.GeneratorFor<Guid>(),
            id => Gen.SelectMany(
                Gen.Choose(0, NotificationTypes.Length - 1),
                idx => Gen.SelectMany(
                    Gen.Choose(0, 365 * 24 * 60), // Minutes offset from base date for deterministic timestamps
                    minutesOffset => Gen.Select(
                        Gen.Choose(1, 10),
                        retryCount => (IOutboxMessage)CreateOutboxMessage(
                            id,
                            NotificationTypes[idx],
                            "{}",
                            BaseDate.AddMinutes(minutesOffset),
                            false,
                            retryCount,
                            "Processing failed")))));
        return Arb.From(gen);
    }

    /// <summary>
    /// Creates an arbitrary for <see cref="IInboxMessage"/>.
    /// </summary>
    public static Arbitrary<IInboxMessage> InboxMessage()
    {
        var gen = Gen.SelectMany(
            ArbMap.Default.GeneratorFor<Guid>(),
            id => Gen.SelectMany(
                Gen.Choose(0, RequestTypes.Length - 1),
                idx => Gen.SelectMany(
                    Gen.Choose(0, 365 * 24 * 60), // Minutes offset from base date for deterministic timestamps
                    minutesOffset => Gen.Select(
                        ArbMap.Default.GeneratorFor<bool>(),
                        processed =>
                        {
                            var receivedAt = BaseDate.AddMinutes(minutesOffset);
                            return (IInboxMessage)CreateInboxMessage(
                                id.ToString("N", CultureInfo.InvariantCulture),
                                RequestTypes[idx],
                                receivedAt,
                                receivedAt.AddDays(7),
                                processed,
                                0,
                                null,
                                null);
                        }))));
        return Arb.From(gen);
    }

    /// <summary>
    /// Creates an arbitrary for <see cref="ISagaState"/>.
    /// </summary>
    public static Arbitrary<ISagaState> SagaState()
    {
        var gen = Gen.SelectMany(
            ArbMap.Default.GeneratorFor<Guid>(),
            id => Gen.SelectMany(
                Gen.Choose(0, SagaTypes.Length - 1),
                typeIdx => Gen.SelectMany(
                    Gen.Choose(0, SagaStatuses.Length - 1),
                    statusIdx => Gen.SelectMany(
                        Gen.Choose(0, 365 * 24 * 60), // Minutes offset from base date for deterministic timestamps
                        minutesOffset => Gen.Select(
                            Gen.Choose(0, 10),
                            step => (ISagaState)CreateSagaState(
                                id,
                                SagaTypes[typeIdx],
                                "{}",
                                SagaStatuses[statusIdx],
                                step,
                                BaseDate.AddMinutes(minutesOffset),
                                statusIdx == 1,
                                null,
                                false))))));
        return Arb.From(gen);
    }

    /// <summary>
    /// Creates an arbitrary for <see cref="IScheduledMessage"/>.
    /// </summary>
    public static Arbitrary<IScheduledMessage> ScheduledMessage()
    {
        var gen = Gen.SelectMany(
            ArbMap.Default.GeneratorFor<Guid>(),
            id => Gen.SelectMany(
                Gen.Choose(0, RequestTypes.Length - 1),
                idx => Gen.SelectMany(
                    Gen.Choose(0, 365 * 24 * 60), // Minutes offset from base date for deterministic timestamps
                    minutesOffset => Gen.SelectMany(
                        Gen.Choose(1, 48),
                        hours => Gen.Select(
                            ArbMap.Default.GeneratorFor<bool>(),
                            processed =>
                            {
                                var createdAt = BaseDate.AddMinutes(minutesOffset);
                                return (IScheduledMessage)CreateScheduledMessage(
                                    id,
                                    RequestTypes[idx],
                                    "{}",
                                    createdAt,
                                    createdAt.AddHours(hours),
                                    false,
                                    processed,
                                    0,
                                    null);
                            })))));
        return Arb.From(gen);
    }

    /// <summary>
    /// Creates an arbitrary for recurring scheduled messages.
    /// </summary>
    public static Arbitrary<IScheduledMessage> RecurringScheduledMessage()
    {
        var gen = Gen.SelectMany(
            ArbMap.Default.GeneratorFor<Guid>(),
            id => Gen.SelectMany(
                Gen.Choose(0, RequestTypes.Length - 1),
                idx => Gen.SelectMany(
                    Gen.Choose(0, 365 * 24 * 60), // Minutes offset from base date for deterministic timestamps
                    minutesOffset => Gen.Select(
                        Gen.Choose(0, CronExpressions.Length - 1),
                        cronIdx =>
                        {
                            var createdAt = BaseDate.AddMinutes(minutesOffset);
                            return (IScheduledMessage)CreateRecurringScheduledMessage(
                                id,
                                RequestTypes[idx],
                                "{}",
                                createdAt,
                                createdAt.AddHours(1),
                                CronExpressions[cronIdx],
                                0);
                        }))));
        return Arb.From(gen);
    }

    #endregion

    #region Factory Methods

    private static ArbitraryOutboxMessage CreateOutboxMessage(
        Guid id,
        string notificationType,
        string content,
        DateTime createdAt,
        bool processed,
        int retryCount,
        string? errorMessage)
    {
        return new ArbitraryOutboxMessage
        {
            Id = id,
            NotificationType = notificationType,
            Content = content,
            CreatedAtUtc = createdAt,
            ProcessedAtUtc = processed ? createdAt.AddSeconds(30) : null,
            RetryCount = retryCount,
            ErrorMessage = errorMessage,
            NextRetryAtUtc = retryCount > 0 && !processed ? createdAt.AddMinutes(5) : null
        };
    }

    private static ArbitraryInboxMessage CreateInboxMessage(
        string messageId,
        string requestType,
        DateTime receivedAt,
        DateTime expiresAt,
        bool processed,
        int retryCount,
        string? response,
        string? errorMessage)
    {
        return new ArbitraryInboxMessage
        {
            MessageId = messageId,
            RequestType = requestType,
            ReceivedAtUtc = receivedAt,
            ExpiresAtUtc = expiresAt,
            ProcessedAtUtc = processed ? receivedAt.AddSeconds(30) : null,
            RetryCount = retryCount,
            Response = response,
            ErrorMessage = errorMessage,
            NextRetryAtUtc = retryCount > 0 && !processed ? receivedAt.AddMinutes(5) : null
        };
    }

    private static ArbitrarySagaState CreateSagaState(
        Guid sagaId,
        string sagaType,
        string data,
        string status,
        int currentStep,
        DateTime startedAt,
        bool completed,
        string? errorMessage,
        bool hasTimeout)
    {
        return new ArbitrarySagaState
        {
            SagaId = sagaId,
            SagaType = sagaType,
            Data = data,
            Status = status,
            CurrentStep = currentStep,
            StartedAtUtc = startedAt,
            LastUpdatedAtUtc = startedAt.AddMinutes(5),
            CompletedAtUtc = completed ? startedAt.AddMinutes(10) : null,
            ErrorMessage = errorMessage,
            TimeoutAtUtc = hasTimeout ? startedAt.AddHours(1) : null
        };
    }

    private static ArbitraryScheduledMessage CreateScheduledMessage(
        Guid id,
        string requestType,
        string content,
        DateTime createdAt,
        DateTime scheduledAt,
        bool isRecurring,
        bool processed,
        int retryCount,
        string? errorMessage)
    {
        return new ArbitraryScheduledMessage
        {
            Id = id,
            RequestType = requestType,
            Content = content,
            CreatedAtUtc = createdAt,
            ScheduledAtUtc = scheduledAt,
            IsRecurring = isRecurring,
            CronExpression = isRecurring ? "0 * * * *" : null,
            ProcessedAtUtc = processed ? scheduledAt.AddSeconds(30) : null,
            LastExecutedAtUtc = processed ? scheduledAt.AddSeconds(30) : null,
            RetryCount = retryCount,
            ErrorMessage = errorMessage,
            NextRetryAtUtc = retryCount > 0 && !processed ? scheduledAt.AddMinutes(5) : null
        };
    }

    private static ArbitraryScheduledMessage CreateRecurringScheduledMessage(
        Guid id,
        string requestType,
        string content,
        DateTime createdAt,
        DateTime scheduledAt,
        string cronExpression,
        int retryCount)
    {
        return new ArbitraryScheduledMessage
        {
            Id = id,
            RequestType = requestType,
            Content = content,
            CreatedAtUtc = createdAt,
            ScheduledAtUtc = scheduledAt,
            IsRecurring = true,
            CronExpression = cronExpression,
            ProcessedAtUtc = null,
            LastExecutedAtUtc = null,
            RetryCount = retryCount,
            ErrorMessage = null,
            NextRetryAtUtc = null
        };
    }

    #endregion
}
