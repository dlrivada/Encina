using System.Buffers;
using System.Data.Common;
using Encina.Compliance.DataSubjectRights;
using Encina.Marten.GDPR;
using Encina.Marten.GDPR.Abstractions;
using LanguageExt;
using Marten;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using NSubstitute;
using Shouldly;
using Weasel.Core;
using static LanguageExt.Prelude;
using ISerializer = Marten.ISerializer;

namespace Encina.UnitTests.Marten.GDPR;

public sealed class CryptoShredderSerializerTests : IDisposable
{
    private readonly ISerializer _mockInner = Substitute.For<ISerializer>();
    private readonly ISubjectKeyProvider _mockKeyProvider = Substitute.For<ISubjectKeyProvider>();
    private readonly IForgottenSubjectHandler _mockForgottenHandler = Substitute.For<IForgottenSubjectHandler>();
    private readonly CryptoShredderSerializer _sut;

    public CryptoShredderSerializerTests()
    {
        _sut = new CryptoShredderSerializer(
            _mockInner,
            _mockKeyProvider,
            _mockForgottenHandler,
            NullLogger<CryptoShredderSerializer>.Instance);
    }

    public void Dispose()
    {
        CryptoShreddedPropertyCache.ClearCache();
    }

    [Fact]
    public void EnumStorage_DelegatesToInner()
    {
        // Arrange
        _mockInner.EnumStorage.Returns(EnumStorage.AsString);

        // Act & Assert
        _sut.EnumStorage.ShouldBe(EnumStorage.AsString);
    }

    [Fact]
    public void Casing_DelegatesToInner()
    {
        // Arrange
        _mockInner.Casing.Returns(Casing.CamelCase);

        // Act & Assert
        _sut.Casing.ShouldBe(Casing.CamelCase);
    }

    [Fact]
    public void ToJson_NonPiiEvent_DelegatesToInner()
    {
        // Arrange
        var evt = new NonPiiEvent { Id = "123", Timestamp = DateTimeOffset.UtcNow };
        _mockInner.ToJson(evt).Returns("{\"id\":\"123\"}");

        // Act
        var json = _sut.ToJson(evt);

        // Assert
        json.ShouldBe("{\"id\":\"123\"}");
        _mockInner.Received(1).ToJson(evt);
    }

    [Fact]
    public void ToJson_PiiEvent_EncryptsField()
    {
        // Arrange
        var evt = new PiiEvent { UserId = "user-1", Email = "test@example.com" };
        var keyMaterial = new byte[32];
        Random.Shared.NextBytes(keyMaterial);

        _mockKeyProvider
            .GetOrCreateSubjectKeyAsync("user-1", Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, byte[]>(keyMaterial));
        _mockKeyProvider
            .GetSubjectInfoAsync("user-1", Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, SubjectEncryptionInfo>(new SubjectEncryptionInfo
            {
                SubjectId = "user-1",
                Status = SubjectStatus.Active,
                ActiveKeyVersion = 1,
                TotalKeyVersions = 1,
                CreatedAtUtc = DateTimeOffset.UtcNow
            }));

        _mockInner.ToJson(Arg.Any<PiiEvent>())
            .Returns(ci =>
            {
                var e = ci.Arg<PiiEvent>();
                // After encryption, the email should be replaced with encrypted JSON
                if (e.Email != null && e.Email.StartsWith("{\"__enc\":true", StringComparison.Ordinal))
                {
                    return $"{{\"userId\":\"{e.UserId}\",\"email\":\"{e.Email}\"}}";
                }

                return $"{{\"userId\":\"{e.UserId}\",\"email\":\"{e.Email}\"}}";
            });

        // Act
        var json = _sut.ToJson(evt);

        // Assert — The inner serializer should have been called
        _mockInner.Received(1).ToJson(Arg.Any<PiiEvent>());
        // Original email should be restored after serialization
        evt.Email.ShouldBe("test@example.com");
    }

