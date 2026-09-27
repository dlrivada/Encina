#pragma warning disable CA2012 // ValueTask consumed by NSubstitute mock setup

using System.Diagnostics;
using System.Text;
using Encina.Messaging.Encryption;
using Encina.Messaging.Encryption.Abstractions;
using Encina.Messaging.Encryption.Diagnostics;
using Encina.Messaging.Encryption.Health;
using Encina.Messaging.Encryption.Model;
using Encina.Messaging.Encryption.Serialization;
using Encina.Messaging.Serialization;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Testing;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Messaging.Encryption;

/// <summary>
/// <see cref="EncinaError.Message"/> must never reach the log, the OpenTelemetry activity tag/status
/// description, or the health-check result of <c>Encina.Messaging.Encryption</c> (#1489, following
/// #1454 and #1168/#1173/#1259/#1274). Only the error code
/// (<c>error.GetCode().IfNone("encina.unknown")</c>) may reach any of those channels. Covers
/// <see cref="EncryptingMessageSerializer.Serialize{T}"/>, its <c>DecryptIfNeeded</c> helper (reached
/// through <see cref="EncryptingMessageSerializer.Deserialize{T}"/>), and
/// <see cref="MessageEncryptionHealthCheck"/>.
/// </summary>
public sealed class EncryptingMessageSerializerErrorMessageLeakTests
{
    // A sentinel that must never appear in a log message, an activity tag/status description or a
    // health-check result. Chosen to be extremely unlikely to collide with any error code or fixed
    // outcome string used by the production code under test.
    private const string SentinelMessage = "SENTINEL-ERROR-MESSAGE-do-not-log-or-tag-this-7a2f91";
    private const string ErrorCode = "msg_encryption.encryption_failed";

    [Fact]
    public void Serialize_EncryptionFails_LogsAndTagsOnlyTheErrorCode()
    {
        // Arrange
        var inner = Substitute.For<IMessageSerializer>();
        var provider = Substitute.For<IMessageEncryptionProvider>();
        inner.Serialize(Arg.Any<TestMessage>()).Returns("{\"Name\":\"test\"}");

        var error = EncinaErrors.Create(ErrorCode, SentinelMessage);
        provider.EncryptAsync(
            Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<MessageEncryptionContext>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, EncryptedPayload>(error));

        var logger = new FakeLogger<EncryptingMessageSerializer>();
        var options = Options.Create(new MessageEncryptionOptions
        {
            Enabled = true,
            EncryptAllMessages = true,
            EnableTracing = true
        });
        var serializer = new EncryptingMessageSerializer(inner, provider, options, logger);

        using var capture = new DiagnosticsCapture();

        // Act
        Action act = () => DiagnosticsCapture.Run(() => serializer.Serialize(new TestMessage { Name = "test" }));

        // Assert
        Should.Throw<InvalidOperationException>(act);

        var logs = logger.Collector.GetSnapshot();
        logs.ShouldContain(r => r.Message.Contains(ErrorCode));
        logs.ShouldAllBe(r => !r.Message.Contains(SentinelMessage));

        capture.AssertNoSentinel(SentinelMessage);
        capture.AssertActivityStatusIs(ErrorCode);
    }

    [Fact]
    public void Deserialize_DecryptionFails_LogsAndTagsOnlyTheErrorCode()
    {
        // Arrange
        var inner = Substitute.For<IMessageSerializer>();
        var provider = Substitute.For<IMessageEncryptionProvider>();

        var payload = new EncryptedPayload
        {
            Ciphertext = System.Collections.Immutable.ImmutableArray.Create<byte>(1, 2, 3),
            KeyId = "bad-key",
            Algorithm = "AES-256-GCM",
            Nonce = System.Collections.Immutable.ImmutableArray.Create<byte>(10, 20, 30),
            Tag = System.Collections.Immutable.ImmutableArray.Create<byte>(40, 50, 60),
            Version = 1
        };
        var encryptedString = EncryptedPayloadFormatter.Format(payload);

        var error = EncinaErrors.Create("msg_encryption.decryption_failed", SentinelMessage);
        provider.DecryptAsync(
            Arg.Any<EncryptedPayload>(), Arg.Any<MessageEncryptionContext>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, System.Collections.Immutable.ImmutableArray<byte>>(error));

        var logger = new FakeLogger<EncryptingMessageSerializer>();
        var options = Options.Create(new MessageEncryptionOptions { EnableTracing = true });
        var serializer = new EncryptingMessageSerializer(inner, provider, options, logger);

        using var capture = new DiagnosticsCapture();

        // Act
        Action act = () => DiagnosticsCapture.Run(() => serializer.Deserialize<TestMessage>(encryptedString));

        // Assert
        Should.Throw<InvalidOperationException>(act);

        var logs = logger.Collector.GetSnapshot();
        logs.ShouldContain(r => r.Message.Contains("msg_encryption.decryption_failed"));
        logs.ShouldAllBe(r => !r.Message.Contains(SentinelMessage));

        capture.AssertNoSentinel(SentinelMessage);
        capture.AssertActivityStatusIs("msg_encryption.decryption_failed");
    }

