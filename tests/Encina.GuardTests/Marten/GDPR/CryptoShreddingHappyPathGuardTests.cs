using Encina.Compliance.DataSubjectRights;
using Encina.Marten.GDPR;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

using Shouldly;

namespace Encina.GuardTests.Marten.GDPR;

/// <summary>
/// Guard tests exercising the public surface and happy paths of Encina.Marten.GDPR so
/// that the guard flag observes real line execution across Model/Event POCOs, Options,
/// Errors, DefaultForgottenSubjectHandler and InMemorySubjectKeyProvider.
/// </summary>
[Trait("Category", "Guard")]
[Trait("Provider", "Marten")]
public sealed class CryptoShreddingHappyPathGuardTests : IDisposable
{
    private readonly FakeTimeProvider _time;
    private readonly InMemorySubjectKeyProvider _keyProvider;

    public CryptoShreddingHappyPathGuardTests()
    {
        _time = new FakeTimeProvider(new DateTimeOffset(2026, 3, 15, 12, 0, 0, TimeSpan.Zero));
        _keyProvider = new InMemorySubjectKeyProvider(_time, NullLogger<InMemorySubjectKeyProvider>.Instance);
    }

    public void Dispose()
    {
        _keyProvider.Clear();
    }

    // ─── CryptoShreddingOptions (public) ───

    [Fact]
    public void CryptoShreddingOptions_Defaults_AreSet()
    {
        var options = new CryptoShreddingOptions();

        options.AnonymizedPlaceholder.ShouldBe("[REDACTED]");
        options.AutoRegisterFromAttributes.ShouldBeTrue();
        options.AddHealthCheck.ShouldBeFalse();
        options.PublishEvents.ShouldBeTrue();
        options.KeyRotationDays.ShouldBe(90);
        options.UsePostgreSqlKeyStore.ShouldBeFalse();
        options.AssembliesToScan.ShouldBeEmpty();
    }

    [Fact]
    public void CryptoShreddingOptions_AllProperties_Settable()
    {
        var options = new CryptoShreddingOptions
        {
            AnonymizedPlaceholder = "<GONE>",
            AutoRegisterFromAttributes = false,
            AddHealthCheck = true,
            PublishEvents = false,
            KeyRotationDays = 180,
            UsePostgreSqlKeyStore = true
        };
        options.AssembliesToScan.Add(typeof(CryptoShreddingHappyPathGuardTests).Assembly);

        options.AnonymizedPlaceholder.ShouldBe("<GONE>");
        options.AutoRegisterFromAttributes.ShouldBeFalse();
        options.AddHealthCheck.ShouldBeTrue();
        options.PublishEvents.ShouldBeFalse();
        options.KeyRotationDays.ShouldBe(180);
        options.UsePostgreSqlKeyStore.ShouldBeTrue();
        options.AssembliesToScan.Count.ShouldBe(1);
    }

    // ─── InMemorySubjectKeyProvider happy paths ───

    [Fact]
    public async Task InMemoryProvider_GetOrCreate_ReturnsKeyOfCorrectLength()
    {
        var result = await _keyProvider.GetOrCreateSubjectKeyAsync("user-1");

        result.IsRight.ShouldBeTrue();
        byte[] key = null!;
        result.IfRight(k => key = k);
        key.Length.ShouldBe(32);
    }

    [Fact]
    public async Task InMemoryProvider_GetOrCreate_Twice_ReturnsSameKey()
    {
        var first = await _keyProvider.GetOrCreateSubjectKeyAsync("user-2");
        var second = await _keyProvider.GetOrCreateSubjectKeyAsync("user-2");

        byte[] k1 = null!;
        byte[] k2 = null!;
        first.IfRight(k => k1 = k);
        second.IfRight(k => k2 = k);
        k1.SequenceEqual(k2).ShouldBeTrue();
    }

