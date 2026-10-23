#pragma warning disable CA2012 // ValueTask instances used in NSubstitute mock setup

using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Security.Cryptography;
using System.Text;
using Encina.Security.Encryption;
using Encina.Security.Encryption.Abstractions;
using Encina.Security.Encryption.Algorithms;
using Encina.Security.Encryption.Diagnostics;
using Encina.Security.Encryption.Health;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Encina.ContractTests.Security.Encryption;

/// <summary>
/// Behavioral contract tests: the real <see cref="AesGcmFieldEncryptor"/>, <see cref="InMemoryKeyProvider"/>,
/// <see cref="EncryptionOrchestrator"/>, pipeline behavior and health check must honor the contracts of
/// <see cref="IFieldEncryptor"/>, <see cref="IKeyProvider"/> and <see cref="IEncryptionOrchestrator"/>.
/// </summary>
public sealed class EncryptionBehaviorContractTests : IDisposable
{
    private const string KeyId = "contract-key";

    private readonly InMemoryKeyProvider _keys = new();
    private readonly AesGcmFieldEncryptor _encryptor;
    private readonly EncryptionOrchestrator _orchestrator;
    private readonly IRequestContext _requestContext = RequestContext.CreateForTest(tenantId: "tenant-1");

    public EncryptionBehaviorContractTests()
    {
        var key = new byte[32];
        RandomNumberGenerator.Fill(key);
        _keys.AddKey(KeyId, key);
        _keys.SetCurrentKey(KeyId);
        _encryptor = new AesGcmFieldEncryptor(_keys);
        _orchestrator = new EncryptionOrchestrator(_encryptor, NullLogger<EncryptionOrchestrator>.Instance);
        EncryptedPropertyCache.ClearCache();
    }

    public void Dispose()
    {
        _keys.Clear();
        EncryptedPropertyCache.ClearCache();
    }

    #region IFieldEncryptor contract

    [Fact]
    public async Task FieldEncryptor_EncryptString_ProducesAesGcmValueWithKeyNonceAndTag()
    {
        var result = await _encryptor.EncryptStringAsync("hello", new EncryptionContext());

        var value = Unwrap(result);
        value.Algorithm.ShouldBe(EncryptionAlgorithm.Aes256Gcm);
        value.KeyId.ShouldBe(KeyId);
        value.Nonce.Length.ShouldBe(12);
        value.Tag.Length.ShouldBe(16);
        value.Ciphertext.Length.ShouldBe(Encoding.UTF8.GetByteCount("hello"));
    }

    [Fact]
    public async Task FieldEncryptor_EncryptString_NeverEmitsPlaintextInCiphertext()
    {
        var value = Unwrap(await _encryptor.EncryptStringAsync("supersecret", new EncryptionContext()));

        Encoding.UTF8.GetString([.. value.Ciphertext]).ShouldNotContain("supersecret");
    }

    [Fact]
    public async Task FieldEncryptor_SamePlaintextTwice_ProducesDistinctNonceAndCiphertext()
    {
        var a = Unwrap(await _encryptor.EncryptStringAsync("same", new EncryptionContext()));
        var b = Unwrap(await _encryptor.EncryptStringAsync("same", new EncryptionContext()));

        a.Nonce.AsSpan().SequenceEqual(b.Nonce.AsSpan()).ShouldBeFalse();
        a.Ciphertext.AsSpan().SequenceEqual(b.Ciphertext.AsSpan()).ShouldBeFalse();
    }

    [Theory]
    [InlineData("a")]
    [InlineData("Unicode: áéíóú 中文")]
    [InlineData("A much longer value that spans more than one AES block to prove the stream is handled whole.")]
    public async Task FieldEncryptor_StringRoundTrip_ReturnsOriginal(string plaintext)
    {
        var context = new EncryptionContext { Purpose = "contract", TenantId = "t" };

        var encrypted = Unwrap(await _encryptor.EncryptStringAsync(plaintext, context));
        var decrypted = Unwrap(await _encryptor.DecryptStringAsync(encrypted, context));

        decrypted.ShouldBe(plaintext);
    }

