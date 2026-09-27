using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.Metrics;

using Encina.Compliance.DataSubjectRights;
using Encina.Marten.GDPR;
using Encina.Marten.GDPR.Abstractions;
using Encina.Marten.GDPR.Diagnostics;

using LanguageExt;

using Marten;

using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using Shouldly;

using static LanguageExt.Prelude;

using ISerializer = Marten.ISerializer;

namespace Encina.UnitTests.Marten.GDPR;

/// <summary>
/// The data subject's own identifier must never reach a log message, an OpenTelemetry activity
/// tag/status, or a metric tag produced by <c>Encina.Marten.GDPR</c> diagnostics (#1429, following
/// #1314). Covers <see cref="CryptoShreddingDiagnostics"/>, <see cref="CryptoShreddingLogMessages"/>
/// and their call sites in <see cref="CryptoShredderSerializer"/>, <see cref="InMemorySubjectKeyProvider"/>
/// and <see cref="CryptoShredErasureStrategy"/>.
/// </summary>
/// <remarks>
/// The audit trail is out of scope: an <c>IAuditStore</c> that legitimately records the subject id
/// is the access-controlled place for it (see the knowledge record for #1429).
/// </remarks>
#pragma warning disable CA2012 // Use ValueTasks correctly (NSubstitute Returns with ValueTask)
public sealed class CryptoShreddingDiagnosticsSubjectIdLeakTests : IDisposable
{
    private const string SubjectId = "patient-42";

    private readonly ISubjectKeyProvider _mockKeyProvider = Substitute.For<ISubjectKeyProvider>();
    private readonly IForgottenSubjectHandler _mockForgottenHandler = Substitute.For<IForgottenSubjectHandler>();

    public void Dispose() => CryptoShreddedPropertyCache.ClearCache();

    #region CryptoShredderSerializer — encryption

    [Fact]
    public void ToJson_EncryptsField_NeverCarriesSubjectId()
    {
        var mockInner = Substitute.For<ISerializer>();
        mockInner.ToJson(Arg.Any<PiiEvent>()).Returns("{}");

        var keyMaterial = new byte[32];
        Random.Shared.NextBytes(keyMaterial);
        _mockKeyProvider.GetOrCreateSubjectKeyAsync(SubjectId, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, byte[]>(keyMaterial));
        _mockKeyProvider.GetSubjectInfoAsync(SubjectId, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, SubjectEncryptionInfo>(new SubjectEncryptionInfo
            {
                SubjectId = SubjectId,
                Status = SubjectStatus.Active,
                ActiveKeyVersion = 1,
                TotalKeyVersions = 1,
                CreatedAtUtc = DateTimeOffset.UtcNow
            }));

        var (logger, capture) = CreateCapture<CryptoShredderSerializer>();
        using (capture)
        {
            var sut = new CryptoShredderSerializer(mockInner, _mockKeyProvider, _mockForgottenHandler, logger);
            var evt = new PiiEvent { UserId = SubjectId, Email = "test@example.com" };

            DiagnosticsCapture.Capture(() => sut.ToJson(evt));
        }

        capture.AssertNoSubjectId(logger, SubjectId, expectActivity: true);
    }

    [Fact]
    public void EncryptField_ProviderFails_NeverCarriesSubjectId()
    {
        var mockInner = Substitute.For<ISerializer>();
        mockInner.ToJson(Arg.Any<PiiEvent>()).Returns("{}");

        var error = CryptoShreddingErrors.SubjectForgotten(SubjectId);
        _mockKeyProvider.GetOrCreateSubjectKeyAsync(SubjectId, Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, byte[]>(error));

        var (logger, capture) = CreateCapture<CryptoShredderSerializer>();
        using (capture)
        {
            var sut = new CryptoShredderSerializer(mockInner, _mockKeyProvider, _mockForgottenHandler, logger);
            var evt = new PiiEvent { UserId = SubjectId, Email = "test@example.com" };

            DiagnosticsCapture.Capture(() => sut.ToJson(evt));
        }

        capture.AssertNoSubjectId(logger, SubjectId, expectActivity: true);
    }

