using System.Text.Json;
using Encina.Messaging.DeadLetter;
using Encina.Messaging.Serialization;
using Encina.Testing.Shouldly;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Messaging.DeadLetter;

/// <summary>
/// Unit tests for DeadLetterOrchestrator.
/// </summary>
public sealed class DeadLetterOrchestratorTests
{
    private static readonly DateTime FixedUtcNow = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private readonly IDeadLetterStore _store;
    private readonly IDeadLetterMessageFactory _messageFactory;
    private readonly DeadLetterOptions _options;
    private readonly ILogger<DeadLetterOrchestrator> _logger;
    private readonly IMessageSerializer _messageSerializer;
    private readonly IRequestContextAccessor _accessor;
    private readonly DeadLetterOrchestrator _orchestrator;

    public DeadLetterOrchestratorTests()
    {
        _accessor = Substitute.For<IRequestContextAccessor>();
        _store = Substitute.For<IDeadLetterStore>();
        _store.AddAsync(Arg.Any<IDeadLetterMessage>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, bool>(true));
        _store.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(unit));
        _messageFactory = Substitute.For<IDeadLetterMessageFactory>();
        _options = new DeadLetterOptions
        {
            RetentionPeriod = TimeSpan.FromDays(7),
            CleanupInterval = TimeSpan.FromHours(1),
            EnableAutomaticCleanup = true
        };
        _logger = Substitute.For<ILogger<DeadLetterOrchestrator>>();
        _messageSerializer = new JsonMessageSerializer();

        _orchestrator = new DeadLetterOrchestrator(_store, _messageFactory, _options, _logger, _messageSerializer, _accessor);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullStore_ThrowsArgumentNullException()
    {
        var act = () => new DeadLetterOrchestrator(null!, _messageFactory, _options, _logger, _messageSerializer, _accessor);

        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("store");
    }

    [Fact]
    public void Constructor_NullMessageFactory_ThrowsArgumentNullException()
    {
        var act = () => new DeadLetterOrchestrator(_store, null!, _options, _logger, _messageSerializer, _accessor);

        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("messageFactory");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new DeadLetterOrchestrator(_store, _messageFactory, null!, _logger, _messageSerializer, _accessor);

        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new DeadLetterOrchestrator(_store, _messageFactory, _options, null!, _messageSerializer, _accessor);

        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("logger");
    }

    [Fact]
    public void Constructor_NullMessageSerializer_ThrowsArgumentNullException()
    {
        var act = () => new DeadLetterOrchestrator(_store, _messageFactory, _options, _logger, null!, _accessor);

        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("messageSerializer");
    }

    [Fact]
    public void Constructor_NullRequestContextAccessor_ThrowsArgumentNullException()
    {
        var act = () => new DeadLetterOrchestrator(_store, _messageFactory, _options, _logger, _messageSerializer, null!);

        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("requestContextAccessor");
    }

    #endregion

    #region AddAsync Tests

    [Fact]
    public async Task AddAsync_ValidRequest_CreatesAndStoresMessage()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        var request = new TestDeadLetterRequest { Id = requestId, Data = "Test" };
        var error = EncinaErrors.Create("test.error", "Test error");
        var sourcePattern = DeadLetterSourcePatterns.Recoverability;
        var firstFailedAt = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        var expectedRequestType = typeof(TestDeadLetterRequest).AssemblyQualifiedName!;
        var expectedRequestContent = JsonSerializer.Serialize(request);
        var expectedRetryCount = 3;
        var expectedRetentionPeriod = _options.RetentionPeriod!.Value;

        var expectedMessage = CreateTestDeadLetterMessage(Guid.NewGuid());
        _messageFactory.Create(Arg.Any<DeadLetterData>())
            .Returns(expectedMessage);

        // Act
        var context = new DeadLetterContext(error, null, sourcePattern, expectedRetryCount, firstFailedAt);
        var result = await _orchestrator.AddAsync(request, context);