    [Fact]
    public async Task FieldEncryptor_BytesRoundTrip_ReturnsOriginal()
    {
        var payload = RandomNumberGenerator.GetBytes(257);

        var encrypted = Unwrap(await _encryptor.EncryptBytesAsync(payload, new EncryptionContext()));
        var decrypted = Unwrap(await _encryptor.DecryptBytesAsync(encrypted, new EncryptionContext()));

        decrypted.ShouldBe(payload);
    }

    [Fact]
    public async Task FieldEncryptor_ExplicitContextKeyId_IsUsedInsteadOfCurrentKey()
    {
        var other = RandomNumberGenerator.GetBytes(32);
        _keys.AddKey("other-key", other);

        var encrypted = Unwrap(await _encryptor.EncryptStringAsync("x", new EncryptionContext { KeyId = "other-key" }));

        encrypted.KeyId.ShouldBe("other-key");
    }

    [Fact]
    public async Task FieldEncryptor_TamperedCiphertext_ReturnsDecryptionFailed()
    {
        var encrypted = Unwrap(await _encryptor.EncryptStringAsync("integrity", new EncryptionContext()));
        var bytes = encrypted.Ciphertext.ToArray();
        bytes[0] ^= 0xFF;
        var tampered = encrypted with { Ciphertext = [.. bytes] };

        var result = await _encryptor.DecryptStringAsync(tampered, new EncryptionContext());

        ErrorCode(result).ShouldBe(EncryptionErrors.DecryptionFailedCode);
    }

    [Fact]
    public async Task FieldEncryptor_TamperedTag_ReturnsDecryptionFailed()
    {
        var encrypted = Unwrap(await _encryptor.EncryptStringAsync("integrity", new EncryptionContext()));
        var tag = encrypted.Tag.ToArray();
        tag[0] ^= 0xFF;

        var result = await _encryptor.DecryptStringAsync(encrypted with { Tag = [.. tag] }, new EncryptionContext());

        ErrorCode(result).ShouldBe(EncryptionErrors.DecryptionFailedCode);
    }

    [Fact]
    public async Task FieldEncryptor_AssociatedDataMismatch_ReturnsDecryptionFailed()
    {
        var encrypted = Unwrap(await _encryptor.EncryptStringAsync(
            "bound", new EncryptionContext { AssociatedData = [.. "tenant-a"u8.ToArray()] }));

        var result = await _encryptor.DecryptStringAsync(
            encrypted, new EncryptionContext { AssociatedData = [.. "tenant-b"u8.ToArray()] });

        ErrorCode(result).ShouldBe(EncryptionErrors.DecryptionFailedCode);
    }

    [Fact]
    public async Task FieldEncryptor_MissingKey_ReturnsKeyNotFound()
    {
        var encrypted = Unwrap(await _encryptor.EncryptStringAsync("x", new EncryptionContext()));
        _keys.Clear();

        var result = await _encryptor.DecryptStringAsync(encrypted, new EncryptionContext());

        ErrorCode(result).ShouldBe(EncryptionErrors.KeyNotFoundCode);
    }

    [Fact]
    public async Task FieldEncryptor_NoCurrentKey_ReturnsKeyNotFound()
    {
        _keys.Clear();

        var result = await _encryptor.EncryptStringAsync("x", new EncryptionContext());

        ErrorCode(result).ShouldBe(EncryptionErrors.KeyNotFoundCode);
    }

    [Fact]
    public async Task FieldEncryptor_UnsupportedAlgorithm_ReturnsAlgorithmNotSupported()
    {
        var encrypted = Unwrap(await _encryptor.EncryptStringAsync("x", new EncryptionContext()));

        var result = await _encryptor.DecryptStringAsync(
            encrypted with { Algorithm = (EncryptionAlgorithm)42 }, new EncryptionContext());

        ErrorCode(result).ShouldBe(EncryptionErrors.AlgorithmNotSupportedCode);
    }