    [Fact]
    public void ToJson_NullSubjectIdValue_NeverCarriesSubjectId()
    {
        var mockInner = Substitute.For<ISerializer>();
        mockInner.ToJson(Arg.Any<NullableSubjectEvent>()).Returns("{}");

        var (logger, capture) = CreateCapture<CryptoShredderSerializer>();
        using (capture)
        {
            var sut = new CryptoShredderSerializer(mockInner, _mockKeyProvider, _mockForgottenHandler, logger);
            var evt = new NullableSubjectEvent { UserId = null, Email = "test@example.com" };

            DiagnosticsCapture.Capture(() => sut.ToJson(evt));
        }

        capture.AssertNoSubjectId(logger, SubjectId, expectActivity: true);
    }

    [Fact]
    public void WriteTo_EncryptsField_NeverCarriesSubjectId()
    {
        var mockInner = Substitute.For<ISerializer>();
        var keyMaterial = new byte[32];
        Random.Shared.NextBytes(keyMaterial);
        _mockKeyProvider.GetOrCreateSubjectKeyAsync(SubjectId, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, byte[]>(keyMaterial));
        _mockKeyProvider.GetSubjectInfoAsync(SubjectId, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, SubjectEncryptionInfo>(new SubjectEncryptionInfo
            {
                SubjectId = SubjectId,
                Status = SubjectStatus.Active,
                ActiveKeyVersion = 1,
                TotalKeyVersions = 1,
                CreatedAtUtc = DateTimeOffset.UtcNow
            }));

        var (logger, capture) = CreateCapture<CryptoShredderSerializer>();
        using (capture)
        {
            var sut = new CryptoShredderSerializer(mockInner, _mockKeyProvider, _mockForgottenHandler, logger);
            var evt = new PiiEvent { UserId = SubjectId, Email = "test@example.com" };
            var writer = new ArrayBufferWriter<byte>();

            DiagnosticsCapture.Capture(() => sut.WriteTo(writer, evt));
        }

        capture.AssertNoSubjectId(logger, SubjectId, expectActivity: true);
    }

    [Fact]
    public void FromJson_DecryptsField_NeverCarriesSubjectId()
    {
        var mockInner = Substitute.For<ISerializer>();
        var keyMaterial = new byte[32];
        Random.Shared.NextBytes(keyMaterial);
        _mockKeyProvider.GetOrCreateSubjectKeyAsync(SubjectId, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, byte[]>(keyMaterial));
        _mockKeyProvider.GetSubjectInfoAsync(SubjectId, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, SubjectEncryptionInfo>(new SubjectEncryptionInfo
            {
                SubjectId = SubjectId,
                Status = SubjectStatus.Active,
                ActiveKeyVersion = 1,
                TotalKeyVersions = 1,
                CreatedAtUtc = DateTimeOffset.UtcNow
            }));
        _mockKeyProvider.GetSubjectKeyAsync(SubjectId, 1, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, byte[]>(keyMaterial));

        var (logger, capture) = CreateCapture<CryptoShredderSerializer>();
        using (capture)
        {
            var sut = new CryptoShredderSerializer(mockInner, _mockKeyProvider, _mockForgottenHandler, logger);

            // Encrypt first, capturing the ciphertext the same way production code produces it.
            string? encryptedEmail = null;
            mockInner.ToJson(Arg.Any<PiiEvent>()).Returns(ci =>
            {
                encryptedEmail = ci.Arg<PiiEvent>().Email;
                return "{}";
            });
            var plaintextEvt = new PiiEvent { UserId = SubjectId, Email = "test@example.com" };
            DiagnosticsCapture.Capture(() => sut.ToJson(plaintextEvt));
            encryptedEmail.ShouldStartWith("{\"__enc\":true");

            // Decrypt: the inner deserializer hands back the encrypted envelope, as it would
            // after reading it back from storage.
            mockInner.FromJson<PiiEvent>(Arg.Any<Stream>())
                .Returns(new PiiEvent { UserId = SubjectId, Email = encryptedEmail! });

            var decrypted = DiagnosticsCapture.Capture(() => sut.FromJson<PiiEvent>(Stream.Null));
            decrypted.Email.ShouldBe("test@example.com");
        }

        capture.AssertNoSubjectId(logger, SubjectId, expectActivity: true);
    }