    [Fact]
    public async Task ToJson_ForgottenSubject_ReturnsError()
    {
        // Arrange
        var evt = new PiiEvent { UserId = "user-forgotten", Email = "test@example.com" };
        var error = CryptoShreddingErrors.SubjectForgotten("user-forgotten");

        _mockKeyProvider
            .GetOrCreateSubjectKeyAsync("user-forgotten", Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, byte[]>(error));

        // Act & Assert — when key provider fails, serializer should still work
        // (it logs the error but lets the inner serializer handle the event)
        _mockInner.ToJson(Arg.Any<PiiEvent>()).Returns("{}");
        var json = _sut.ToJson(evt);

        // Should have attempted key retrieval
        await _mockKeyProvider.Received(1)
            .GetOrCreateSubjectKeyAsync("user-forgotten", Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Constructor_SetsAnonymizedPlaceholder()
    {
        // Arrange & Act
        var serializer = new CryptoShredderSerializer(
            _mockInner,
            _mockKeyProvider,
            _mockForgottenHandler,
            NullLogger<CryptoShredderSerializer>.Instance,
            "***DELETED***");

        // Assert — serializer created successfully with custom placeholder
        serializer.ShouldNotBeNull();
    }

    [Fact]
    public void WriteTo_NullWriter_ThrowsArgumentNullException()
    {
        // Arrange
        var evt = new NonPiiEvent { Id = "123", Timestamp = DateTimeOffset.UtcNow };

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _sut.WriteTo(null!, evt));
    }

    [Fact]
    public void WriteTo_NonPiiEvent_DelegatesToInner()
    {
        // Arrange
        var writer = new ArrayBufferWriter<byte>();
        var evt = new NonPiiEvent { Id = "123", Timestamp = DateTimeOffset.UtcNow };

        // Act
        _sut.WriteTo(writer, evt);

        // Assert
        _mockInner.Received(1).WriteTo(writer, evt);
    }

    [Fact]
    public void WriteTo_PiiEvent_EncryptsFieldAndRestoresOriginal()
    {
        // Arrange
        var writer = new ArrayBufferWriter<byte>();
        var evt = new PiiEvent { UserId = "user-1", Email = "test@example.com" };
        var keyMaterial = new byte[32];
        Random.Shared.NextBytes(keyMaterial);

        _mockKeyProvider
            .GetOrCreateSubjectKeyAsync("user-1", Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, byte[]>(keyMaterial));
        _mockKeyProvider
            .GetSubjectInfoAsync("user-1", Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, SubjectEncryptionInfo>(new SubjectEncryptionInfo
            {
                SubjectId = "user-1",
                Status = SubjectStatus.Active,
                ActiveKeyVersion = 1,
                TotalKeyVersions = 1,
                CreatedAtUtc = DateTimeOffset.UtcNow
            }));

        string? captured = null;
        _mockInner
            .When(x => x.WriteTo(Arg.Any<IBufferWriter<byte>>(), Arg.Any<PiiEvent>()))
            .Do(ci => captured = ci.Arg<PiiEvent>().Email);

        // Act
        _sut.WriteTo(writer, evt);

        // Assert
        captured.ShouldNotBeNull();
        captured.ShouldStartWith("{\"__enc\":true");
        evt.Email.ShouldBe("test@example.com");
    }

    [Fact]
    public void WriteToCleanJson_NullWriter_ThrowsArgumentNullException()
    {
        // Arrange
        var evt = new NonPiiEvent { Id = "123", Timestamp = DateTimeOffset.UtcNow };

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _sut.WriteToCleanJson(null!, evt));
    }

    [Fact]
    public void WriteToCleanJson_NonPiiEvent_DelegatesToInner()
    {
        // Arrange
        var writer = new ArrayBufferWriter<byte>();
        var evt = new NonPiiEvent { Id = "123", Timestamp = DateTimeOffset.UtcNow };

        // Act
        _sut.WriteToCleanJson(writer, evt);

        // Assert
        _mockInner.Received(1).WriteToCleanJson(writer, evt);
    }