    [Fact]
    public async Task InMemoryProvider_GetKey_AfterCreate_ReturnsKey()
    {
        await _keyProvider.GetOrCreateSubjectKeyAsync("user-3");
        var result = await _keyProvider.GetSubjectKeyAsync("user-3");
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task InMemoryProvider_GetKey_Unknown_ReturnsLeft()
    {
        var result = await _keyProvider.GetSubjectKeyAsync("unknown-user");
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task InMemoryProvider_Rotate_ProducesNewVersion()
    {
        await _keyProvider.GetOrCreateSubjectKeyAsync("user-4");
        var rotate = await _keyProvider.RotateSubjectKeyAsync("user-4");

        rotate.IsRight.ShouldBeTrue();
        int newVersion = 0;
        rotate.IfRight(r => newVersion = r.NewVersion);
        newVersion.ShouldBe(2);
    }

    [Fact]
    public async Task InMemoryProvider_Delete_MakesSubjectForgotten()
    {
        await _keyProvider.GetOrCreateSubjectKeyAsync("user-5");
        var delete = await _keyProvider.DeleteSubjectKeysAsync("user-5");
        delete.IsRight.ShouldBeTrue();

        var isForgotten = await _keyProvider.IsSubjectForgottenAsync("user-5");
        isForgotten.IsRight.ShouldBeTrue();
        bool forgotten = false;
        isForgotten.IfRight(f => forgotten = f);
        forgotten.ShouldBeTrue();
    }

    [Fact]
    public async Task InMemoryProvider_IsForgotten_UnknownSubject_ReturnsFalse()
    {
        var result = await _keyProvider.IsSubjectForgottenAsync("never-seen");
        result.IsRight.ShouldBeTrue();
        bool forgotten = true;
        result.IfRight(f => forgotten = f);
        forgotten.ShouldBeFalse();
    }

    [Fact]
    public async Task InMemoryProvider_GetSubjectInfo_ActiveSubject_ReturnsInfo()
    {
        await _keyProvider.GetOrCreateSubjectKeyAsync("user-6");
        var info = await _keyProvider.GetSubjectInfoAsync("user-6");

        info.IsRight.ShouldBeTrue();
        info.IfRight(i =>
        {
            i.SubjectId.ShouldBe("user-6");
            i.ActiveKeyVersion.ShouldBe(1);
            i.Status.ShouldBe(SubjectStatus.Active);
        });
    }

    [Fact]
    public async Task InMemoryProvider_GetSubjectInfo_ForgottenSubject_ReturnsForgottenStatus()
    {
        await _keyProvider.GetOrCreateSubjectKeyAsync("user-7");
        await _keyProvider.DeleteSubjectKeysAsync("user-7");

        var info = await _keyProvider.GetSubjectInfoAsync("user-7");
        info.IsRight.ShouldBeTrue();
        info.IfRight(i => i.Status.ShouldBe(SubjectStatus.Forgotten));
    }

    [Fact]
    public async Task InMemoryProvider_ForgottenSubject_CannotRecreate()
    {
        await _keyProvider.GetOrCreateSubjectKeyAsync("user-f");
        await _keyProvider.DeleteSubjectKeysAsync("user-f");

        var recreate = await _keyProvider.GetOrCreateSubjectKeyAsync("user-f");
        recreate.IsLeft.ShouldBeTrue();
    }

    // ─── DefaultForgottenSubjectHandler ───

    [Fact]
    public async Task DefaultForgottenSubjectHandler_HandleForgotten_ReturnsCompletedTask()
    {
        var handler = new DefaultForgottenSubjectHandler(NullLogger<DefaultForgottenSubjectHandler>.Instance);

        await handler.HandleForgottenSubjectAsync("user-8", "Email", typeof(string));
        // No exception = pass
    }

    [Fact]
    public void DefaultForgottenSubjectHandler_Constructor_NullLogger_Throws()
    {
        Should.Throw<ArgumentNullException>(() => new DefaultForgottenSubjectHandler(null!));
    }

    // ─── Events (POCO records) ───

    [Fact]
    public void SubjectForgottenEvent_PropertiesSet()
    {
        var evt = new SubjectForgottenEvent(
            SubjectId: "user-9",
            KeysDeleted: 2,
            FieldsAffected: 5,
            OccurredAtUtc: _time.GetUtcNow());

        evt.SubjectId.ShouldBe("user-9");
        evt.KeysDeleted.ShouldBe(2);
        evt.FieldsAffected.ShouldBe(5);
        evt.OccurredAtUtc.ShouldBe(_time.GetUtcNow());
    }

    [Fact]
    public void SubjectKeyRotatedEvent_PropertiesSet()
    {
        var evt = new SubjectKeyRotatedEvent(
            SubjectId: "user-10",
            OldKeyId: "subject:user-10:v1",
            NewKeyId: "subject:user-10:v2",
            OccurredAtUtc: _time.GetUtcNow());

        evt.SubjectId.ShouldBe("user-10");
        evt.OldKeyId.ShouldBe("subject:user-10:v1");
        evt.NewKeyId.ShouldBe("subject:user-10:v2");
    }

    [Fact]
    public void PiiEncryptionFailedEvent_PropertiesSet()
    {
        var evt = new PiiEncryptionFailedEvent(
            SubjectId: "user-11",
            PropertyName: "Email",
            ErrorMessage: "key not found",
            OccurredAtUtc: _time.GetUtcNow());

        evt.SubjectId.ShouldBe("user-11");
        evt.PropertyName.ShouldBe("Email");
        evt.ErrorMessage.ShouldBe("key not found");
    }

    // ─── Model records ───

    [Fact]
    public void CryptoShreddingResult_PropertiesSet()
    {
        var result = new CryptoShreddingResult
        {
            SubjectId = "u",
            KeysDeleted = 1,
            FieldsAffected = 2,
            ShreddedAtUtc = _time.GetUtcNow()
        };

        result.SubjectId.ShouldBe("u");
        result.KeysDeleted.ShouldBe(1);
        result.FieldsAffected.ShouldBe(2);
    }

    [Fact]
    public void KeyRotationResult_PropertiesSet()
    {
        var result = new KeyRotationResult
        {
            SubjectId = "u",
            OldKeyId = "subject:u:v1",
            NewKeyId = "subject:u:v2",
            OldVersion = 1,
            NewVersion = 2,
            RotatedAtUtc = _time.GetUtcNow()
        };

        result.SubjectId.ShouldBe("u");
        result.OldVersion.ShouldBe(1);
        result.NewVersion.ShouldBe(2);
    }

    [Fact]
    public void SubjectKeyInfo_PropertiesSet()
    {
        var info = new SubjectKeyInfo
        {
            SubjectId = "u",
            KeyId = "subject:u:v1",
            Version = 1,
            Status = SubjectKeyStatus.Active,
            CreatedAtUtc = _time.GetUtcNow(),
            ExpiresAtUtc = _time.GetUtcNow().AddDays(90)
        };

        info.SubjectId.ShouldBe("u");
        info.KeyId.ShouldBe("subject:u:v1");
        info.Version.ShouldBe(1);
        info.Status.ShouldBe(SubjectKeyStatus.Active);
        info.ExpiresAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public void SubjectEncryptionInfo_PropertiesSet()
    {
        var info = new SubjectEncryptionInfo
        {
            SubjectId = "u",
            Status = SubjectStatus.Active,
            ActiveKeyVersion = 1,
            TotalKeyVersions = 1,
            CreatedAtUtc = _time.GetUtcNow(),
            ForgottenAtUtc = null
        };

        info.SubjectId.ShouldBe("u");
        info.ActiveKeyVersion.ShouldBe(1);
        info.TotalKeyVersions.ShouldBe(1);
        info.Status.ShouldBe(SubjectStatus.Active);
        info.ForgottenAtUtc.ShouldBeNull();
    }

    [Fact]
    public void CryptoShreddedFieldMetadata_PropertiesSet()
    {
        var metadata = new CryptoShreddedFieldMetadata
        {
            DeclaringType = typeof(CryptoShreddingHappyPathGuardTests),
            PropertyName = "Email",
            SubjectIdProperty = "UserId",
            Category = PersonalDataCategory.Contact
        };

        metadata.PropertyName.ShouldBe("Email");
        metadata.DeclaringType.ShouldBe(typeof(CryptoShreddingHappyPathGuardTests));
        metadata.SubjectIdProperty.ShouldBe("UserId");
        metadata.Category.ShouldBe(PersonalDataCategory.Contact);
    }

    // ─── CryptoShreddedAttribute ───

    [Fact]
    public void CryptoShreddedAttribute_SubjectIdProperty_Set()
    {
        var attr = new CryptoShreddedAttribute { SubjectIdProperty = "UserId" };
        attr.SubjectIdProperty.ShouldBe("UserId");
    }

    // ─── Enums ───

    [Fact]
    public void SubjectKeyStatus_HasAllValues()
    {
        Enum.IsDefined(SubjectKeyStatus.Active).ShouldBeTrue();
        Enum.IsDefined(SubjectKeyStatus.Rotated).ShouldBeTrue();
        Enum.IsDefined(SubjectKeyStatus.Deleted).ShouldBeTrue();
    }

    [Fact]
    public void SubjectStatus_HasAllValues()
    {
        Enum.IsDefined(SubjectStatus.Active).ShouldBeTrue();
        Enum.IsDefined(SubjectStatus.Forgotten).ShouldBeTrue();
    }

    // ─── CryptoShreddingErrors (public static factory) ───

    [Fact]
    public void CryptoShreddingErrors_SubjectForgotten_ReturnsError()
    {
        var error = CryptoShreddingErrors.SubjectForgotten("u-1");
        error.Message.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void CryptoShreddingErrors_InvalidSubjectId_NullAllowed()
    {
        var error = CryptoShreddingErrors.InvalidSubjectId(null);
        error.Message.ShouldContain("(null)");
    }

    [Fact]
    public void CryptoShreddingErrors_AllCodes_FollowConvention()
    {
        CryptoShreddingErrors.SubjectForgottenCode.ShouldStartWith("crypto.");
        CryptoShreddingErrors.EncryptionFailedCode.ShouldStartWith("crypto.");
        CryptoShreddingErrors.DecryptionFailedCode.ShouldStartWith("crypto.");
        CryptoShreddingErrors.KeyRotationFailedCode.ShouldStartWith("crypto.");
        CryptoShreddingErrors.KeyStoreErrorCode.ShouldStartWith("crypto.");
        CryptoShreddingErrors.InvalidSubjectIdCode.ShouldStartWith("crypto.");
        CryptoShreddingErrors.KeyAlreadyExistsCode.ShouldStartWith("crypto.");
        CryptoShreddingErrors.SerializationErrorCode.ShouldStartWith("crypto.");
        CryptoShreddingErrors.AttributeMisconfiguredCode.ShouldStartWith("crypto.");
    }

    [Fact]
    public void CryptoShreddingErrors_EncryptionFailed_WithException_AttachesException()
    {
        var ex = new InvalidOperationException("boom");
        var error = CryptoShreddingErrors.EncryptionFailed("u", "p", ex);
        error.Exception.IsSome.ShouldBeTrue();
    }

    [Fact]
    public void CryptoShreddingErrors_DecryptionFailed_WithException_AttachesException()
    {
        var ex = new InvalidOperationException("boom");
        var error = CryptoShreddingErrors.DecryptionFailed("u", "p", ex);
        error.Exception.IsSome.ShouldBeTrue();
    }

    [Fact]
    public void CryptoShreddingErrors_KeyRotationFailed_WithException_AttachesException()
    {
        var ex = new TimeoutException("t");
        var error = CryptoShreddingErrors.KeyRotationFailed("u", ex);
        error.Exception.IsSome.ShouldBeTrue();
    }

    [Fact]
    public void CryptoShreddingErrors_KeyStoreError_ReturnsError()
    {
        var error = CryptoShreddingErrors.KeyStoreError("GetKey");
        error.Message.ShouldContain("GetKey");
    }

    [Fact]
    public void CryptoShreddingErrors_KeyStoreError_WithException_AttachesException()
    {
        var ex = new TimeoutException("t");
        var error = CryptoShreddingErrors.KeyStoreError("DeleteKeys", ex);
        error.Exception.IsSome.ShouldBeTrue();
    }

    [Fact]
    public void CryptoShreddingErrors_KeyAlreadyExists_ReturnsError()
    {
        var error = CryptoShreddingErrors.KeyAlreadyExists("u");
        error.Message.ShouldContain("u");
    }

    [Fact]
    public void CryptoShreddingErrors_SerializationError_ReturnsError()
    {
        var error = CryptoShreddingErrors.SerializationError(typeof(string));
        error.Message.ShouldContain("String");
    }

    [Fact]
    public void CryptoShreddingErrors_SerializationError_WithException_AttachesException()
    {
        var ex = new InvalidOperationException("boom");
        var error = CryptoShreddingErrors.SerializationError(typeof(string), ex);
        error.Exception.IsSome.ShouldBeTrue();
    }

    [Fact]
    public void CryptoShreddingErrors_AttributeMisconfigured_ReturnsError()
    {
        var error = CryptoShreddingErrors.AttributeMisconfigured("Email", typeof(string), "missing [PersonalData]");
        error.Message.ShouldContain("Email");
        error.Message.ShouldContain("missing [PersonalData]");
    }
}