    [Fact]
    public async Task HealthCheck_EncryptPhaseFails_ReportsAndLogsOnlyTheErrorCode()
    {
        // Arrange
        var provider = Substitute.For<IMessageEncryptionProvider>();
        var error = EncinaErrors.Create(ErrorCode, SentinelMessage);
        provider.EncryptAsync(
            Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<MessageEncryptionContext>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, EncryptedPayload>(error));

        var services = new ServiceCollection();
        services.AddSingleton(provider);
        await using var serviceProvider = services.BuildServiceProvider();

        var logger = new FakeLogger<MessageEncryptionHealthCheck>();
        var check = new MessageEncryptionHealthCheck(serviceProvider, logger);

        // Act
        var result = await check.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldNotBeNull();
        result.Description!.ShouldContain(ErrorCode);
        result.Description!.ShouldNotContain(SentinelMessage);

        var logs = logger.Collector.GetSnapshot();
        logs.ShouldContain(r => r.Message.Contains(ErrorCode));
        logs.ShouldAllBe(r => !r.Message.Contains(SentinelMessage));
    }

    [Fact]
    public async Task HealthCheck_DecryptPhaseFails_ReportsAndLogsOnlyTheErrorCode()
    {
        // Arrange
        var provider = Substitute.For<IMessageEncryptionProvider>();
        var encryptedPayload = new EncryptedPayload
        {
            Ciphertext = System.Collections.Immutable.ImmutableArray.Create<byte>(1, 2, 3),
            KeyId = "probe-key",
            Algorithm = "AES-256-GCM",
            Nonce = System.Collections.Immutable.ImmutableArray.Create<byte>(10, 20, 30),
            Tag = System.Collections.Immutable.ImmutableArray.Create<byte>(40, 50, 60),
            Version = 1
        };
        provider.EncryptAsync(
            Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<MessageEncryptionContext>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, EncryptedPayload>(encryptedPayload));

        var decryptError = EncinaErrors.Create("msg_encryption.decryption_failed", SentinelMessage);
        provider.DecryptAsync(
            Arg.Any<EncryptedPayload>(), Arg.Any<MessageEncryptionContext>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, System.Collections.Immutable.ImmutableArray<byte>>(decryptError));

        var services = new ServiceCollection();
        services.AddSingleton(provider);
        await using var serviceProvider = services.BuildServiceProvider();

        var logger = new FakeLogger<MessageEncryptionHealthCheck>();
        var check = new MessageEncryptionHealthCheck(serviceProvider, logger);

        // Act
        var result = await check.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldNotBeNull();
        result.Description!.ShouldContain("msg_encryption.decryption_failed");
        result.Description!.ShouldNotContain(SentinelMessage);

        var logs = logger.Collector.GetSnapshot();
        logs.ShouldContain(r => r.Message.Contains("msg_encryption.decryption_failed"));
        logs.ShouldAllBe(r => !r.Message.Contains(SentinelMessage));
    }

    public sealed class TestMessage
    {
        public string? Name { get; set; }
    }

    /// <summary>
    /// Captures every activity tag and status description recorded on
    /// <see cref="MessageEncryptionDiagnostics.ActivitySource"/> during a scoped call. An
    /// <see cref="AsyncLocal{T}"/> flag scopes capture to the action passed to <see cref="Run"/>, so
    /// a process-global listener never picks up another test's concurrently running activity on the
    /// same shared source (following #1454; see #1423, where a catch-all listener without this
    /// scoping made a Messaging test flaky).
    /// </summary>
    private sealed class DiagnosticsCapture : IDisposable
    {
        private static readonly AsyncLocal<bool> Capturing = new();

        private readonly ActivityListener _listener;
        private readonly Lock _sync = new();
        private readonly List<Activity> _activities = [];

        public DiagnosticsCapture()
        {
            _listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == MessageEncryptionDiagnostics.SourceName,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                ActivityStopped = activity =>
                {
                    if (!Capturing.Value)
                    {
                        return;
                    }

                    lock (_sync)
                    {
                        _activities.Add(activity);
                    }
                }
            };
            ActivitySource.AddActivityListener(_listener);
        }

        public static void Run(Action action)
        {
            Capturing.Value = true;
            try
            {
                action();
            }
            finally
            {
                Capturing.Value = false;
            }
        }

        public void AssertNoSentinel(string sentinel)
        {
            List<Activity> snapshot;
            lock (_sync)
            {
                snapshot = [.. _activities];
            }

            snapshot.ShouldNotBeEmpty();

            foreach (var activity in snapshot)
            {
                (activity.StatusDescription ?? string.Empty).ShouldNotContain(sentinel);
                foreach (var tag in activity.Tags)
                {
                    (tag.Value ?? string.Empty).ShouldNotContain(sentinel);
                }
            }
        }

        public void AssertActivityStatusIs(string expectedCode)
        {
            List<Activity> snapshot;
            lock (_sync)
            {
                snapshot = [.. _activities];
            }

            snapshot.ShouldContain(a => a.StatusDescription == expectedCode);
        }

        public void Dispose() => _listener.Dispose();
    }
}
#pragma warning restore CA2012