    [Fact]
    public async Task FieldEncryptor_MissingNonceOrTag_ReturnsInvalidCiphertext()
    {
        var encrypted = Unwrap(await _encryptor.EncryptStringAsync("x", new EncryptionContext()));

        var noNonce = await _encryptor.DecryptStringAsync(
            encrypted with { Nonce = ImmutableArray<byte>.Empty }, new EncryptionContext());
        var noTag = await _encryptor.DecryptStringAsync(
            encrypted with { Tag = ImmutableArray<byte>.Empty }, new EncryptionContext());

        ErrorCode(noNonce).ShouldBe(EncryptionErrors.InvalidCiphertextCode);
        ErrorCode(noTag).ShouldBe(EncryptionErrors.InvalidCiphertextCode);
    }

    [Fact]
    public async Task FieldEncryptor_KeyOfWrongSize_ReturnsKeyNotFound()
    {
        _keys.AddKey("bad-size", new byte[10]);

        var result = await _encryptor.EncryptStringAsync("x", new EncryptionContext { KeyId = "bad-size" });

        ErrorCode(result).ShouldBe(EncryptionErrors.KeyNotFoundCode);
    }

    #endregion

    #region IKeyProvider contract

    [Fact]
    public async Task KeyProvider_GetKey_ReturnsStoredKeyMaterial()
    {
        var key = RandomNumberGenerator.GetBytes(32);
        _keys.AddKey("stored", key);

        var result = Unwrap(await _keys.GetKeyAsync("stored"));

        result.ShouldBe(key);
    }

    [Fact]
    public async Task KeyProvider_GetKey_UnknownKeyReturnsKeyNotFound()
    {
        ErrorCode(await _keys.GetKeyAsync("nope")).ShouldBe(EncryptionErrors.KeyNotFoundCode);
    }

    [Fact]
    public async Task KeyProvider_GetCurrentKeyId_ReturnsConfiguredKey()
    {
        Unwrap(await _keys.GetCurrentKeyIdAsync()).ShouldBe(KeyId);
    }

