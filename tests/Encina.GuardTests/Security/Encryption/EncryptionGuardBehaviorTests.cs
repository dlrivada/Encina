#pragma warning disable CA2012 // ValueTask instances used in NSubstitute mock setup

using System.Collections.Immutable;
using System.Security.Cryptography;
using Encina.Security.Encryption;
using Encina.Security.Encryption.Abstractions;
using Encina.Security.Encryption.Algorithms;
using Encina.Security.Encryption.Health;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.GuardTests.Security.Encryption;

/// <summary>
/// Guard tests that run the real Encina.Security.Encryption implementations: invalid arguments are
/// rejected with the right exception and valid or edge arguments are accepted without throwing.
/// </summary>
public sealed class EncryptionGuardBehaviorTests : IDisposable
{
    private readonly InMemoryKeyProvider _keyProvider = CreateKeyProvider();
    private readonly AesGcmFieldEncryptor _encryptor;
    private readonly EncryptionOrchestrator _orchestrator;
    private readonly IRequestContext _requestContext = RequestContext.CreateForTest(tenantId: "tenant-1");

    public EncryptionGuardBehaviorTests()
    {
        _encryptor = new AesGcmFieldEncryptor(_keyProvider);
        _orchestrator = new EncryptionOrchestrator(_encryptor, NullLogger<EncryptionOrchestrator>.Instance);
        EncryptedPropertyCache.ClearCache();
    }

    public void Dispose()
    {
        _keyProvider.Clear();
        EncryptedPropertyCache.ClearCache();
    }

    #region Null-argument rejection on the real implementations

    [Fact]
    public async Task AesGcmFieldEncryptor_NullArguments_AreRejectedWithTheParameterName()
    {
        var ctx = new EncryptionContext();
        var value = new EncryptedValue
        {
            Ciphertext = [1],
            Nonce = [2],
            Tag = [3],
            Algorithm = EncryptionAlgorithm.Aes256Gcm,
            KeyId = "guard-key"
        };

        (await Should.ThrowAsync<ArgumentNullException>(async () => await _encryptor.EncryptStringAsync(null!, ctx)))
            .ParamName.ShouldBe("plaintext");
        (await Should.ThrowAsync<ArgumentNullException>(async () => await _encryptor.EncryptStringAsync("x", null!)))
            .ParamName.ShouldBe("context");
        (await Should.ThrowAsync<ArgumentNullException>(async () => await _encryptor.DecryptStringAsync(value, null!)))
            .ParamName.ShouldBe("context");
        (await Should.ThrowAsync<ArgumentNullException>(async () => await _encryptor.EncryptBytesAsync(new byte[1], null!)))
            .ParamName.ShouldBe("context");
        (await Should.ThrowAsync<ArgumentNullException>(async () => await _encryptor.DecryptBytesAsync(value, null!)))
            .ParamName.ShouldBe("context");
    }

    [Fact]
    public async Task EncryptionOrchestrator_NullArguments_AreRejectedWithTheParameterName()
    {
        var instance = new GuardPayload { Secret = "x" };

        (await Should.ThrowAsync<ArgumentNullException>(async () => await _orchestrator.EncryptAsync<GuardPayload>(null!, _requestContext)))
            .ParamName.ShouldBe("instance");
        (await Should.ThrowAsync<ArgumentNullException>(async () => await _orchestrator.EncryptAsync(instance, null!)))
            .ParamName.ShouldBe("context");
        (await Should.ThrowAsync<ArgumentNullException>(async () => await _orchestrator.DecryptAsync<GuardPayload>(null!, _requestContext)))
            .ParamName.ShouldBe("instance");
        (await Should.ThrowAsync<ArgumentNullException>(async () => await _orchestrator.DecryptAsync(instance, null!)))
            .ParamName.ShouldBe("context");
        instance.Secret.ShouldBe("x");
    }