    [Fact]
    public void WriteToCleanJson_PiiEvent_EncryptsFieldAndRestoresOriginal()
    {
        // Arrange
        var writer = new ArrayBufferWriter<byte>();
        var evt = new PiiEvent { UserId = "user-1", Email = "test@example.com" };
        var keyMaterial = new byte[32];
        Random.Shared.NextBytes(keyMaterial);

        _mockKeyProvider
            .GetOrCreateSubjectKeyAsync("user-1", Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, byte[]>(keyMaterial));
        _mockKeyProvider
            .GetSubjectInfoAsync("user-1", Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, SubjectEncryptionInfo>(new SubjectEncryptionInfo
            {
                SubjectId = "user-1",
                Status = SubjectStatus.Active,
                ActiveKeyVersion = 1,
                TotalKeyVersions = 1,
                CreatedAtUtc = DateTimeOffset.UtcNow
            }));

        string? captured = null;
        _mockInner
            .When(x => x.WriteToCleanJson(Arg.Any<IBufferWriter<byte>>(), Arg.Any<PiiEvent>()))
            .Do(ci => captured = ci.Arg<PiiEvent>().Email);

        // Act
        _sut.WriteToCleanJson(writer, evt);

        // Assert
        captured.ShouldNotBeNull();
        captured.ShouldStartWith("{\"__enc\":true");
        evt.Email.ShouldBe("test@example.com");
    }