        // Assert
        result.ShouldBeRight().ShouldBe(expectedMessage);
        await _store.Received(1).AddAsync(expectedMessage, Arg.Any<CancellationToken>());
        await _store.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddAsync_CalledWithABaseTypeParameter_StoresTheRuntimeTypeNotTheDeclaredOne()
    {
        // Arrange - calling AddAsync<TRequest> with TRequest bound to a base/interface type while
        // passing a derived instance must not desync RequestType (stored from the declared type
        // parameter) from RequestContent (serialized from the runtime type by
        // SerializeAsRuntimeType), or DeadLetterManager.ReplayAsync cannot resolve the type back
        // (#1259 review).
        var request = new DerivedDeadLetterRequest { Id = Guid.NewGuid(), Data = "Test" };
        var error = EncinaErrors.Create("test.error", "Test error");
        var context = new DeadLetterContext(
            error, null, DeadLetterSourcePatterns.Recoverability, TotalRetryAttempts: 1, FirstFailedAtUtc: FixedUtcNow);

        var expectedRuntimeType = typeof(DerivedDeadLetterRequest).AssemblyQualifiedName!;
        var expectedMessage = CreateTestDeadLetterMessage(Guid.NewGuid());
        _messageFactory.Create(Arg.Any<DeadLetterData>()).Returns(expectedMessage);

        // Act - TRequest is explicitly the base type, but the instance passed is the derived one.
        await _orchestrator.AddAsync<BaseDeadLetterRequest>(request, context);

        // Assert
        _messageFactory.Received(1).Create(Arg.Is<DeadLetterData>(d =>
            d.RequestType == expectedRuntimeType));
    }

    [Fact]
    public async Task AddAsync_WithException_KeepsExceptionTypeButNotItsMessage()
    {
        // Arrange
        var request = new TestDeadLetterRequest { Id = Guid.NewGuid() };
        var error = EncinaErrors.Create("test.error", "Test error");
        var exception = new InvalidOperationException("Something went wrong");
        var sourcePattern = DeadLetterSourcePatterns.Outbox;
        var firstFailedAt = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        const int retryCount = 3;

        var expectedRequestType = typeof(TestDeadLetterRequest).AssemblyQualifiedName!;
        var expectedMessage = CreateTestDeadLetterMessage(Guid.NewGuid());

        _messageFactory.Create(Arg.Is<DeadLetterData>(d =>
            d.RequestType == expectedRequestType &&
            d.ErrorCode == "test.error" &&
            d.SourcePattern == sourcePattern &&
            d.TotalRetryAttempts == retryCount &&
            d.FirstFailedAtUtc == firstFailedAt &&
            d.ExceptionType == typeof(InvalidOperationException).FullName))
            .Returns(expectedMessage);

        // Act
        var context = new DeadLetterContext(error, exception, sourcePattern, retryCount, firstFailedAt);
        await _orchestrator.AddAsync(request, context);

        // Assert
        _messageFactory.Received(1).Create(Arg.Is<DeadLetterData>(d =>
            d.RequestType == expectedRequestType &&
            d.ErrorCode == "test.error" &&
            d.ErrorCode != "Test error" &&
            d.SourcePattern == sourcePattern &&
            d.TotalRetryAttempts == retryCount &&
            d.FirstFailedAtUtc == firstFailedAt &&
            d.ExceptionType == typeof(InvalidOperationException).FullName &&
            d.ExceptionStackTrace == null));
    }

    [Fact]
    public async Task AddAsync_WithOnDeadLetterCallback_InvokesCallback()
    {
        // Arrange
        var callbackInvoked = false;
        IDeadLetterMessage? callbackMessage = null;

        var optionsWithCallback = new DeadLetterOptions
        {
            RetentionPeriod = TimeSpan.FromDays(7),
            OnDeadLetter = (msg, ct) =>
            {
                callbackInvoked = true;
                callbackMessage = msg;
                return Task.CompletedTask;
            }
        };

        var orchestrator = new DeadLetterOrchestrator(
            _store, _messageFactory, optionsWithCallback, _logger, _messageSerializer, _accessor);

        var request = new TestDeadLetterRequest { Id = Guid.NewGuid() };
        var error = EncinaErrors.Create("test.error", "Test error");
        var expectedMessage = CreateTestDeadLetterMessage(Guid.NewGuid());

        _messageFactory.Create(Arg.Any<DeadLetterData>())
            .Returns(expectedMessage);

        // Act
        var context = new DeadLetterContext(error, null, DeadLetterSourcePatterns.Recoverability, 3, FixedUtcNow);
        await orchestrator.AddAsync(request, context);

        // Assert
        callbackInvoked.ShouldBeTrue();
        callbackMessage.ShouldBe(expectedMessage);
    }