    [Fact]
    public void EncryptionPipelineBehavior_NullConstructorArguments_AreRejected()
    {
        var options = Options.Create(new EncryptionOptions());
        var logger = NullLogger<EncryptionPipelineBehavior<GuardPlain, Unit>>.Instance;

        Should.Throw<ArgumentNullException>(() => new EncryptionPipelineBehavior<GuardPlain, Unit>(null!, options, logger))
            .ParamName.ShouldBe("orchestrator");
        Should.Throw<ArgumentNullException>(() => new EncryptionPipelineBehavior<GuardPlain, Unit>(_orchestrator, null!, logger))
            .ParamName.ShouldBe("options");
        Should.Throw<ArgumentNullException>(() => new EncryptionPipelineBehavior<GuardPlain, Unit>(_orchestrator, options, null!))
            .ParamName.ShouldBe("logger");
    }

    [Fact]
    public void AddEncinaEncryption_NullServices_IsRejected()
    {
        IServiceCollection services = null!;

        Should.Throw<ArgumentNullException>(() => services.AddEncinaEncryption()).ParamName.ShouldBe("services");
        Should.Throw<ArgumentNullException>(() => services.AddEncinaEncryption<GuardKeyProvider>()).ParamName.ShouldBe("services");
    }

    [Fact]
    public void Constructors_NullDependencies_AreRejected()
    {
        Should.Throw<ArgumentNullException>(() => new AesGcmFieldEncryptor(null!)).ParamName.ShouldBe("keyProvider");
        Should.Throw<ArgumentNullException>(() => new EncryptionHealthCheck(null!)).ParamName.ShouldBe("serviceProvider");
        Should.Throw<ArgumentNullException>(() => new EncryptionOrchestrator(null!, NullLogger<EncryptionOrchestrator>.Instance))
            .ParamName.ShouldBe("fieldEncryptor");
        Should.Throw<ArgumentNullException>(() => new EncryptionOrchestrator(_encryptor, null!))
            .ParamName.ShouldBe("logger");
    }

    [Fact]
    public void InMemoryKeyProvider_NullArguments_AreRejected()
    {
        var provider = new InMemoryKeyProvider();

        Should.Throw<ArgumentNullException>(() => provider.AddKey(null!, new byte[32])).ParamName.ShouldBe("keyId");
        Should.Throw<ArgumentNullException>(() => provider.AddKey("k", null!)).ParamName.ShouldBe("key");
        Should.Throw<ArgumentNullException>(() => provider.SetCurrentKey(null!)).ParamName.ShouldBe("keyId");
        provider.Count.ShouldBe(0);
    }

    #endregion

    #region AesGcmFieldEncryptor

    [Fact]
    public async Task EncryptStringAsync_EmptyPlaintext_IsAccepted()
    {
        var encrypted = await _encryptor.EncryptStringAsync(string.Empty, new EncryptionContext());

        encrypted.IsRight.ShouldBeTrue();
        var value = encrypted.Match(Right: v => v, Left: _ => default);
        value.KeyId.ShouldBe("guard-key");
        value.Nonce.Length.ShouldBe(12);
        value.Tag.Length.ShouldBe(16);
    }