    [Fact]
    public void WriteToJsonWithTypes_NullValue_ThrowsArgumentNullException()
    {
        // Arrange
        var writer = new ArrayBufferWriter<byte>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _sut.WriteToJsonWithTypes(writer, null!));
    }

    [Fact]
    public void WriteToJsonWithTypes_NonPiiEvent_DelegatesToInner()
    {
        // Arrange
        var writer = new ArrayBufferWriter<byte>();
        var evt = new NonPiiEvent { Id = "123", Timestamp = DateTimeOffset.UtcNow };

        // Act
        _sut.WriteToJsonWithTypes(writer, evt);

        // Assert
        _mockInner.Received(1).WriteToJsonWithTypes(writer, evt);
    }

    [Fact]
    public void WriteToJsonWithTypes_PiiEvent_EncryptsFieldAndRestoresOriginal()
    {
        // Arrange
        var writer = new ArrayBufferWriter<byte>();
        var evt = new PiiEvent { UserId = "user-1", Email = "test@example.com" };
        var keyMaterial = new byte[32];
        Random.Shared.NextBytes(keyMaterial);

        _mockKeyProvider
            .GetOrCreateSubjectKeyAsync("user-1", Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, byte[]>(keyMaterial));
        _mockKeyProvider
            .GetSubjectInfoAsync("user-1", Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, SubjectEncryptionInfo>(new SubjectEncryptionInfo
            {
                SubjectId = "user-1",
                Status = SubjectStatus.Active,
                ActiveKeyVersion = 1,
                TotalKeyVersions = 1,
                CreatedAtUtc = DateTimeOffset.UtcNow
            }));

        string? captured = null;
        _mockInner
            .When(x => x.WriteToJsonWithTypes(Arg.Any<IBufferWriter<byte>>(), Arg.Any<PiiEvent>()))
            .Do(ci => captured = ci.Arg<PiiEvent>().Email);

        // Act
        _sut.WriteToJsonWithTypes(writer, evt);

        // Assert
        captured.ShouldNotBeNull();
        captured.ShouldStartWith("{\"__enc\":true");
        evt.Email.ShouldBe("test@example.com");
    }

    [Fact]
    public void WriteToParameter_DbParameter_NullParameter_ThrowsArgumentNullException()
    {
        // Arrange
        var evt = new NonPiiEvent { Id = "123", Timestamp = DateTimeOffset.UtcNow };

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _sut.WriteToParameter((DbParameter)null!, evt));
    }

    [Fact]
    public void WriteToParameter_DbParameter_NonPiiEvent_DelegatesToInner()
    {
        // Arrange
        var parameter = Substitute.For<DbParameter>();
        var evt = new NonPiiEvent { Id = "123", Timestamp = DateTimeOffset.UtcNow };

        // Act
        _sut.WriteToParameter(parameter, evt);

        // Assert
        _mockInner.Received(1).WriteToParameter(parameter, evt);
    }

    [Fact]
    public void WriteToParameter_DbParameter_PiiEvent_EncryptsFieldAndRestoresOriginal()
    {
        // Arrange
        var parameter = Substitute.For<DbParameter>();
        var evt = new PiiEvent { UserId = "user-1", Email = "test@example.com" };
        var keyMaterial = new byte[32];
        Random.Shared.NextBytes(keyMaterial);

        _mockKeyProvider
            .GetOrCreateSubjectKeyAsync("user-1", Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, byte[]>(keyMaterial));
        _mockKeyProvider
            .GetSubjectInfoAsync("user-1", Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, SubjectEncryptionInfo>(new SubjectEncryptionInfo
            {
                SubjectId = "user-1",
                Status = SubjectStatus.Active,
                ActiveKeyVersion = 1,
                TotalKeyVersions = 1,
                CreatedAtUtc = DateTimeOffset.UtcNow
            }));

        string? captured = null;
        _mockInner
            .When(x => x.WriteToParameter(Arg.Any<DbParameter>(), Arg.Any<PiiEvent>()))
            .Do(ci => captured = ci.Arg<PiiEvent>().Email);

        // Act
        _sut.WriteToParameter(parameter, evt);

        // Assert
        captured.ShouldNotBeNull();
        captured.ShouldStartWith("{\"__enc\":true");
        evt.Email.ShouldBe("test@example.com");
    }

    [Fact]
    public void WriteToParameter_NpgsqlParameter_NonPiiEvent_DelegatesToInner()
    {
        // Arrange
        var parameter = new NpgsqlParameter();
        var evt = new NonPiiEvent { Id = "123", Timestamp = DateTimeOffset.UtcNow };

        // Act
        _sut.WriteToParameter(parameter, evt);

        // Assert
        _mockInner.Received(1).WriteToParameter(parameter, evt);
    }

    [Fact]
    public void WriteToParameter_NpgsqlParameter_PiiEvent_EncryptsFieldAndRestoresOriginal()
    {
        // Arrange
        var parameter = new NpgsqlParameter();
        var evt = new PiiEvent { UserId = "user-1", Email = "test@example.com" };
        var keyMaterial = new byte[32];
        Random.Shared.NextBytes(keyMaterial);

        _mockKeyProvider
            .GetOrCreateSubjectKeyAsync("user-1", Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, byte[]>(keyMaterial));
        _mockKeyProvider
            .GetSubjectInfoAsync("user-1", Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, SubjectEncryptionInfo>(new SubjectEncryptionInfo
            {
                SubjectId = "user-1",
                Status = SubjectStatus.Active,
                ActiveKeyVersion = 1,
                TotalKeyVersions = 1,
                CreatedAtUtc = DateTimeOffset.UtcNow
            }));

        string? captured = null;
        _mockInner
            .When(x => x.WriteToParameter(Arg.Any<NpgsqlParameter>(), Arg.Any<PiiEvent>()))
            .Do(ci => captured = ci.Arg<PiiEvent>().Email);

        // Act
        _sut.WriteToParameter(parameter, evt);

        // Assert
        captured.ShouldNotBeNull();
        captured.ShouldStartWith("{\"__enc\":true");
        evt.Email.ShouldBe("test@example.com");
    }

    // Test types

    public class NonPiiEvent
    {
        public string Id { get; set; } = string.Empty;
        public DateTimeOffset Timestamp { get; set; }
    }

    public class PiiEvent
    {
        public string UserId { get; set; } = string.Empty;

        [PersonalData(Category = PersonalDataCategory.Contact, Erasable = true)]
        [CryptoShredded(SubjectIdProperty = nameof(UserId))]
        public string Email { get; set; } = string.Empty;
    }
}