    [Fact]
    public async Task AddAsync_CallbackThrows_DoesNotPropagateException()
    {
        // Arrange
        var optionsWithCallback = new DeadLetterOptions
        {
            RetentionPeriod = TimeSpan.FromDays(7),
            OnDeadLetter = (msg, ct) => throw new InvalidOperationException("Callback failed")
        };

        var orchestrator = new DeadLetterOrchestrator(
            _store, _messageFactory, optionsWithCallback, _logger, _messageSerializer, _accessor);

        var request = new TestDeadLetterRequest { Id = Guid.NewGuid() };
        var error = EncinaErrors.Create("test.error", "Test error");
        var expectedMessage = CreateTestDeadLetterMessage(Guid.NewGuid());

        _messageFactory.Create(Arg.Any<DeadLetterData>())
            .Returns(expectedMessage);

        // Act & Assert - should not throw
        var context = new DeadLetterContext(error, null, DeadLetterSourcePatterns.Recoverability, 3, FixedUtcNow);
        var result = await orchestrator.AddAsync(request, context);

        result.ShouldBeRight().ShouldBe(expectedMessage);
    }

    [Fact]
    public async Task AddAsync_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var error = EncinaErrors.Create("test.error", "Test error");
        var context = new DeadLetterContext(error, null, DeadLetterSourcePatterns.Recoverability, 3, FixedUtcNow);

        // Act
        var act = async () => await _orchestrator.AddAsync<TestDeadLetterRequest>(null!, context);