    [Fact]
    public async Task FromJsonAsync_DecryptsField_NeverCarriesSubjectId()
    {
        var mockInner = Substitute.For<ISerializer>();
        var keyMaterial = new byte[32];
        Random.Shared.NextBytes(keyMaterial);
        _mockKeyProvider.GetOrCreateSubjectKeyAsync(SubjectId, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, byte[]>(keyMaterial));
        _mockKeyProvider.GetSubjectInfoAsync(SubjectId, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, SubjectEncryptionInfo>(new SubjectEncryptionInfo
            {
                SubjectId = SubjectId,
                Status = SubjectStatus.Active,
                ActiveKeyVersion = 1,
                TotalKeyVersions = 1,
                CreatedAtUtc = DateTimeOffset.UtcNow
            }));
        _mockKeyProvider.GetSubjectKeyAsync(SubjectId, 1, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, byte[]>(keyMaterial));

        var (logger, capture) = CreateCapture<CryptoShredderSerializer>();
        using (capture)
        {
            var sut = new CryptoShredderSerializer(mockInner, _mockKeyProvider, _mockForgottenHandler, logger);

            string? encryptedEmail = null;
            mockInner.ToJson(Arg.Any<PiiEvent>()).Returns(ci =>
            {
                encryptedEmail = ci.Arg<PiiEvent>().Email;
                return "{}";
            });
            var plaintextEvt = new PiiEvent { UserId = SubjectId, Email = "test@example.com" };
            DiagnosticsCapture.Capture(() => sut.ToJson(plaintextEvt));

            mockInner.FromJsonAsync<PiiEvent>(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
                .Returns(ValueTask.FromResult(new PiiEvent { UserId = SubjectId, Email = encryptedEmail! }));

            var decrypted = await DiagnosticsCapture.CaptureAsync(
                () => sut.FromJsonAsync<PiiEvent>(Stream.Null).AsTask());
            decrypted.Email.ShouldBe("test@example.com");
        }

        capture.AssertNoSubjectId(logger, SubjectId, expectActivity: true);
    }

    [Fact]
    public void FromJson_ForgottenSubject_NeverCarriesSubjectId()
    {
        var mockInner = Substitute.For<ISerializer>();
        var keyMaterial = new byte[32];
        Random.Shared.NextBytes(keyMaterial);
        _mockKeyProvider.GetOrCreateSubjectKeyAsync(SubjectId, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, byte[]>(keyMaterial));
        _mockKeyProvider.GetSubjectInfoAsync(SubjectId, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, SubjectEncryptionInfo>(new SubjectEncryptionInfo
            {
                SubjectId = SubjectId,
                Status = SubjectStatus.Active,
                ActiveKeyVersion = 1,
                TotalKeyVersions = 1,
                CreatedAtUtc = DateTimeOffset.UtcNow
            }));
        _mockKeyProvider.GetSubjectKeyAsync(SubjectId, 1, Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, byte[]>(CryptoShreddingErrors.SubjectForgotten(SubjectId)));

        var (logger, capture) = CreateCapture<CryptoShredderSerializer>();
        using (capture)
        {
            var sut = new CryptoShredderSerializer(mockInner, _mockKeyProvider, _mockForgottenHandler, logger, "[REDACTED]");

            string? encryptedEmail = null;
            mockInner.ToJson(Arg.Any<PiiEvent>()).Returns(ci =>
            {
                encryptedEmail = ci.Arg<PiiEvent>().Email;
                return "{}";
            });
            var plaintextEvt = new PiiEvent { UserId = SubjectId, Email = "test@example.com" };
            DiagnosticsCapture.Capture(() => sut.ToJson(plaintextEvt));

            mockInner.FromJson<PiiEvent>(Arg.Any<Stream>())
                .Returns(new PiiEvent { UserId = SubjectId, Email = encryptedEmail! });

            var decrypted = DiagnosticsCapture.Capture(() => sut.FromJson<PiiEvent>(Stream.Null));
            decrypted.Email.ShouldBe("[REDACTED]");
        }

        capture.AssertNoSubjectId(logger, SubjectId, expectActivity: true);
    }

    #endregion

    #region InMemorySubjectKeyProvider

    [Fact]
    public void GetOrCreateSubjectKeyAsync_CreatesKey_NeverLogsSubjectId()
    {
        var (logger, capture) = CreateCapture<InMemorySubjectKeyProvider>();
        using (capture)
        {
            var sut = new InMemorySubjectKeyProvider(new FakeTimeProvider(), logger);

            var result = DiagnosticsCapture.Capture(() => sut.GetOrCreateSubjectKeyAsync(SubjectId).AsTask().GetAwaiter().GetResult());
            result.IsRight.ShouldBeTrue();
        }

        capture.AssertNoSubjectId(logger, SubjectId, expectActivity: false);
    }

    [Fact]
    public void DeleteSubjectKeysAsync_NeverCarriesSubjectId()
    {
        var (logger, capture) = CreateCapture<InMemorySubjectKeyProvider>();
        using (capture)
        {
            var sut = new InMemorySubjectKeyProvider(new FakeTimeProvider(), logger);
            DiagnosticsCapture.Capture(() => sut.GetOrCreateSubjectKeyAsync(SubjectId).AsTask().GetAwaiter().GetResult());

            var result = DiagnosticsCapture.Capture(() => sut.DeleteSubjectKeysAsync(SubjectId).AsTask().GetAwaiter().GetResult());
            result.IsRight.ShouldBeTrue();
        }

        capture.AssertNoSubjectId(logger, SubjectId, expectActivity: true);
    }

    [Fact]
    public void RotateSubjectKeyAsync_NeverCarriesSubjectId()
    {
        var (logger, capture) = CreateCapture<InMemorySubjectKeyProvider>();
        using (capture)
        {
            var sut = new InMemorySubjectKeyProvider(new FakeTimeProvider(), logger);
            DiagnosticsCapture.Capture(() => sut.GetOrCreateSubjectKeyAsync(SubjectId).AsTask().GetAwaiter().GetResult());

            var result = DiagnosticsCapture.Capture(() => sut.RotateSubjectKeyAsync(SubjectId).AsTask().GetAwaiter().GetResult());
            result.IsRight.ShouldBeTrue();
        }

        capture.AssertNoSubjectId(logger, SubjectId, expectActivity: true);
    }

    #endregion

    #region CryptoShredErasureStrategy

    [Fact]
    public async Task EraseFieldAsync_NeverCarriesSubjectId()
    {
        var location = new PersonalDataLocation
        {
            EntityType = typeof(string),
            EntityId = SubjectId,
            FieldName = "Email",
            Category = PersonalDataCategory.Contact,
            IsErasable = true,
            IsPortable = false,
            HasLegalRetention = false
        };

        _mockKeyProvider.DeleteSubjectKeysAsync(SubjectId, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, CryptoShreddingResult>(new CryptoShreddingResult
            {
                SubjectId = SubjectId,
                KeysDeleted = 1,
                FieldsAffected = 0,
                ShreddedAtUtc = DateTimeOffset.UtcNow
            }));

        var (logger, capture) = CreateCapture<CryptoShredErasureStrategy>();
        using (capture)
        {
            var sut = new CryptoShredErasureStrategy(_mockKeyProvider, logger);

            var result = await DiagnosticsCapture.CaptureAsync(() => sut.EraseFieldAsync(location).AsTask());
            result.IsRight.ShouldBeTrue();
        }

        capture.AssertNoSubjectId(logger, SubjectId, expectActivity: true);
    }

    #endregion

    private static (FakeLogger<T> Logger, DiagnosticsCapture Capture) CreateCapture<T>() =>
        (new FakeLogger<T>(), new DiagnosticsCapture(CryptoShreddingDiagnostics.ActivitySource, CryptoShreddingDiagnostics.Meter));

    // Test types

    public class PiiEvent
    {
        public string UserId { get; set; } = string.Empty;

        [PersonalData(Category = PersonalDataCategory.Contact, Erasable = true)]
        [CryptoShredded(SubjectIdProperty = nameof(UserId))]
        public string Email { get; set; } = string.Empty;
    }

    /// <summary>
    /// A crypto-shredded field whose subject id property is a valid, cached string property but
    /// holds a <c>null</c> value at runtime, so <c>GetSubjectId</c> resolves <c>null</c> and
    /// encryption is skipped with only a warning naming the field — never a subject id (#1429).
    /// </summary>
    public class NullableSubjectEvent
    {
        public string? UserId { get; set; }

        [PersonalData(Category = PersonalDataCategory.Contact, Erasable = true)]
        [CryptoShredded(SubjectIdProperty = nameof(UserId))]
        public string Email { get; set; } = string.Empty;
    }

    /// <summary>
    /// Captures every log record, activity and metric tag recorded on a given
    /// <see cref="ActivitySource"/>/<see cref="Meter"/> pair, but only for the calls made through
    /// <see cref="Capture{T}"/>/<see cref="CaptureAsync{T}"/> — an <see cref="AsyncLocal{T}"/> flag
    /// scopes capture to this test's own logical call, so a process-global listener never picks up
    /// another test's concurrently running activity on the same shared source (#1429; see #1423,
    /// where a catch-all listener without this scoping made a Messaging test flaky).
    /// </summary>
    private sealed class DiagnosticsCapture : IDisposable
    {
        private static readonly AsyncLocal<bool> Capturing = new();

        private readonly ActivityListener _activityListener;
        private readonly MeterListener _meterListener;
        private readonly Lock _sync = new();
        private readonly List<Activity> _activities = [];
        private readonly List<object?> _tagValues = [];

        public DiagnosticsCapture(ActivitySource activitySource, Meter meter)
        {
            _activityListener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == activitySource.Name,
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
                        foreach (var tag in activity.Tags)
                        {
                            _tagValues.Add(tag.Value);
                        }
                    }
                }
            };
            ActivitySource.AddActivityListener(_activityListener);