    [Fact]
    public async Task KeyProvider_RotateKey_CreatesNewCurrentKeyAndKeepsOldOne()
    {
        var rotatedId = Unwrap(await _keys.RotateKeyAsync());

        rotatedId.ShouldNotBe(KeyId);
        Unwrap(await _keys.GetCurrentKeyIdAsync()).ShouldBe(rotatedId);
        Unwrap(await _keys.GetKeyAsync(rotatedId)).Length.ShouldBe(32);
        (await _keys.GetKeyAsync(KeyId)).IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task KeyProvider_RotatedKey_DecryptsDataEncryptedWithPreviousKey()
    {
        var encrypted = Unwrap(await _encryptor.EncryptStringAsync("before-rotation", new EncryptionContext()));
        await _keys.RotateKeyAsync();

        var decrypted = Unwrap(await _encryptor.DecryptStringAsync(encrypted, new EncryptionContext()));
        var reEncrypted = Unwrap(await _encryptor.EncryptStringAsync("after", new EncryptionContext()));

        decrypted.ShouldBe("before-rotation");
        reEncrypted.KeyId.ShouldNotBe(KeyId);
    }

    [Fact]
    public async Task KeyProvider_CancelledToken_ReturnsLeftForEveryOperation()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        (await _keys.GetKeyAsync(KeyId, cts.Token)).IsLeft.ShouldBeTrue();
        (await _keys.GetCurrentKeyIdAsync(cts.Token)).IsLeft.ShouldBeTrue();
        (await _keys.RotateKeyAsync(cts.Token)).IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task KeyProvider_Clear_RemovesEverything()
    {
        _keys.Count.ShouldBe(1);

        _keys.Clear();

        _keys.Count.ShouldBe(0);
        ErrorCode(await _keys.GetCurrentKeyIdAsync()).ShouldBe(EncryptionErrors.KeyNotFoundCode);
    }

    #endregion

    #region IEncryptionOrchestrator contract

    [Fact]
    public async Task Orchestrator_Encrypt_ReplacesAnnotatedPropertiesWithVersionedEnvelope()
    {
        var model = new ContractModel { Email = "a@b.c", Name = "plain" };

        var result = Unwrap(await _orchestrator.EncryptAsync(model, _requestContext));

        result.ShouldBeSameAs(model);
        model.Email.ShouldStartWith("ENC:v1:0:" + KeyId + ":");
        model.Email!.Split(':').Length.ShouldBe(7);
        model.Name.ShouldBe("plain");
    }

    [Fact]
    public async Task Orchestrator_EncryptThenDecrypt_RestoresOriginalValues()
    {
        var model = new ContractModel { Email = "a@b.c", Name = "plain" };

        await _orchestrator.EncryptAsync(model, _requestContext);
        await _orchestrator.DecryptAsync(model, _requestContext);

        model.Email.ShouldBe("a@b.c");
        model.Name.ShouldBe("plain");
    }

    [Fact]
    public async Task Orchestrator_Decrypt_TamperedEnvelope_FailsClosedByDefault()
    {
        var model = new ContractModel { Email = "a@b.c" };
        await _orchestrator.EncryptAsync(model, _requestContext);
        var parts = model.Email!.Split(':');
        parts[6] = Convert.ToBase64String(new byte[] { 9, 9, 9, 9 });
        model.Email = string.Join(':', parts);

        var result = await _orchestrator.DecryptAsync(model, _requestContext);

        ErrorCode(result).ShouldBe(EncryptionErrors.DecryptionFailedCode);
    }

    [Theory]
    [InlineData("not-encrypted")]
    [InlineData("ENC:v1:0:key:only:four")]
    [InlineData("ENC:v1:7:key:AA==:AA==:AA==")]
    [InlineData("ENC:v1:0:key:***:AA==:AA==")]
    public async Task Orchestrator_Decrypt_MalformedEnvelope_ReturnsInvalidCiphertext(string envelope)
    {
        var model = new ContractModel { Email = envelope };

        var result = await _orchestrator.DecryptAsync(model, _requestContext);

        ErrorCode(result).ShouldBe(EncryptionErrors.InvalidCiphertextCode);
    }

    [Fact]
    public async Task Orchestrator_FailOnErrorFalse_LeavesValuesUnchangedOnEncryptionAndDecryptionErrors()
    {
        _keys.Clear();
        var model = new ContractLenientModel { Value = "keep" };

        (await _orchestrator.EncryptAsync(model, _requestContext)).IsRight.ShouldBeTrue();
        model.Value.ShouldBe("keep");

        var other = new ContractLenientModel { Value = "ENC:v1:0:gone:AAAA:AAAA:AAAA" };
        (await _orchestrator.DecryptAsync(other, _requestContext)).IsRight.ShouldBeTrue();
        other.Value.ShouldBe("ENC:v1:0:gone:AAAA:AAAA:AAAA");
    }

    [Fact]
    public async Task Orchestrator_CancelledToken_ReturnsLeft()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        (await _orchestrator.EncryptAsync(new ContractModel(), _requestContext, cts.Token)).IsLeft.ShouldBeTrue();
        (await _orchestrator.DecryptAsync(new ContractModel(), _requestContext, cts.Token)).IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task Orchestrator_TypeWithoutAnnotatedProperties_IsReturnedUntouched()
    {
        var model = new ContractPlain { Name = "n" };

        Unwrap(await _orchestrator.EncryptAsync(model, _requestContext)).ShouldBeSameAs(model);
        Unwrap(await _orchestrator.DecryptAsync(model, _requestContext)).ShouldBeSameAs(model);
        model.Name.ShouldBe("n");
    }

    [Fact]
    public async Task Orchestrator_PassesPurposeKeyIdAndTenantToFieldEncryptor()
    {
        var captured = new List<EncryptionContext>();
        var spy = Substitute.For<IFieldEncryptor>();
        spy.EncryptStringAsync(Arg.Any<string>(), Arg.Do<EncryptionContext>(captured.Add), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, EncryptedValue>>(
                new EncryptedValue
                {
                    Ciphertext = [1],
                    Nonce = [2],
                    Tag = [3],
                    Algorithm = EncryptionAlgorithm.Aes256Gcm,
                    KeyId = "k"
                }));
        var orchestrator = new EncryptionOrchestrator(spy, NullLogger<EncryptionOrchestrator>.Instance);

        await orchestrator.EncryptAsync(new ContractPurposeModel { Value = "v" }, _requestContext);

        var context = captured.ShouldHaveSingleItem();
        context.Purpose.ShouldBe("billing");
        context.KeyId.ShouldBe("billing-key");
        context.TenantId.ShouldBe("tenant-1");
    }

    #endregion

    #region Pipeline behavior and health check

    [Fact]
    public async Task PipelineBehavior_EncryptedRequest_RunsHandlerOnlyAfterEncryption()
    {
        var behavior = new EncryptionPipelineBehavior<ContractCommand, Unit>(
            _orchestrator,
            Options.Create(new EncryptionOptions { EnableTracing = true, EnableMetrics = true }),
            NullLogger<EncryptionPipelineBehavior<ContractCommand, Unit>>.Instance);
        var command = new ContractCommand { Email = "a@b.c" };
        string? seen = null;

        var result = await behavior.Handle(
            command,
            _requestContext,
            () =>
            {
                seen = command.Email;
                return ValueTask.FromResult<Either<EncinaError, Unit>>(Unit.Default);
            },
            CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        seen.ShouldNotBeNull().ShouldStartWith("ENC:v1:");
    }

    [Fact]
    public async Task PipelineBehavior_EmitsActivityAndMetricsWhenEnabled()
    {
        var activities = new List<Activity>();
        using var activityListener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == EncryptionDiagnostics.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activities.Add
        };
        ActivitySource.AddActivityListener(activityListener);
        var measurements = new List<string>();
        using var meterListener = new MeterListener();
        meterListener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Meter.Name == EncryptionDiagnostics.SourceName)
            {
                l.EnableMeasurementEvents(instrument);
            }
        };
        meterListener.SetMeasurementEventCallback<long>((i, _, _, _) => measurements.Add(i.Name));
        meterListener.Start();
        var behavior = new EncryptionPipelineBehavior<ContractCommand, Unit>(
            _orchestrator,
            Options.Create(new EncryptionOptions { EnableTracing = true, EnableMetrics = true }),
            NullLogger<EncryptionPipelineBehavior<ContractCommand, Unit>>.Instance);

