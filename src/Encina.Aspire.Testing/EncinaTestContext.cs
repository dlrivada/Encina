using Encina.Messaging.DeadLetter;
using Encina.Messaging.Inbox;
using Encina.Messaging.Outbox;
using Encina.Messaging.Sagas;
using Encina.Messaging.Scheduling;
using Encina.Testing.Fakes.Stores;

namespace Encina.Aspire.Testing;

/// <summary>
/// Provides access to Encina test state and operations for integration tests.
/// </summary>
/// <remarks>
/// This class is automatically registered when using <see cref="DistributedApplicationTestingBuilderExtensions.WithEncinaTestSupport"/>.
/// It provides a centralized way to access fake stores, clear data, and perform test-specific operations.
/// </remarks>
public sealed class EncinaTestContext
{
    private readonly EncinaTestSupportOptions _options;
    private readonly FakeOutboxStore? _outboxStore;
    private readonly FakeInboxStore? _inboxStore;
    private readonly FakeSagaStore? _sagaStore;
    private readonly FakeScheduledMessageStore? _scheduledMessageStore;
    private readonly FakeDeadLetterStore? _deadLetterStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="EncinaTestContext"/> class.
    /// </summary>
    /// <param name="options">The test support options.</param>
    /// <param name="outboxStore">The fake outbox store (optional).</param>
    /// <param name="inboxStore">The fake inbox store (optional).</param>
    /// <param name="sagaStore">The fake saga store (optional).</param>
    /// <param name="scheduledMessageStore">The fake scheduled message store (optional).</param>
    /// <param name="deadLetterStore">The fake dead letter store (optional).</param>
    public EncinaTestContext(
        EncinaTestSupportOptions options,
        FakeOutboxStore? outboxStore = null,
        FakeInboxStore? inboxStore = null,
        FakeSagaStore? sagaStore = null,
        FakeScheduledMessageStore? scheduledMessageStore = null,
        FakeDeadLetterStore? deadLetterStore = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _outboxStore = outboxStore;
        _inboxStore = inboxStore;
        _sagaStore = sagaStore;
        _scheduledMessageStore = scheduledMessageStore;
        _deadLetterStore = deadLetterStore;
    }

    /// <summary>
    /// Gets the fake outbox store for verification and inspection.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the outbox store is not configured.</exception>
    public FakeOutboxStore OutboxStore =>
        _outboxStore ?? throw new InvalidOperationException("Outbox store is not configured. Ensure AddEncinaFakes() was called.");

    /// <summary>
    /// Gets the fake inbox store for verification and inspection.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the inbox store is not configured.</exception>
    public FakeInboxStore InboxStore =>
        _inboxStore ?? throw new InvalidOperationException("Inbox store is not configured. Ensure AddEncinaFakes() was called.");

    /// <summary>
    /// Gets the fake saga store for verification and inspection.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the saga store is not configured.</exception>
    public FakeSagaStore SagaStore =>
        _sagaStore ?? throw new InvalidOperationException("Saga store is not configured. Ensure AddEncinaFakes() was called.");

    /// <summary>
    /// Gets the fake scheduled message store for verification and inspection.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the scheduled message store is not configured.</exception>
    public FakeScheduledMessageStore ScheduledMessageStore =>
        _scheduledMessageStore ?? throw new InvalidOperationException("Scheduled message store is not configured. Ensure AddEncinaFakes() was called.");

    /// <summary>
    /// Gets the fake dead letter store for verification and inspection.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the dead letter store is not configured.</exception>
    public FakeDeadLetterStore DeadLetterStore =>
        _deadLetterStore ?? throw new InvalidOperationException("Dead letter store is not configured. Ensure AddEncinaFakes() was called.");

    /// <summary>
    /// Gets the test support options.
    /// </summary>
    public EncinaTestSupportOptions Options => _options;

    /// <summary>
    /// Clears all test data based on the configured options.
    /// </summary>
    /// <remarks>
    /// This method respects the <see cref="EncinaTestSupportOptions"/> settings
    /// to determine which stores to clear.
    /// </remarks>
    public void ClearAll()
    {
        if (_options.ClearOutboxBeforeTest)
        {
            _outboxStore?.Clear();
        }

        if (_options.ClearInboxBeforeTest)
        {
            _inboxStore?.Clear();
        }

        if (_options.ResetSagasBeforeTest)
        {
            _sagaStore?.Clear();
        }

        if (_options.ClearScheduledMessagesBeforeTest)
        {
            _scheduledMessageStore?.Clear();
        }

        if (_options.ClearDeadLetterBeforeTest)
        {
            _deadLetterStore?.Clear();
        }
    }

    /// <summary>
    /// Clears all outbox messages.
    /// </summary>
    public void ClearOutbox() => _outboxStore?.Clear();

    /// <summary>
    /// Clears all inbox messages.
    /// </summary>
    public void ClearInbox() => _inboxStore?.Clear();

    /// <summary>
    /// Clears all sagas.
    /// </summary>
    public void ClearSagas() => _sagaStore?.Clear();

    /// <summary>
    /// Clears all scheduled messages.
    /// </summary>
    public void ClearScheduledMessages() => _scheduledMessageStore?.Clear();

    /// <summary>
    /// Clears all dead letter messages.
    /// </summary>
    public void ClearDeadLetter() => _deadLetterStore?.Clear();
}