    [Fact]
    public async Task EncryptBytesAsync_EmptyPayload_IsAccepted()
    {
        var result = await _encryptor.EncryptBytesAsync(ReadOnlyMemory<byte>.Empty, new EncryptionContext());

        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task EncryptBytesAsync_ThenDecryptBytesAsync_ReturnsOriginalBytes()
    {
        var payload = new byte[] { 1, 2, 3, 4, 5 };
        var context = new EncryptionContext { AssociatedData = [.. "aad"u8.ToArray()] };

        var encrypted = await _encryptor.EncryptBytesAsync(payload, context);
        var value = encrypted.Match(Right: v => v, Left: _ => default);
        var decrypted = await _encryptor.DecryptBytesAsync(value, context);

        decrypted.Match(Right: b => b, Left: _ => []).ShouldBe(payload);
    }

    [Fact]
    public async Task DecryptStringAsync_EmptyEncryptedValue_ReturnsInvalidCiphertextInsteadOfThrowing()
    {
        var empty = new EncryptedValue
        {
            Ciphertext = ImmutableArray<byte>.Empty,
            Algorithm = EncryptionAlgorithm.Aes256Gcm,
            KeyId = "guard-key",
            Nonce = ImmutableArray<byte>.Empty
        };

        var result = await _encryptor.DecryptStringAsync(empty, new EncryptionContext());

        ErrorCodeOf(result).ShouldBe(EncryptionErrors.InvalidCiphertextCode);
    }

    [Fact]
    public async Task DecryptBytesAsync_DefaultEncryptedValue_ReturnsInvalidCiphertextInsteadOfThrowing()
    {
        var result = await _encryptor.DecryptBytesAsync(default, new EncryptionContext());

        ErrorCodeOf(result).ShouldBe(EncryptionErrors.InvalidCiphertextCode);
    }

    [Fact]
    public async Task EncryptStringAsync_UnknownContextKeyId_FailsClosedWithKeyNotFound()
    {
        var context = new EncryptionContext { KeyId = "missing-key" };

        var result = await _encryptor.EncryptStringAsync("secret", context);

        ErrorCodeOf(result).ShouldBe(EncryptionErrors.KeyNotFoundCode);
    }

    [Fact]
    public async Task EncryptStringAsync_NoCurrentKeyConfigured_FailsClosedWithKeyNotFound()
    {
        var encryptor = new AesGcmFieldEncryptor(new InMemoryKeyProvider());

        var result = await encryptor.EncryptStringAsync("secret", new EncryptionContext());

        ErrorCodeOf(result).ShouldBe(EncryptionErrors.KeyNotFoundCode);
    }

    [Fact]
    public async Task EncryptStringAsync_KeyOfWrongLength_FailsClosedWithKeyNotFound()
    {
        var provider = new InMemoryKeyProvider();
        provider.AddKey("short", new byte[16]);
        provider.SetCurrentKey("short");
        var encryptor = new AesGcmFieldEncryptor(provider);

        var result = await encryptor.EncryptStringAsync("secret", new EncryptionContext());

        ErrorCodeOf(result).ShouldBe(EncryptionErrors.KeyNotFoundCode);
    }

    [Fact]
    public async Task DecryptStringAsync_KeyOfWrongLength_FailsClosedWithKeyNotFound()
    {
        var encrypted = await _encryptor.EncryptStringAsync("secret", new EncryptionContext());
        var value = encrypted.Match(Right: v => v, Left: _ => default);
        var provider = new InMemoryKeyProvider();
        provider.AddKey(value.KeyId, new byte[8]);

        var result = await new AesGcmFieldEncryptor(provider).DecryptStringAsync(value, new EncryptionContext());

        ErrorCodeOf(result).ShouldBe(EncryptionErrors.KeyNotFoundCode);
    }

    [Fact]
    public async Task DecryptStringAsync_UnknownKey_FailsClosedWithKeyNotFound()
    {
        var encrypted = await _encryptor.EncryptStringAsync("secret", new EncryptionContext());
        var value = encrypted.Match(Right: v => v, Left: _ => default);

        var result = await new AesGcmFieldEncryptor(new InMemoryKeyProvider())
            .DecryptStringAsync(value, new EncryptionContext());

        ErrorCodeOf(result).ShouldBe(EncryptionErrors.KeyNotFoundCode);
    }

    [Fact]
    public async Task DecryptStringAsync_UnsupportedAlgorithm_FailsClosedWithAlgorithmNotSupported()
    {
        var encrypted = await _encryptor.EncryptStringAsync("secret", new EncryptionContext());
        var value = encrypted.Match(Right: v => v, Left: _ => default) with
        {
            Algorithm = (EncryptionAlgorithm)99
        };

        var result = await _encryptor.DecryptStringAsync(value, new EncryptionContext());

        ErrorCodeOf(result).ShouldBe(EncryptionErrors.AlgorithmNotSupportedCode);
    }

    [Fact]
    public async Task DecryptStringAsync_WrongAssociatedData_FailsClosedWithDecryptionFailed()
    {
        var encrypted = await _encryptor.EncryptStringAsync(
            "secret", new EncryptionContext { AssociatedData = [.. "a"u8.ToArray()] });
        var value = encrypted.Match(Right: v => v, Left: _ => default);

        var result = await _encryptor.DecryptStringAsync(
            value, new EncryptionContext { AssociatedData = [.. "b"u8.ToArray()] });

        ErrorCodeOf(result).ShouldBe(EncryptionErrors.DecryptionFailedCode);
    }

    #endregion

    #region InMemoryKeyProvider

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AddKey_BlankKeyId_ThrowsArgumentException(string keyId)
    {
        var provider = new InMemoryKeyProvider();

        var act = () => provider.AddKey(keyId, new byte[32]);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("keyId");
        provider.Count.ShouldBe(0);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void SetCurrentKey_BlankKeyId_ThrowsArgumentException(string keyId)
    {
        var provider = new InMemoryKeyProvider();

        var act = () => provider.SetCurrentKey(keyId);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("keyId");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void GetKeyAsync_BlankKeyId_ThrowsArgumentException(string keyId)
    {
        var provider = new InMemoryKeyProvider();

        Action act = () => _ = provider.GetKeyAsync(keyId);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("keyId");
    }

    [Fact]
    public async Task GetKeyAsync_UnknownKey_ReturnsKeyNotFoundInsteadOfThrowing()
    {
        var result = await _keyProvider.GetKeyAsync("unknown");

        ErrorCodeOf(result).ShouldBe(EncryptionErrors.KeyNotFoundCode);
    }

    [Fact]
    public async Task KeyProviderOperations_CancelledToken_ReturnLeftInsteadOfThrowing()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        (await _keyProvider.GetKeyAsync("guard-key", cts.Token)).IsLeft.ShouldBeTrue();
        (await _keyProvider.GetCurrentKeyIdAsync(cts.Token)).IsLeft.ShouldBeTrue();
        (await _keyProvider.RotateKeyAsync(cts.Token)).IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task RotateKeyAsync_AddsKeyAndMakesItCurrent()
    {
        var before = _keyProvider.Count;

        var rotated = await _keyProvider.RotateKeyAsync();

        var newId = rotated.Match(Right: id => id, Left: _ => string.Empty);
        newId.ShouldStartWith("key-");
        _keyProvider.Count.ShouldBe(before + 1);
        (await _keyProvider.GetCurrentKeyIdAsync()).Match(Right: id => id, Left: _ => "x").ShouldBe(newId);
    }

    [Fact]
    public async Task Clear_RemovesKeysAndCurrentKey()
    {
        _keyProvider.Clear();

        _keyProvider.Count.ShouldBe(0);
        ErrorCodeOf(await _keyProvider.GetCurrentKeyIdAsync()).ShouldBe(EncryptionErrors.KeyNotFoundCode);
    }

    #endregion

    #region EncryptionOrchestrator

    [Fact]
    public async Task EncryptAsync_CancelledToken_ReturnsLeftWithoutTouchingTheInstance()
    {
        var instance = new GuardPayload { Secret = "plain" };
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var result = await _orchestrator.EncryptAsync(instance, _requestContext, cts.Token);

        result.IsLeft.ShouldBeTrue();
        instance.Secret.ShouldBe("plain");
    }

    [Fact]
    public async Task DecryptAsync_CancelledToken_ReturnsLeftWithoutTouchingTheInstance()
    {
        var instance = new GuardPayload { Secret = "ENC:v1:0:k:AA==:AA==:AA==" };
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var result = await _orchestrator.DecryptAsync(instance, _requestContext, cts.Token);

        result.IsLeft.ShouldBeTrue();
        instance.Secret.ShouldBe("ENC:v1:0:k:AA==:AA==:AA==");
    }

    [Fact]
    public async Task EncryptAsync_TypeWithoutEncryptedProperties_ReturnsSameInstance()
    {
        var instance = new GuardPlain { Name = "n" };

        var result = await _orchestrator.EncryptAsync(instance, _requestContext);

        result.Match(Right: r => ReferenceEquals(r, instance), Left: _ => false).ShouldBeTrue();
    }

    [Fact]
    public async Task EncryptAsync_NullStringProperty_IsSkipped()
    {
        var instance = new GuardPayload { Secret = null };

        var result = await _orchestrator.EncryptAsync(instance, _requestContext);

        result.IsRight.ShouldBeTrue();
        instance.Secret.ShouldBeNull();
    }

    [Fact]
    public async Task DecryptAsync_MalformedCiphertext_FailsClosedWithInvalidCiphertext()
    {
        var instance = new GuardPayload { Secret = "not-an-envelope" };

        var result = await _orchestrator.DecryptAsync(instance, _requestContext);

        ErrorCodeOf(result).ShouldBe(EncryptionErrors.InvalidCiphertextCode);
    }

    [Fact]
    public async Task DecryptAsync_MalformedCiphertextWithFailOnErrorFalse_LeavesValueUnchanged()
    {
        var instance = new GuardLenient { Secret = "not-an-envelope" };

        var result = await _orchestrator.DecryptAsync(instance, _requestContext);

        result.IsRight.ShouldBeTrue();
        instance.Secret.ShouldBe("not-an-envelope");
    }

    [Fact]
    public async Task EncryptAsync_MissingKeyWithFailOnErrorFalse_LeavesValueUnchanged()
    {
        var orchestrator = new EncryptionOrchestrator(
            new AesGcmFieldEncryptor(new InMemoryKeyProvider()),
            NullLogger<EncryptionOrchestrator>.Instance);
        var instance = new GuardLenient { Secret = "plain" };

        var result = await orchestrator.EncryptAsync(instance, _requestContext);

        result.IsRight.ShouldBeTrue();
        instance.Secret.ShouldBe("plain");
    }

    [Fact]
    public async Task EncryptAsync_MissingKeyWithDefaultFailOnError_FailsClosed()
    {
        var orchestrator = new EncryptionOrchestrator(
            new AesGcmFieldEncryptor(new InMemoryKeyProvider()),
            NullLogger<EncryptionOrchestrator>.Instance);
        var instance = new GuardPayload { Secret = "plain" };

        var result = await orchestrator.EncryptAsync(instance, _requestContext);

        ErrorCodeOf(result).ShouldBe(EncryptionErrors.KeyNotFoundCode);
        instance.Secret.ShouldBe("plain");
    }

    #endregion

    #region EncryptionPipelineBehavior

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        var behavior = CreateBehavior<GuardPlain, Unit>();

        var act = async () => await behavior.Handle(
            null!, _requestContext, () => ValueTask.FromResult(Prelude.Right<EncinaError, Unit>(Unit.Default)), CancellationToken.None);

        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("request");
    }

    [Fact]
    public async Task Handle_NullContext_ThrowsArgumentNullException()
    {
        var behavior = CreateBehavior<GuardPlain, Unit>();

        var act = async () => await behavior.Handle(
            new GuardPlain(), null!, () => ValueTask.FromResult(Prelude.Right<EncinaError, Unit>(Unit.Default)), CancellationToken.None);

        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("context");
    }

    [Fact]
    public async Task Handle_EncryptedRequest_EncryptsBeforeCallingNextStep()
    {
        var behavior = CreateBehavior<GuardPayload, Unit>();
        var request = new GuardPayload { Secret = "plain" };
        string? seenByHandler = null;

        var result = await behavior.Handle(
            request,
            _requestContext,
            () =>
            {
                seenByHandler = request.Secret;
                return ValueTask.FromResult(Prelude.Right<EncinaError, Unit>(Unit.Default));
            },
            CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        seenByHandler.ShouldNotBeNull();
        seenByHandler.ShouldStartWith("ENC:v1:");
    }

    [Fact]
    public async Task Handle_EncryptionFails_DoesNotCallNextStep()
    {
        var emptyKeys = new EncryptionOrchestrator(
            new AesGcmFieldEncryptor(new InMemoryKeyProvider()),
            NullLogger<EncryptionOrchestrator>.Instance);
        var behavior = new EncryptionPipelineBehavior<GuardPayload, Unit>(
            emptyKeys,
            Options.Create(new EncryptionOptions { EnableMetrics = true }),
            NullLogger<EncryptionPipelineBehavior<GuardPayload, Unit>>.Instance);
        var called = false;

        var result = await behavior.Handle(
            new GuardPayload { Secret = "plain" },
            _requestContext,
            () =>
            {
                called = true;
                return ValueTask.FromResult(Prelude.Right<EncinaError, Unit>(Unit.Default));
            },
            CancellationToken.None);

        ErrorCodeOf(result).ShouldBe(EncryptionErrors.KeyNotFoundCode);
        called.ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_DecryptOnReceiveRequestWithMalformedValue_FailsClosedBeforeNextStep()
    {
        var behavior = CreateBehavior<GuardIncoming, Unit>();
        var called = false;

        var result = await behavior.Handle(
            new GuardIncoming { Secret = "tampered" },
            _requestContext,
            () =>
            {
                called = true;
                return ValueTask.FromResult(Prelude.Right<EncinaError, Unit>(Unit.Default));
            },
            CancellationToken.None);

        ErrorCodeOf(result).ShouldBe(EncryptionErrors.InvalidCiphertextCode);
        called.ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_EncryptedResponse_EncryptsResponsePropertiesAfterHandler()
    {
        var behavior = CreateBehavior<GuardResponseRequest, GuardResponse>(tracing: true, metrics: true);
        var response = new GuardResponse { Secret = "plain-response" };

        var result = await behavior.Handle(
            new GuardResponseRequest(),
            _requestContext,
            () => ValueTask.FromResult(Prelude.Right<EncinaError, GuardResponse>(response)),
            CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        response.Secret.ShouldStartWith("ENC:v1:");
    }

    [Fact]
    public async Task Handle_HandlerReturnsLeft_PropagatesErrorWithoutEncryptingResponse()
    {
        var behavior = CreateBehavior<GuardResponseRequest, GuardResponse>();
        var error = EncinaError.New("handler failed");
        var response = new GuardResponse { Secret = "untouched" };

        var result = await behavior.Handle(
            new GuardResponseRequest(),
            _requestContext,
            () => ValueTask.FromResult(Prelude.Left<EncinaError, GuardResponse>(error)),
            CancellationToken.None);

        result.Match(Right: _ => "right", Left: e => e.Message).ShouldBe("handler failed");
        response.Secret.ShouldBe("untouched");
    }

    #endregion

    #region EncryptionHealthCheck

    [Fact]
    public async Task CheckHealthAsync_NoKeyProviderRegistered_ReportsUnhealthy()
    {
        using var provider = new ServiceCollection().BuildServiceProvider();
        var check = new EncryptionHealthCheck(provider);

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task CheckHealthAsync_KeyProviderWithoutCurrentKey_ReportsUnhealthy()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IKeyProvider>(new InMemoryKeyProvider());
        using var provider = services.BuildServiceProvider();

        var result = await new EncryptionHealthCheck(provider).CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task CheckHealthAsync_NoFieldEncryptorRegistered_ReportsUnhealthy()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IKeyProvider>(_keyProvider);
        using var provider = services.BuildServiceProvider();

        var result = await new EncryptionHealthCheck(provider).CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task CheckHealthAsync_FullRegistration_ReportsHealthy()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IKeyProvider>(_keyProvider);
        services.AddSingleton<IFieldEncryptor>(_encryptor);
        using var provider = services.BuildServiceProvider();

        var result = await new EncryptionHealthCheck(provider).CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Data["currentKeyId"].ShouldBe("guard-key");
    }

    [Fact]
    public async Task CheckHealthAsync_FieldEncryptorThrows_ReportsUnhealthyWithoutLeakingMessage()
    {
        var failing = Substitute.For<IFieldEncryptor>();
        failing.EncryptStringAsync(Arg.Any<string>(), Arg.Any<EncryptionContext>(), Arg.Any<CancellationToken>())
            .Returns<ValueTask<Either<EncinaError, EncryptedValue>>>(_ => throw new InvalidOperationException("secret detail"));
        var services = new ServiceCollection();
        services.AddSingleton<IKeyProvider>(_keyProvider);
        services.AddSingleton(failing);
        using var provider = services.BuildServiceProvider();

        var result = await new EncryptionHealthCheck(provider).CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        (result.Description ?? string.Empty).ShouldNotContain("secret detail");
    }

    #endregion

    #region ServiceCollectionExtensions

    [Fact]
    public void AddEncinaEncryption_NullConfigure_RegistersDefaultsAndResolves()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        var returned = services.AddEncinaEncryption();
        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        using var scope = provider.CreateScope();

        returned.ShouldBeSameAs(services);
        scope.ServiceProvider.GetRequiredService<IKeyProvider>().ShouldBeOfType<InMemoryKeyProvider>();
        scope.ServiceProvider.GetRequiredService<IFieldEncryptor>().ShouldBeOfType<AesGcmFieldEncryptor>();
        scope.ServiceProvider.GetRequiredService<IEncryptionOrchestrator>().ShouldBeOfType<EncryptionOrchestrator>();
        scope.ServiceProvider.GetRequiredService<IOptions<EncryptionOptions>>().Value.AddHealthCheck.ShouldBeFalse();
    }

    [Fact]
    public void AddEncinaEncryption_WithHealthCheckOption_RegistersHealthCheck()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaEncryption(o => o.AddHealthCheck = true);
        using var provider = services.BuildServiceProvider();

        var registrations = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations;
        registrations.ShouldContain(r => r.Name == EncryptionHealthCheck.DefaultName);
    }

    [Fact]
    public void AddEncinaEncryption_Generic_RegistersCustomKeyProviderBeforeDefault()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaEncryption<GuardKeyProvider>();
        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IKeyProvider>().ShouldBeOfType<GuardKeyProvider>();
    }

    #endregion

    #region EncryptedPropertyCache

    [Fact]
    public void GetProperties_NullType_ThrowsArgumentNullException()
    {
        var act = () => EncryptedPropertyCache.GetProperties(null!);

        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void GetProperties_PropertiesWithoutPublicSetter_AreIgnored()
    {
        var properties = EncryptedPropertyCache.GetProperties(typeof(GuardReadOnly));

        properties.ShouldBeEmpty();
    }

    [Fact]
    public void GetProperties_AnnotatedProperty_IsDiscoveredWithItsAttributeAndAccessors()
    {
        var properties = EncryptedPropertyCache.GetProperties(typeof(GuardPayload));
        var instance = new GuardPayload { Secret = "a" };

        properties.Length.ShouldBe(1);
        properties[0].Property.Name.ShouldBe(nameof(GuardPayload.Secret));
        properties[0].Attribute.ShouldBeOfType<EncryptAttribute>();
        properties[0].GetValue(instance).ShouldBe("a");
        properties[0].SetValue(instance, "b");
        instance.Secret.ShouldBe("b");
        EncryptedPropertyCache.GetProperties(typeof(GuardPayload)).ShouldBeSameAs(properties);
    }

    #endregion

    private static string? ErrorCodeOf<T>(Either<EncinaError, T> result) =>
        result.Match(Right: _ => null, Left: e => e.GetCode().IfNone("none"));

    private static InMemoryKeyProvider CreateKeyProvider()
    {
        var provider = new InMemoryKeyProvider();
        var key = new byte[32];
        RandomNumberGenerator.Fill(key);
        provider.AddKey("guard-key", key);
        provider.SetCurrentKey("guard-key");
        return provider;
    }

    private EncryptionPipelineBehavior<TRequest, TResponse> CreateBehavior<TRequest, TResponse>(
        bool tracing = false,
        bool metrics = false)
        where TRequest : notnull, IRequest<TResponse> =>
        new(
            _orchestrator,
            Options.Create(new EncryptionOptions { EnableTracing = tracing, EnableMetrics = metrics }),
            NullLogger<EncryptionPipelineBehavior<TRequest, TResponse>>.Instance);

    #region Test Types

    public sealed class GuardPayload : IRequest<Unit>
    {
        [Encrypt]
        public string? Secret { get; set; }
    }

    public sealed class GuardLenient
    {
        [Encrypt(FailOnError = false)]
        public string? Secret { get; set; }
    }

    [DecryptOnReceive]
    public sealed class GuardIncoming : IRequest<Unit>
    {
        [Encrypt]
        public string? Secret { get; set; }
    }

    public sealed class GuardPlain : IRequest<Unit>
    {
        public string? Name { get; set; }
    }

    public sealed class GuardResponseRequest : IRequest<GuardResponse>
    {
    }

    [EncryptedResponse]
    public sealed class GuardResponse
    {
        [Encrypt]
        public string? Secret { get; set; }
    }

    public sealed class GuardReadOnly
    {
        [Encrypt]
        public string Secret { get; } = "x";
    }

    public sealed class GuardKeyProvider : IKeyProvider
    {
        public ValueTask<Either<EncinaError, byte[]>> GetKeyAsync(string keyId, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(Prelude.Right<EncinaError, byte[]>(new byte[32]));

        public ValueTask<Either<EncinaError, string>> GetCurrentKeyIdAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(Prelude.Right<EncinaError, string>("custom"));

        public ValueTask<Either<EncinaError, string>> RotateKeyAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(Prelude.Right<EncinaError, string>("custom"));
    }

    #endregion
}