            _meterListener = new MeterListener
            {
                InstrumentPublished = (instrument, listener) =>
                {
                    if (instrument.Meter.Name == meter.Name)
                    {
                        listener.EnableMeasurementEvents(instrument);
                    }
                }
            };
            _meterListener.SetMeasurementEventCallback<long>((_, _, tags, _) => CaptureTags(tags));
            _meterListener.SetMeasurementEventCallback<double>((_, _, tags, _) => CaptureTags(tags));
            _meterListener.Start();
        }

        private void CaptureTags(ReadOnlySpan<KeyValuePair<string, object?>> tags)
        {
            if (!Capturing.Value)
            {
                return;
            }

            lock (_sync)
            {
                foreach (var tag in tags)
                {
                    _tagValues.Add(tag.Value);
                }
            }
        }

        public static void Capture(Action action)
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

        public static T Capture<T>(Func<T> action)
        {
            Capturing.Value = true;
            try
            {
                return action();
            }
            finally
            {
                Capturing.Value = false;
            }
        }

        public static async Task<T> CaptureAsync<T>(Func<Task<T>> action)
        {
            Capturing.Value = true;
            try
            {
                return await action().ConfigureAwait(false);
            }
            finally
            {
                Capturing.Value = false;
            }
        }

        public void AssertNoSubjectId<T>(FakeLogger<T> logger, string subjectId, bool expectActivity)
        {
            var logs = logger.Collector.GetSnapshot();
            logs.ShouldAllBe(r => !r.Message.Contains(subjectId, StringComparison.Ordinal));

            List<Activity> activitiesSnapshot;
            List<object?> tagValuesSnapshot;
            lock (_sync)
            {
                activitiesSnapshot = [.. _activities];
                tagValuesSnapshot = [.. _tagValues];
            }

            if (expectActivity)
            {
                activitiesSnapshot.ShouldNotBeEmpty();
            }

            foreach (var activity in activitiesSnapshot)
            {
                activity.DisplayName.ShouldNotContain(subjectId);
                (activity.StatusDescription ?? string.Empty).ShouldNotContain(subjectId);
            }

            foreach (var tagValue in tagValuesSnapshot)
            {
                if (tagValue is string tagString)
                {
                    tagString.ShouldNotContain(subjectId);
                }
            }
        }

        public void Dispose()
        {
            _activityListener.Dispose();
            _meterListener.Dispose();
        }
    }
}
#pragma warning restore CA2012