        await behavior.Handle(
            new ContractCommand { Email = "a@b.c" },
            _requestContext,
            () => ValueTask.FromResult<Either<EncinaError, Unit>>(Unit.Default),
            CancellationToken.None);

        activities.ShouldContain(a => a.OperationName == "Encryption.Process"
            && Equals(a.GetTagItem(EncryptionDiagnostics.TagOutcome), "success"));
        measurements.ShouldContain("encryption.operations");
    }

    [Fact]
    public async Task PipelineBehavior_FailedEncryption_RecordsFailureTelemetryAndSkipsHandler()
    {
        _keys.Clear();
        var failures = new List<string>();
        using var meterListener = new MeterListener();
        meterListener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Meter.Name == EncryptionDiagnostics.SourceName)
            {
                l.EnableMeasurementEvents(instrument);
            }
        };
        meterListener.SetMeasurementEventCallback<long>((i, _, _, _) => failures.Add(i.Name));
        meterListener.Start();
        var behavior = new EncryptionPipelineBehavior<ContractCommand, Unit>(
            _orchestrator,
            Options.Create(new EncryptionOptions { EnableTracing = true, EnableMetrics = true }),
            NullLogger<EncryptionPipelineBehavior<ContractCommand, Unit>>.Instance);
        var handlerRan = false;

        var result = await behavior.Handle(
            new ContractCommand { Email = "a@b.c" },
            _requestContext,
            () =>
            {
                handlerRan = true;
                return ValueTask.FromResult<Either<EncinaError, Unit>>(Unit.Default);
            },
            CancellationToken.None);

        ErrorCode(result).ShouldBe(EncryptionErrors.KeyNotFoundCode);
        handlerRan.ShouldBeFalse();
        failures.ShouldContain("encryption.failures");
    }

    [Fact]
    public async Task HealthCheck_WithRegisteredServices_RoundTripsAndReportsCurrentKey()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IKeyProvider>(_keys);
        services.AddSingleton<IFieldEncryptor>(_encryptor);
        using var provider = services.BuildServiceProvider();

        var result = await new EncryptionHealthCheck(provider).CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Data["currentKeyId"].ShouldBe(KeyId);
        result.Data["algorithm"].ShouldBe("Aes256Gcm");
    }

    [Fact]
    public async Task HealthCheck_WithoutCurrentKey_FailsClosed()
    {
        _keys.Clear();
        var services = new ServiceCollection();
        services.AddSingleton<IKeyProvider>(_keys);
        services.AddSingleton<IFieldEncryptor>(_encryptor);
        using var provider = services.BuildServiceProvider();

        var result = await new EncryptionHealthCheck(provider).CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task HealthCheck_MismatchedRoundTrip_FailsClosed()
    {
        var liar = new StubFieldEncryptor(StubValue(), "something else");
        var services = new ServiceCollection();
        services.AddSingleton<IKeyProvider>(_keys);
        services.AddSingleton<IFieldEncryptor>(liar);
        using var provider = services.BuildServiceProvider();

        var result = await new EncryptionHealthCheck(provider).CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task HealthCheck_DecryptPhaseFailure_FailsClosed()
    {
        var failing = new StubFieldEncryptor(StubValue(), EncryptionErrors.DecryptionFailed(KeyId));
        var services = new ServiceCollection();
        services.AddSingleton<IKeyProvider>(_keys);
        services.AddSingleton<IFieldEncryptor>(failing);
        using var provider = services.BuildServiceProvider();

        var result = await new EncryptionHealthCheck(provider).CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldNotBeNull().ShouldContain(EncryptionErrors.DecryptionFailedCode);
    }

    [Fact]
    public async Task HealthCheck_EncryptPhaseFailure_FailsClosed()
    {
        var failing = new StubFieldEncryptor(EncryptionErrors.KeyNotFound(KeyId), "unused");
        var services = new ServiceCollection();
        services.AddSingleton<IKeyProvider>(_keys);
        services.AddSingleton<IFieldEncryptor>(failing);
        using var provider = services.BuildServiceProvider();

        var result = await new EncryptionHealthCheck(provider).CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldNotBeNull().ShouldContain(EncryptionErrors.KeyNotFoundCode);
    }

    #endregion

    #region Registration, errors and value types

    [Fact]
    public void AddEncinaEncryption_RegistersTheDocumentedServiceSurface()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaEncryption(o =>
        {
            o.AddHealthCheck = true;
            o.EnableTracing = true;
        });
        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IFieldEncryptor>().ShouldBeOfType<AesGcmFieldEncryptor>();
        scope.ServiceProvider.GetRequiredService<IEncryptionOrchestrator>().ShouldBeOfType<EncryptionOrchestrator>();
        scope.ServiceProvider.GetRequiredService<IOptions<EncryptionOptions>>().Value.EnableTracing.ShouldBeTrue();
        services.ShouldContain(d => d.ServiceType == typeof(IPipelineBehavior<,>)
            && d.ImplementationType == typeof(EncryptionPipelineBehavior<,>));
    }

    [Fact]
    public void AddEncinaEncryption_ApplicationKeyProviderWinsOverDefault()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var custom = new InMemoryKeyProvider();
        services.AddSingleton<IKeyProvider>(custom);

        services.AddEncinaEncryption();
        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IKeyProvider>().ShouldBeSameAs(custom);
    }

    [Fact]
    public void AddEncinaEncryption_ApplicationKeyProviderRegisteredAfterWins()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaEncryption();
        var custom = new InMemoryKeyProvider();
        services.Replace(ServiceDescriptor.Singleton<IKeyProvider>(custom));

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IKeyProvider>().ShouldBeSameAs(custom);
    }

    [Fact]
    public void AddEncinaEncryption_PipelineBehaviorResolvesAsClosedGeneric()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaEncryption();
        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateScopes = true });
        using var scope = provider.CreateScope();

        var behavior = scope.ServiceProvider.GetServices<IPipelineBehavior<ContractCommand, Unit>>();

        behavior.ShouldContain(b => b is EncryptionPipelineBehavior<ContractCommand, Unit>);
    }

    [Fact]
    public void EncryptionErrors_CarryCodeStageAndRelevantMetadata()
    {
        var notFound = EncryptionErrors.KeyNotFound("k1");
        var failed = EncryptionErrors.DecryptionFailed("k1", "Email", new CryptographicException("boom"));
        var invalid = EncryptionErrors.InvalidCiphertext("Email");
        var unsupported = EncryptionErrors.AlgorithmNotSupported(EncryptionAlgorithm.Aes256Gcm);
        var rotation = EncryptionErrors.KeyRotationFailed(new InvalidOperationException());

        notFound.GetCode().IfNone("").ShouldBe(EncryptionErrors.KeyNotFoundCode);
        notFound.Message.ShouldContain("k1");
        failed.GetCode().IfNone("").ShouldBe(EncryptionErrors.DecryptionFailedCode);
        failed.Message.ShouldContain("Email");
        failed.GetCause().IsSome.ShouldBeTrue();
        invalid.GetCode().IfNone("").ShouldBe(EncryptionErrors.InvalidCiphertextCode);
        invalid.Message.ShouldContain("Email");
        unsupported.GetCode().IfNone("").ShouldBe(EncryptionErrors.AlgorithmNotSupportedCode);
        unsupported.Message.ShouldContain("Aes256Gcm");
        rotation.GetCode().IfNone("").ShouldBe(EncryptionErrors.KeyRotationFailedCode);
        EncryptionErrors.DecryptionFailed("k1").Message.ShouldNotContain("property");
        EncryptionErrors.InvalidCiphertext().Message.ShouldNotContain("property");
    }

    [Fact]
    public void EncryptedValue_TagDefaultsToEmptyAndWithCopiesFields()
    {
        var a = new EncryptedValue { Ciphertext = [1, 2], Nonce = [3], Algorithm = EncryptionAlgorithm.Aes256Gcm, KeyId = "k" };

        a.Tag.IsEmpty.ShouldBeTrue();
        a.KeyId.ShouldBe("k");
        a.Algorithm.ShouldBe(EncryptionAlgorithm.Aes256Gcm);
        var copy = a with { KeyId = "other" };
        copy.KeyId.ShouldBe("other");
        copy.Ciphertext.ShouldBe(a.Ciphertext);
    }

    [Fact]
    public void EncryptionContext_DefaultsAreEmptyAndSupportWithExpressions()
    {
        var context = new EncryptionContext();
        var changed = context with { KeyId = "k", Purpose = "p", TenantId = "t" };

        context.KeyId.ShouldBeNull();
        context.Purpose.ShouldBeNull();
        context.TenantId.ShouldBeNull();
        context.AssociatedData.IsEmpty.ShouldBeTrue();
        changed.KeyId.ShouldBe("k");
        changed.Purpose.ShouldBe("p");
        changed.TenantId.ShouldBe("t");
    }

    [Fact]
    public void Attributes_ExposeDefaultsAndAreDiscoverableByReflection()
    {
        var encrypt = new EncryptAttribute();
        var incoming = new DecryptOnReceiveAttribute();
        var response = new EncryptedResponseAttribute();

        encrypt.Algorithm.ShouldBe(EncryptionAlgorithm.Aes256Gcm);
        encrypt.FailOnError.ShouldBeTrue();
        encrypt.Purpose.ShouldBeNull();
        encrypt.KeyId.ShouldBeNull();
        incoming.FailOnError.ShouldBeTrue();
        response.Algorithm.ShouldBe(EncryptionAlgorithm.Aes256Gcm);
        typeof(ContractModel).GetProperty(nameof(ContractModel.Email))!
            .GetCustomAttributes(typeof(EncryptAttribute), inherit: true).ShouldHaveSingleItem();
    }

    [Fact]
    public void EncryptedPropertyCache_ReturnsStableDescriptorsPerType()
    {
        var first = EncryptedPropertyCache.GetProperties(typeof(ContractModel));
        var second = EncryptedPropertyCache.GetProperties(typeof(ContractModel));

        first.ShouldBeSameAs(second);
        first.Select(p => p.Property.Name).ShouldBe([nameof(ContractModel.Email)]);
        EncryptedPropertyCache.GetProperties(typeof(ContractPlain)).ShouldBeEmpty();
    }

    #endregion

    private static T Unwrap<T>(Either<EncinaError, T> result) =>
        result.Match(Right: v => v, Left: e => throw new InvalidOperationException(e.GetCode().IfNone("none")));

    private static string? ErrorCode<T>(Either<EncinaError, T> result) =>
        result.Match(Right: _ => null, Left: e => e.GetCode().IfNone("none"));

    private static EncryptedValue StubValue() =>
        new()
        {
            Ciphertext = [1],
            Nonce = [2],
            Tag = [3],
            Algorithm = EncryptionAlgorithm.Aes256Gcm,
            KeyId = KeyId
        };

    #region Test Types

    private sealed class StubFieldEncryptor : IFieldEncryptor
    {
        private readonly Either<EncinaError, EncryptedValue> _encrypt;
        private readonly Either<EncinaError, string> _decrypt;

        public StubFieldEncryptor(Either<EncinaError, EncryptedValue> encrypt, Either<EncinaError, string> decrypt)
        {
            _encrypt = encrypt;
            _decrypt = decrypt;
        }

        public ValueTask<Either<EncinaError, EncryptedValue>> EncryptStringAsync(
            string plaintext, EncryptionContext context, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(_encrypt);

        public ValueTask<Either<EncinaError, string>> DecryptStringAsync(
            EncryptedValue encryptedValue, EncryptionContext context, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(_decrypt);

        public ValueTask<Either<EncinaError, EncryptedValue>> EncryptBytesAsync(
            ReadOnlyMemory<byte> plaintext, EncryptionContext context, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(_encrypt);

        public ValueTask<Either<EncinaError, byte[]>> DecryptBytesAsync(
            EncryptedValue encryptedValue, EncryptionContext context, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<Either<EncinaError, byte[]>>(Array.Empty<byte>());
    }

    public sealed class ContractModel
    {
        [Encrypt]
        public string? Email { get; set; }

        public string? Name { get; set; }
    }

    public sealed class ContractLenientModel
    {
        [Encrypt(FailOnError = false)]
        public string? Value { get; set; }
    }

    public sealed class ContractPurposeModel
    {
        [Encrypt(Purpose = "billing", KeyId = "billing-key")]
        public string? Value { get; set; }
    }

    public sealed class ContractPlain
    {
        public string? Name { get; set; }
    }

    public sealed class ContractCommand : IRequest<Unit>
    {
        [Encrypt]
        public string? Email { get; set; }
    }

    #endregion
}