        // Assert
        var ex = await act.ShouldThrowAsync<ArgumentNullException>();
        ex.ParamName.ShouldBe("request");
    }

    [Fact]
    public async Task AddAsync_NullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var request = new TestDeadLetterRequest { Id = Guid.NewGuid() };

        // Act
        var act = async () => await _orchestrator.AddAsync(request, null!);

        // Assert
        var ex = await act.ShouldThrowAsync<ArgumentNullException>();
        ex.ParamName.ShouldBe("context");
    }

    [Fact]
    public async Task AddAsync_NullSourcePattern_ThrowsArgumentException()
    {
        // Arrange
        var request = new TestDeadLetterRequest { Id = Guid.NewGuid() };
        var error = EncinaErrors.Create("test.error", "Test error");
        var context = new DeadLetterContext(error, null, null!, 3, FixedUtcNow);

        // Act
        var act = async () => await _orchestrator.AddAsync(request, context);

        // Assert
        var ex = await act.ShouldThrowAsync<ArgumentException>();
    }

    #endregion

    #region GetAsync Tests

    [Fact]
    public async Task GetAsync_ExistingMessage_ReturnsMessage()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var expectedMessage = CreateTestDeadLetterMessage(messageId);

        _store.GetAsync(messageId, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Option<IDeadLetterMessage>>(Option<IDeadLetterMessage>.Some(expectedMessage)));

        // Act
        var result = await _orchestrator.GetAsync(messageId);

        // Assert
        var option = result.ShouldBeRight();
        option.IsSome.ShouldBeTrue();
        option.Match(
            Some: msg => msg.ShouldBe(expectedMessage),
            None: () => Assert.Fail("Expected Some"));
    }

    [Fact]
    public async Task GetAsync_NonExistentMessage_ReturnsNone()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        _store.GetAsync(messageId, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Option<IDeadLetterMessage>>(Option<IDeadLetterMessage>.None));

        // Act
        var result = await _orchestrator.GetAsync(messageId);

        // Assert
        var option = result.ShouldBeRight();
        option.IsNone.ShouldBeTrue();
    }

    #endregion

    #region GetPendingCountAsync Tests

    [Fact]
    public async Task GetPendingCountAsync_ReturnsCountFromStore()
    {
        // Arrange
        _store.GetCountAsync(
            Arg.Is<DeadLetterFilter>(f => f.ExcludeReplayed == true),
            Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, int>(5));

        // Act
        var result = await _orchestrator.GetPendingCountAsync();

        // Assert
        result.ShouldBeRight().ShouldBe(5);
    }

    #endregion

    #region CleanupExpiredAsync Tests

    [Fact]
    public async Task CleanupExpiredAsync_DelegatesToStore()
    {
        // Arrange
        _store.DeleteExpiredAsync(Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, int>(10));

        // Act
        var result = await _orchestrator.CleanupExpiredAsync();

        // Assert
        result.ShouldBeRight().ShouldBe(10);
        await _store.Received(1).DeleteExpiredAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CleanupExpiredAsync_NoExpiredMessages_ReturnsZero()
    {
        // Arrange
        _store.DeleteExpiredAsync(Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, int>(0));

        // Act
        var result = await _orchestrator.CleanupExpiredAsync();

        // Assert
        result.ShouldBeRight().ShouldBe(0);
    }

    #endregion

    #region GetStatisticsAsync Tests

    [Fact]
    public async Task GetStatisticsAsync_ReturnsCorrectStatistics()
    {
        // Arrange
        ArrangeStatisticsCounts(total: 100, pending: 80, expired: 5);
        var oldest = CreateTestDeadLetterMessage(Guid.NewGuid());
        oldest.DeadLetteredAtUtc = FixedUtcNow.AddDays(-3);
        var newest = CreateTestDeadLetterMessage(Guid.NewGuid());
        newest.DeadLetteredAtUtc = FixedUtcNow;
        _store.GetMessagesAsync(
            Arg.Any<DeadLetterFilter>(), 0, 1, false, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IEnumerable<IDeadLetterMessage>>(new IDeadLetterMessage[] { oldest }));
        _store.GetMessagesAsync(
            Arg.Any<DeadLetterFilter>(), 0, 1, true, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IEnumerable<IDeadLetterMessage>>(new IDeadLetterMessage[] { newest }));

        // Act
        var result = await _orchestrator.GetStatisticsAsync();

        // Assert
        var statistics = result.ShouldBeRight();
        statistics.TotalCount.ShouldBe(100);
        statistics.PendingCount.ShouldBe(80);
        statistics.ReplayedCount.ShouldBe(20);
        statistics.ExpiredCount.ShouldBe(5);
        statistics.OldestPendingAtUtc.ShouldBe(oldest.DeadLetteredAtUtc);
        statistics.NewestPendingAtUtc.ShouldBe(newest.DeadLetteredAtUtc);
    }

    [Fact]
    public async Task GetStatisticsAsync_NeverLoadsTheQueue_ReadsOnlySingleRows()
    {
        ArrangeStatisticsCounts(total: 10, pending: 4, expired: 1);
        _store.GetMessagesAsync(
            Arg.Any<DeadLetterFilter>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IEnumerable<IDeadLetterMessage>>(System.Array.Empty<IDeadLetterMessage>()));

        await _orchestrator.GetStatisticsAsync();

        await _store.DidNotReceive().GetMessagesAsync(
            Arg.Any<DeadLetterFilter>(), Arg.Any<int>(), Arg.Is<int>(take => take != 1), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetStatisticsAsync_CountsExpiredRowsInTheStoreAtTheProvidedInstant()
    {
        var now = new DateTimeOffset(FixedUtcNow);
        var orchestrator = new DeadLetterOrchestrator(
            _store, _messageFactory, _options, _logger, _messageSerializer, _accessor, new FakeTimeProvider(now));
        ArrangeStatisticsCounts(total: 3, pending: 3, expired: 2);
        _store.GetMessagesAsync(
            Arg.Any<DeadLetterFilter>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IEnumerable<IDeadLetterMessage>>(System.Array.Empty<IDeadLetterMessage>()));

        var statistics = (await orchestrator.GetStatisticsAsync()).ShouldBeRight();

        statistics.ExpiredCount.ShouldBe(2);
        await _store.Received().GetCountAsync(
            Arg.Is<DeadLetterFilter>(f => f.ExpiresAtOrBeforeUtc == FixedUtcNow && f.ExcludeReplayed == true),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetStatisticsAsync_AmbientTenant_ScopesEveryRead()
    {
        UseAmbientTenant("tenant-a");
        ArrangeStatisticsCounts(total: 1, pending: 1, expired: 0);
        _store.GetMessagesAsync(
            Arg.Any<DeadLetterFilter>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IEnumerable<IDeadLetterMessage>>(System.Array.Empty<IDeadLetterMessage>()));

        await _orchestrator.GetStatisticsAsync();

        await _store.DidNotReceive().GetCountAsync(
            Arg.Is<DeadLetterFilter>(f => f.TenantId != "tenant-a"), Arg.Any<CancellationToken>());
        await _store.DidNotReceive().GetMessagesAsync(
            Arg.Is<DeadLetterFilter>(f => f.TenantId != "tenant-a"), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetStatisticsAsync_CountFails_ReturnsTheLeft()
    {
        _store.GetCountAsync(Arg.Any<DeadLetterFilter>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, int>(EncinaErrors.Create("db.down", "down")));

        var result = await _orchestrator.GetStatisticsAsync();

        result.ShouldBeError();
    }

    [Fact]
    public async Task GetStatisticsAsync_PendingReadFails_ReturnsTheLeft()
    {
        ArrangeStatisticsCounts(total: 1, pending: 1, expired: 0);
        _store.GetMessagesAsync(
            Arg.Any<DeadLetterFilter>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, IEnumerable<IDeadLetterMessage>>(EncinaErrors.Create("db.down", "down")));

        var result = await _orchestrator.GetStatisticsAsync();

        result.ShouldBeError();
    }

    #endregion

    #region Capture Tests (idempotency, tenant, store failures)

    [Fact]
    public async Task AddAsync_DuplicateSource_ReturnsExistingAndSkipsCallbackAndSave()
    {
        var callbacks = 0;
        var options = new DeadLetterOptions { OnDeadLetter = (_, _) => { callbacks++; return Task.CompletedTask; } };
        var orchestrator = new DeadLetterOrchestrator(
            _store, _messageFactory, options, _logger, _messageSerializer, _accessor);
        var incoming = CreateTestDeadLetterMessage(Guid.NewGuid());
        var existing = CreateTestDeadLetterMessage(Guid.NewGuid());
        _messageFactory.Create(Arg.Any<DeadLetterData>()).Returns(incoming);
        _store.AddAsync(incoming, Arg.Any<CancellationToken>()).Returns(Right<EncinaError, bool>(false));
        _store.GetMessagesAsync(
            Arg.Is<DeadLetterFilter>(f => f.SourcePattern == "Outbox" && f.SourceMessageId == "src-1"),
            0, 1, false, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IEnumerable<IDeadLetterMessage>>(new IDeadLetterMessage[] { existing }));

        var context = new DeadLetterContext(
            EncinaErrors.Create("e", "m"), null, "Outbox", 1, FixedUtcNow, SourceMessageId: "src-1");
        var result = await orchestrator.AddAsync(new TestDeadLetterRequest(), context);

        result.ShouldBeRight().ShouldBe(existing);
        callbacks.ShouldBe(0);
        await _store.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddAsync_DuplicateWhoseOriginalWasDeleted_ReturnsStoreFailedError()
    {
        var incoming = CreateTestDeadLetterMessage(Guid.NewGuid());
        _messageFactory.Create(Arg.Any<DeadLetterData>()).Returns(incoming);
        _store.AddAsync(incoming, Arg.Any<CancellationToken>()).Returns(Right<EncinaError, bool>(false));
        _store.GetMessagesAsync(
            Arg.Any<DeadLetterFilter>(), 0, 1, false, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IEnumerable<IDeadLetterMessage>>(System.Array.Empty<IDeadLetterMessage>()));

        var context = new DeadLetterContext(EncinaErrors.Create("e", "m"), null, "Outbox", 1, FixedUtcNow);
        var result = await _orchestrator.AddAsync(new TestDeadLetterRequest(), context);

        result.ShouldBeError();
    }

    [Fact]
    public async Task AddAsync_DuplicateLookupFails_ReturnsTheLeft()
    {
        var incoming = CreateTestDeadLetterMessage(Guid.NewGuid());
        _messageFactory.Create(Arg.Any<DeadLetterData>()).Returns(incoming);
        _store.AddAsync(incoming, Arg.Any<CancellationToken>()).Returns(Right<EncinaError, bool>(false));
        _store.GetMessagesAsync(
            Arg.Any<DeadLetterFilter>(), 0, 1, false, Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, IEnumerable<IDeadLetterMessage>>(EncinaErrors.Create("db.down", "down")));

        var context = new DeadLetterContext(EncinaErrors.Create("e", "m"), null, "Outbox", 1, FixedUtcNow);
        var result = await _orchestrator.AddAsync(new TestDeadLetterRequest(), context);

        result.ShouldBeError();
    }

    [Fact]
    public async Task AddAsync_StoreAddFails_ReturnsTheLeftWithoutSaving()
    {
        var message = CreateTestDeadLetterMessage(Guid.NewGuid());
        _messageFactory.Create(Arg.Any<DeadLetterData>()).Returns(message);
        _store.AddAsync(message, Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, bool>(EncinaErrors.Create("db.down", "down")));

        var context = new DeadLetterContext(EncinaErrors.Create("e", "m"), null, "Outbox", 1, FixedUtcNow);
        var result = await _orchestrator.AddAsync(new TestDeadLetterRequest(), context);

        result.ShouldBeError();
        await _store.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddAsync_SaveFails_ReturnsTheLeft()
    {
        var message = CreateTestDeadLetterMessage(Guid.NewGuid());
        _messageFactory.Create(Arg.Any<DeadLetterData>()).Returns(message);
        _store.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, Unit>(EncinaErrors.Create("db.down", "down")));

        var context = new DeadLetterContext(EncinaErrors.Create("e", "m"), null, "Outbox", 1, FixedUtcNow);
        var result = await _orchestrator.AddAsync(new TestDeadLetterRequest(), context);

        result.ShouldBeError();
    }

    [Fact]
    public async Task AddAsync_NoSourceMessageId_UsesTheNewDeadLetterIdAndAmbientTenant()
    {
        UseAmbientTenant("tenant-a");
        _messageFactory.Create(Arg.Any<DeadLetterData>()).Returns(CreateTestDeadLetterMessage(Guid.NewGuid()));

        var context = new DeadLetterContext(EncinaErrors.Create("e", "m"), null, "Outbox", 1, FixedUtcNow);
        await _orchestrator.AddAsync(new TestDeadLetterRequest(), context);

        _messageFactory.Received(1).Create(Arg.Is<DeadLetterData>(d =>
            d.SourceMessageId == d.Id.ToString("D") && d.TenantId == "tenant-a"));
    }

    [Fact]
    public async Task AddAsync_ContextOverrides_WinOverTheAmbientTenant()
    {
        UseAmbientTenant("tenant-a");
        _messageFactory.Create(Arg.Any<DeadLetterData>()).Returns(CreateTestDeadLetterMessage(Guid.NewGuid()));

        var context = new DeadLetterContext(
            EncinaErrors.Create("e", "m"), null, "Outbox", 1, FixedUtcNow, SourceMessageId: "src-9", TenantId: "tenant-b");
        await _orchestrator.AddAsync(new TestDeadLetterRequest(), context);

        _messageFactory.Received(1).Create(Arg.Is<DeadLetterData>(d =>
            d.SourceMessageId == "src-9" && d.TenantId == "tenant-b"));
    }

    [Fact]
    public async Task AddFromFailedMessageAsync_UsesTheFailedMessageIdAsSourceMessageId()
    {
        var failedId = Guid.NewGuid();
        var failed = new global::Encina.Messaging.Recoverability.FailedMessage
        {
            Id = failedId,
            Request = new TestDeadLetterRequest(),
            RequestType = typeof(TestDeadLetterRequest).AssemblyQualifiedName!,
            Error = EncinaErrors.Create("e", "m"),
            TotalAttempts = 2,
            ImmediateRetryAttempts = 1,
            DelayedRetryAttempts = 1,
            FirstAttemptAtUtc = FixedUtcNow,
            FailedAtUtc = FixedUtcNow
        };
        _messageFactory.Create(Arg.Any<DeadLetterData>()).Returns(CreateTestDeadLetterMessage(Guid.NewGuid()));

        var result = await _orchestrator.AddFromFailedMessageAsync(failed, DeadLetterSourcePatterns.Recoverability);

        result.ShouldBeRight();
        _messageFactory.Received(1).Create(Arg.Is<DeadLetterData>(d =>
            d.SourceMessageId == failedId.ToString("D") && d.ErrorCode == "e"));
    }

    [Fact]
    public async Task AddAsync_ComputesExpiryFromTheTimeProvider()
    {
        var orchestrator = new DeadLetterOrchestrator(
            _store, _messageFactory, _options, _logger, _messageSerializer, _accessor,
            new FakeTimeProvider(new DateTimeOffset(FixedUtcNow)));
        _messageFactory.Create(Arg.Any<DeadLetterData>()).Returns(CreateTestDeadLetterMessage(Guid.NewGuid()));

        var context = new DeadLetterContext(EncinaErrors.Create("e", "m"), null, "Outbox", 1, FixedUtcNow);
        await orchestrator.AddAsync(new TestDeadLetterRequest(), context);

        _messageFactory.Received(1).Create(Arg.Is<DeadLetterData>(d =>
            d.DeadLetteredAtUtc == FixedUtcNow && d.ExpiresAtUtc == FixedUtcNow.AddDays(7)));
    }

    [Fact]
    public async Task AddAsync_NoRetentionPeriod_LeavesExpiryNull()
    {
        var options = new DeadLetterOptions { RetentionPeriod = null };
        var orchestrator = new DeadLetterOrchestrator(
            _store, _messageFactory, options, _logger, _messageSerializer, _accessor);
        _messageFactory.Create(Arg.Any<DeadLetterData>()).Returns(CreateTestDeadLetterMessage(Guid.NewGuid()));

        var context = new DeadLetterContext(EncinaErrors.Create("e", "m"), null, "Outbox", 1, FixedUtcNow);
        await orchestrator.AddAsync(new TestDeadLetterRequest(), context);

        _messageFactory.Received(1).Create(Arg.Is<DeadLetterData>(d => d.ExpiresAtUtc == null));
    }

    #endregion

    #region Helpers

    private void UseAmbientTenant(string tenantId)
    {
        var context = Substitute.For<IRequestContext>();
        context.TenantId.Returns(tenantId);
        _accessor.RequestContext.Returns(context);
    }

    // total = every row; pending = ExcludeReplayed; expired = ExpiresAtOrBeforeUtc set. Per-source counts are 0.
    private void ArrangeStatisticsCounts(int total, int pending, int expired)
    {
        _store.GetCountAsync(Arg.Any<DeadLetterFilter>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, int>(0));
        _store.GetCountAsync(
            Arg.Is<DeadLetterFilter>(f => f.ExcludeReplayed == null && f.SourcePattern == null),
            Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, int>(total));
        _store.GetCountAsync(
            Arg.Is<DeadLetterFilter>(f => f.ExcludeReplayed == true && f.SourcePattern == null && f.ExpiresAtOrBeforeUtc == null),
            Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, int>(pending));
        _store.GetCountAsync(
            Arg.Is<DeadLetterFilter>(f => f.ExpiresAtOrBeforeUtc != null),
            Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, int>(expired));
    }

    private static TestDeadLetterMessage CreateTestDeadLetterMessage(Guid id)
    {
        return new TestDeadLetterMessage
        {
            Id = id,
            RequestType = typeof(TestDeadLetterRequest).AssemblyQualifiedName!,
            RequestContent = "{}",
            ErrorCode = "test.error",
            SourceMessageId = id.ToString("D"),
            SourcePattern = DeadLetterSourcePatterns.Recoverability,
            TotalRetryAttempts = 3,
            FirstFailedAtUtc = FixedUtcNow.AddMinutes(-5),
            DeadLetteredAtUtc = FixedUtcNow
        };
    }

    #endregion
}

/// <summary>
/// Test request for dead letter tests.
/// </summary>
public sealed class TestDeadLetterRequest
{
    public Guid Id { get; set; }
    public string Data { get; set; } = string.Empty;
}

/// <summary>Base type used to exercise <c>AddAsync&lt;TRequest&gt;</c> with a declared type
/// parameter narrower than the instance's runtime type.</summary>
public abstract class BaseDeadLetterRequest
{
    public Guid Id { get; set; }
}

/// <summary>Derived request whose runtime type must be the one stored in <c>RequestType</c>,
/// not <see cref="BaseDeadLetterRequest"/>.</summary>
public sealed class DerivedDeadLetterRequest : BaseDeadLetterRequest
{
    public string Data { get; set; } = string.Empty;
}

/// <summary>
/// Test implementation of IDeadLetterMessage for unit tests.
/// </summary>
internal sealed class TestDeadLetterMessage : IDeadLetterMessage
{
    public Guid Id { get; set; }
    public string RequestType { get; set; } = string.Empty;
    public string RequestContent { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public string? ExceptionType { get; set; }
    public string? ExceptionStackTrace { get; set; }
    public string? CorrelationId { get; set; }
    public string SourcePattern { get; set; } = string.Empty;
    public string SourceMessageId { get; set; } = string.Empty;
    public string? TenantId { get; set; }
    public int TotalRetryAttempts { get; set; }
    public DateTime FirstFailedAtUtc { get; set; }
    public DateTime DeadLetteredAtUtc { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public DateTime? ReplayClaimedAtUtc { get; set; }
    public DateTime? ReplayedAtUtc { get; set; }
    public string? ReplayResult { get; set; }

    public bool IsReplayed => ReplayedAtUtc.HasValue;
    public bool IsExpiredAt(DateTime utcNow) => ExpiresAtUtc is { } expiresAtUtc && expiresAtUtc <= utcNow;
}

