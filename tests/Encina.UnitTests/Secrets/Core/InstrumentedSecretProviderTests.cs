using System.Diagnostics;
using System.Diagnostics.Metrics;
using Encina.Secrets;
using Encina.Secrets.Diagnostics;
using Encina.TestInfrastructure.Extensions;
using FluentAssertions;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using NSubstitute;

namespace Encina.UnitTests.Secrets.Core;

/// <summary>
/// Unit tests for <see cref="InstrumentedSecretProvider"/>.
/// </summary>
/// <remarks>
/// Activity tracing tests register a global <see cref="ActivityListener"/> against the
/// static <see cref="SecretsActivitySource"/> source, so the tracing test uses a
/// try/finally block to ensure the listener is disposed and does not bleed into other tests.
/// Metrics tests use per-test <see cref="MeterListener"/> instances that are disposed
/// after each assertion, also avoiding cross-test interference.
/// </remarks>
public sealed class InstrumentedSecretProviderTests
{
    #region Constructor Guard Tests

    [Fact]
    public void Constructor_NullInner_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new SecretsInstrumentationOptions();
        var logger = NullLogger<InstrumentedSecretProvider>.Instance;

        // Act
        var act = () => new InstrumentedSecretProvider(null!, options, metrics: null, logger);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("inner");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var inner = Substitute.For<ISecretProvider>();
        var logger = NullLogger<InstrumentedSecretProvider>.Instance;

        // Act
        var act = () => new InstrumentedSecretProvider(inner, null!, metrics: null, logger);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var inner = Substitute.For<ISecretProvider>();
        var options = new SecretsInstrumentationOptions();

        // Act
        var act = () => new InstrumentedSecretProvider(inner, options, metrics: null, logger: null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void Constructor_NullMetrics_DoesNotThrow()
    {
        // Arrange
        var inner = Substitute.For<ISecretProvider>();
        var options = new SecretsInstrumentationOptions();
        var logger = NullLogger<InstrumentedSecretProvider>.Instance;

        // Act
        var act = () => new InstrumentedSecretProvider(inner, options, metrics: null, logger);

        // Assert - metrics is explicitly nullable, constructor must not throw
        act.Should().NotThrow();
    }

    #endregion

    #region Delegation Tests - All 6 ISecretProvider Methods

    [Fact]
    public async Task GetSecretAsync_DelegatesToInner_AndReturnsResult()
    {
        // Arrange
        var inner = Substitute.For<ISecretProvider>();
        var expected = new Secret("my-secret", "secret-value", "v1", null);
        var provider = CreateProvider(inner);

#pragma warning disable CA2012
        inner.GetSecretAsync("my-secret", Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Secret>>(Either<EncinaError, Secret>.Right(expected)));
#pragma warning restore CA2012

        // Act
        var result = await provider.GetSecretAsync("my-secret");

        // Assert
        result.ShouldBeSuccess(value => value.Should().Be(expected));
        await inner.Received(1).GetSecretAsync("my-secret", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetSecretVersionAsync_DelegatesToInner_AndReturnsResult()
    {
        // Arrange
        var inner = Substitute.For<ISecretProvider>();
        var expected = new Secret("my-secret", "secret-value", "v2", null);
        var provider = CreateProvider(inner);

#pragma warning disable CA2012
        inner.GetSecretVersionAsync("my-secret", "v2", Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Secret>>(Either<EncinaError, Secret>.Right(expected)));
#pragma warning restore CA2012

        // Act
        var result = await provider.GetSecretVersionAsync("my-secret", "v2");

        // Assert
        result.ShouldBeSuccess(value => value.Should().Be(expected));
        await inner.Received(1).GetSecretVersionAsync("my-secret", "v2", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetSecretAsync_DelegatesToInner_AndReturnsResult()
    {
        // Arrange
        var inner = Substitute.For<ISecretProvider>();
        var expectedMetadata = new SecretMetadata("my-secret", "v1", DateTime.UtcNow, null);
        var provider = CreateProvider(inner);

#pragma warning disable CA2012
        inner.SetSecretAsync("my-secret", "value", Arg.Any<SecretOptions?>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, SecretMetadata>>(
                Either<EncinaError, SecretMetadata>.Right(expectedMetadata)));
#pragma warning restore CA2012

        // Act
        var result = await provider.SetSecretAsync("my-secret", "value");

        // Assert
        result.ShouldBeSuccess(value => value.Should().Be(expectedMetadata));
        await inner.Received(1).SetSecretAsync("my-secret", "value", Arg.Any<SecretOptions?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteSecretAsync_DelegatesToInner_AndReturnsResult()
    {
        // Arrange
        var inner = Substitute.For<ISecretProvider>();
        var provider = CreateProvider(inner);

#pragma warning disable CA2012
        inner.DeleteSecretAsync("my-secret", Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Unit>>(Either<EncinaError, Unit>.Right(Unit.Default)));
#pragma warning restore CA2012

        // Act
        var result = await provider.DeleteSecretAsync("my-secret");

        // Assert
        result.ShouldBeSuccess();
        await inner.Received(1).DeleteSecretAsync("my-secret", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListSecretsAsync_DelegatesToInner_AndReturnsResult()
    {
        // Arrange
        var inner = Substitute.For<ISecretProvider>();
        var expectedNames = new[] { "secret-a", "secret-b" };
        var provider = CreateProvider(inner);

#pragma warning disable CA2012
        inner.ListSecretsAsync(Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, IEnumerable<string>>>(
                Either<EncinaError, IEnumerable<string>>.Right(expectedNames)));
#pragma warning restore CA2012

        // Act
        var result = await provider.ListSecretsAsync();

        // Assert
        result.ShouldBeSuccess(value => value.Should().BeEquivalentTo(expectedNames));
        await inner.Received(1).ListSecretsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExistsAsync_DelegatesToInner_AndReturnsResult()
    {
        // Arrange
        var inner = Substitute.For<ISecretProvider>();
        var provider = CreateProvider(inner);

#pragma warning disable CA2012
        inner.ExistsAsync("my-secret", Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, bool>>(Either<EncinaError, bool>.Right(true)));
#pragma warning restore CA2012

        // Act
        var result = await provider.ExistsAsync("my-secret");

        // Assert
        result.ShouldBeSuccess(value => value.Should().BeTrue());
        await inner.Received(1).ExistsAsync("my-secret", Arg.Any<CancellationToken>());
    }

    #endregion

    #region Right Result (Success Path) Tests

    [Fact]
    public async Task GetSecretAsync_RightResult_ReturnsUnchangedSuccess()
    {
        // Arrange
        var inner = Substitute.For<ISecretProvider>();
        var expected = new Secret("api-key", "super-secret", "v1", null);
        var provider = CreateProvider(inner);

#pragma warning disable CA2012
        inner.GetSecretAsync("api-key", Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Secret>>(Either<EncinaError, Secret>.Right(expected)));
#pragma warning restore CA2012

        // Act
        var result = await provider.GetSecretAsync("api-key");

        // Assert - the Either result must pass through unchanged, preserving the secret value
        result.IsRight.Should().BeTrue();
        result.Match(
            Right: value =>
            {
                value.Name.Should().Be("api-key");
                value.Value.Should().Be("super-secret");
                value.Version.Should().Be("v1");
            },
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    #endregion

    #region Left Result (Error Path) Tests

    [Fact]
    public async Task GetSecretAsync_LeftResult_ReturnsUnchangedError()
    {
        // Arrange
        var inner = Substitute.For<ISecretProvider>();
        var error = SecretsErrorCodes.NotFound("missing-secret");
        var logger = new FakeLogger<InstrumentedSecretProvider>();
        var provider = CreateProviderWithLogger(inner, logger);

#pragma warning disable CA2012
        inner.GetSecretAsync("missing-secret", Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Secret>>(Either<EncinaError, Secret>.Left(error)));
#pragma warning restore CA2012

        // Act
        var result = await provider.GetSecretAsync("missing-secret");

        // Assert - error is propagated unchanged to the caller
        result.IsLeft.Should().BeTrue();
        _ = result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: err => { err.Message.Should().Be(error.Message); return Unit.Default; });
    }

    [Fact]
    public async Task GetSecretAsync_LeftResult_LogsWarningWithErrorCode()
    {
        // Arrange
        var inner = Substitute.For<ISecretProvider>();
        var error = SecretsErrorCodes.NotFound("missing-secret");
        var logger = new FakeLogger<InstrumentedSecretProvider>();
        var provider = CreateProviderWithLogger(inner, logger);

#pragma warning disable CA2012
        inner.GetSecretAsync("missing-secret", Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Secret>>(Either<EncinaError, Secret>.Left(error)));
#pragma warning restore CA2012

        // Act
        await provider.GetSecretAsync("missing-secret");

        // Assert - a Warning-level log is emitted containing the operation name and error code
        var records = logger.Collector.GetSnapshot();
        records.Should().Contain(r =>
            r.Level == LogLevel.Warning &&
            r.Message.Contains("get") &&
            r.Message.Contains(SecretsErrorCodes.NotFoundCode));
    }

    [Fact]
    public async Task GetSecretAsync_LeftResultWithOperationFailed_LogsWarningWithCorrectCode()
    {
        // Arrange
        var inner = Substitute.For<ISecretProvider>();
        var error = SecretsErrorCodes.OperationFailed("get", "Provider timeout");
        var logger = new FakeLogger<InstrumentedSecretProvider>();
        var provider = CreateProviderWithLogger(inner, logger);

#pragma warning disable CA2012
        inner.GetSecretAsync("my-secret", Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Secret>>(Either<EncinaError, Secret>.Left(error)));
#pragma warning restore CA2012

        // Act
        await provider.GetSecretAsync("my-secret");

        // Assert - the operation-failed code appears in the warning log
        var records = logger.Collector.GetSnapshot();
        records.Should().Contain(r =>
            r.Level == LogLevel.Warning &&
            r.Message.Contains(SecretsErrorCodes.OperationFailedCode));
    }

    [Fact]
    public async Task GetSecretAsync_LeftResultWithNoCode_LogsUnknownFallbackCode()
    {
        // Arrange - plain EncinaError.New has no EncinaException metadata, so GetCode() returns None
        var inner = Substitute.For<ISecretProvider>();
        var error = EncinaError.New("some generic error without a structured code");
        var logger = new FakeLogger<InstrumentedSecretProvider>();
        var provider = CreateProviderWithLogger(inner, logger);

#pragma warning disable CA2012
        inner.GetSecretAsync("my-secret", Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Secret>>(Either<EncinaError, Secret>.Left(error)));
#pragma warning restore CA2012

        // Act
        await provider.GetSecretAsync("my-secret");

        // Assert - error.GetCode().IfNone("unknown") falls back to "unknown"
        var records = logger.Collector.GetSnapshot();
        records.Should().Contain(r =>
            r.Level == LogLevel.Warning &&
            r.Message.Contains("unknown"));
    }

    #endregion

    #region RecordSecretNames=false Tests

    [Fact]
    public async Task GetSecretAsync_RecordSecretNamesFalse_MetricsDoNotIncludeSecretNameTag()
    {
        // Arrange
        var inner = Substitute.For<ISecretProvider>();
        var options = new SecretsInstrumentationOptions
        {
            RecordSecretNames = false,
            EnableTracing = false,
            EnableMetrics = true
        };

        var operationTags = new List<List<KeyValuePair<string, object?>>>();

        using var meterListener = new MeterListener();
        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == SecretsMetrics.MeterName)
                listener.EnableMeasurementEvents(instrument);
        };
        meterListener.SetMeasurementEventCallback<long>((instrument, _, tags, _) =>
        {
            if (instrument.Name == "encina.secrets.operations")
                operationTags.Add(ReadTags(tags));
        });
        meterListener.Start();

        var metrics = CreateMetrics();
        var logger = NullLogger<InstrumentedSecretProvider>.Instance;
        var provider = new InstrumentedSecretProvider(inner, options, metrics, logger);

#pragma warning disable CA2012
        inner.GetSecretAsync("top-secret", Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Secret>>(
                Either<EncinaError, Secret>.Right(new Secret("top-secret", "value", null, null))));
#pragma warning restore CA2012

        // Act
        await provider.GetSecretAsync("top-secret");

        // Assert - the sanitizedName is null (RecordSecretNames=false), so no secrets.name tag
        operationTags.Should().NotBeEmpty();
        operationTags.Should().AllSatisfy(tags =>
            tags.Should().NotContain(t => t.Key == "secrets.name"));
    }

    #endregion

    #region RecordSecretNames=true Tests

    [Fact]
    public async Task GetSecretAsync_RecordSecretNamesTrue_MetricsIncludeSecretNameTag()
    {
        // Arrange
        var inner = Substitute.For<ISecretProvider>();
        var options = new SecretsInstrumentationOptions
        {
            RecordSecretNames = true,
            EnableTracing = false,
            EnableMetrics = true
        };

        var operationTags = new List<List<KeyValuePair<string, object?>>>();

        using var meterListener = new MeterListener();
        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == SecretsMetrics.MeterName)
                listener.EnableMeasurementEvents(instrument);
        };
        meterListener.SetMeasurementEventCallback<long>((instrument, _, tags, _) =>
        {
            if (instrument.Name == "encina.secrets.operations")
                operationTags.Add(ReadTags(tags));
        });
        meterListener.Start();

        var metrics = CreateMetrics();
        var logger = NullLogger<InstrumentedSecretProvider>.Instance;
        var provider = new InstrumentedSecretProvider(inner, options, metrics, logger);

#pragma warning disable CA2012
        inner.GetSecretAsync("named-secret", Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Secret>>(
                Either<EncinaError, Secret>.Right(new Secret("named-secret", "value", null, null))));
#pragma warning restore CA2012

        // Act
        await provider.GetSecretAsync("named-secret");

        // Assert - sanitizedName = secretName because RecordSecretNames=true
        operationTags.Should().NotBeEmpty();
        operationTags.Should().Contain(tags =>
            tags.Any(t => t.Key == "secrets.name" && t.Value != null && t.Value.ToString() == "named-secret"));
    }

    #endregion

    #region EnableMetrics=false Tests

    [Fact]
    public async Task GetSecretAsync_EnableMetricsFalse_SuccessDoesNotRecordAnyMetrics()
    {
        // Arrange - when EnableMetrics=false the DI registration passes null for the metrics parameter
        var inner = Substitute.For<ISecretProvider>();
        var options = new SecretsInstrumentationOptions
        {
            RecordSecretNames = false,
            EnableTracing = false,
            EnableMetrics = false
        };

        var instrumentNames = new List<string>();

        using var meterListener = new MeterListener();
        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == SecretsMetrics.MeterName)
                listener.EnableMeasurementEvents(instrument);
        };
        meterListener.SetMeasurementEventCallback<long>((instrument, _, _, _) =>
            instrumentNames.Add(instrument.Name));
        meterListener.SetMeasurementEventCallback<double>((instrument, _, _, _) =>
            instrumentNames.Add(instrument.Name));
        meterListener.Start();

        var logger = NullLogger<InstrumentedSecretProvider>.Instance;
        var provider = new InstrumentedSecretProvider(inner, options, metrics: null, logger);

#pragma warning disable CA2012
        inner.GetSecretAsync("my-secret", Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Secret>>(
                Either<EncinaError, Secret>.Right(new Secret("my-secret", "value", null, null))));
#pragma warning restore CA2012

        // Act
        await provider.GetSecretAsync("my-secret");

        // Assert - no metrics counters or histograms were touched (metrics is null)
        instrumentNames.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSecretAsync_EnableMetricsFalse_ErrorDoesNotRecordAnyMetrics()
    {
        // Arrange
        var inner = Substitute.For<ISecretProvider>();
        var options = new SecretsInstrumentationOptions
        {
            RecordSecretNames = false,
            EnableTracing = false,
            EnableMetrics = false
        };

        var instrumentNames = new List<string>();

        using var meterListener = new MeterListener();
        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == SecretsMetrics.MeterName)
                listener.EnableMeasurementEvents(instrument);
        };
        meterListener.SetMeasurementEventCallback<long>((instrument, _, _, _) =>
            instrumentNames.Add(instrument.Name));
        meterListener.SetMeasurementEventCallback<double>((instrument, _, _, _) =>
            instrumentNames.Add(instrument.Name));
        meterListener.Start();

        var logger = NullLogger<InstrumentedSecretProvider>.Instance;
        var provider = new InstrumentedSecretProvider(inner, options, metrics: null, logger);
        var error = SecretsErrorCodes.NotFound("my-secret");

#pragma warning disable CA2012
        inner.GetSecretAsync("my-secret", Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Secret>>(Either<EncinaError, Secret>.Left(error)));
#pragma warning restore CA2012

        // Act
        await provider.GetSecretAsync("my-secret");

        // Assert - even on error, no metrics recorded when metrics is null
        instrumentNames.Should().BeEmpty();
    }

    #endregion

    #region EnableMetrics=true Tests

    [Fact]
    public async Task GetSecretAsync_SuccessWithMetrics_RecordsOperationsCounterAndDurationHistogram()
    {
        // Arrange
        var inner = Substitute.For<ISecretProvider>();
        var options = new SecretsInstrumentationOptions
        {
            RecordSecretNames = false,
            EnableTracing = false,
            EnableMetrics = true
        };

        var counterNames = new List<string>();
        var histogramNames = new List<string>();

        using var meterListener = new MeterListener();
        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == SecretsMetrics.MeterName)
                listener.EnableMeasurementEvents(instrument);
        };
        meterListener.SetMeasurementEventCallback<long>((instrument, _, _, _) =>
            counterNames.Add(instrument.Name));
        meterListener.SetMeasurementEventCallback<double>((instrument, _, _, _) =>
            histogramNames.Add(instrument.Name));
        meterListener.Start();

        var metrics = CreateMetrics();
        var logger = NullLogger<InstrumentedSecretProvider>.Instance;
        var provider = new InstrumentedSecretProvider(inner, options, metrics, logger);

#pragma warning disable CA2012
        inner.GetSecretAsync("my-secret", Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Secret>>(
                Either<EncinaError, Secret>.Right(new Secret("my-secret", "value", null, null))));
#pragma warning restore CA2012

        // Act
        await provider.GetSecretAsync("my-secret");

        // Assert - success path calls RecordSuccess: operations counter + duration histogram
        counterNames.Should().Contain("encina.secrets.operations");
        histogramNames.Should().Contain("encina.secrets.duration");
        // Error counter must NOT be incremented on a successful result
        counterNames.Should().NotContain("encina.secrets.errors");
    }

    [Fact]
    public async Task GetSecretAsync_ErrorWithMetrics_RecordsOperationsCounterDurationAndErrorsCounter()
    {
        // Arrange
        var inner = Substitute.For<ISecretProvider>();
        var options = new SecretsInstrumentationOptions
        {
            RecordSecretNames = false,
            EnableTracing = false,
            EnableMetrics = true
        };

        var counterNames = new List<string>();
        var histogramNames = new List<string>();

        using var meterListener = new MeterListener();
        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == SecretsMetrics.MeterName)
                listener.EnableMeasurementEvents(instrument);
        };
        meterListener.SetMeasurementEventCallback<long>((instrument, _, _, _) =>
            counterNames.Add(instrument.Name));
        meterListener.SetMeasurementEventCallback<double>((instrument, _, _, _) =>
            histogramNames.Add(instrument.Name));
        meterListener.Start();

        var metrics = CreateMetrics();
        var logger = NullLogger<InstrumentedSecretProvider>.Instance;
        var provider = new InstrumentedSecretProvider(inner, options, metrics, logger);
        var error = SecretsErrorCodes.NotFound("my-secret");

#pragma warning disable CA2012
        inner.GetSecretAsync("my-secret", Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Secret>>(Either<EncinaError, Secret>.Left(error)));
#pragma warning restore CA2012

        // Act
        await provider.GetSecretAsync("my-secret");

        // Assert - error path calls RecordError: operations counter + duration histogram + errors counter
        counterNames.Should().Contain("encina.secrets.operations");
        counterNames.Should().Contain("encina.secrets.errors");
        histogramNames.Should().Contain("encina.secrets.duration");
    }

    #endregion

    #region EnableTracing=false Tests

    [Fact]
    public async Task GetSecretAsync_EnableTracingFalse_NoActivityCreated()
    {
        // Arrange
        var inner = Substitute.For<ISecretProvider>();
        var options = new SecretsInstrumentationOptions
        {
            RecordSecretNames = false,
            EnableTracing = false,
            EnableMetrics = false
        };

        var completedActivities = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == SecretsActivitySource.Name,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity => completedActivities.Add(activity)
        };
        ActivitySource.AddActivityListener(listener);

        try
        {
            var logger = NullLogger<InstrumentedSecretProvider>.Instance;
            var provider = new InstrumentedSecretProvider(inner, options, metrics: null, logger);

#pragma warning disable CA2012
            inner.GetSecretAsync("my-secret", Arg.Any<CancellationToken>())
                .Returns(new ValueTask<Either<EncinaError, Secret>>(
                    Either<EncinaError, Secret>.Right(new Secret("my-secret", "value", null, null))));
#pragma warning restore CA2012

            // Act
            await provider.GetSecretAsync("my-secret");

            // Assert - InstrumentAsync uses null activity when EnableTracing=false
            completedActivities.Should().BeEmpty();
        }
        finally
        {
            listener.Dispose();
        }
    }

    #endregion

    #region ListSecretsAsync - No Secret Name Tests

    [Fact]
    public async Task ListSecretsAsync_RecordSecretNamesTrue_SecretNameTagIsNeverRecorded()
    {
        // Arrange - ListSecretsAsync passes secretName=null to InstrumentAsync regardless of RecordSecretNames,
        // because there is no individual secret name for a list operation.
        var inner = Substitute.For<ISecretProvider>();
        var options = new SecretsInstrumentationOptions
        {
            RecordSecretNames = true,
            EnableTracing = false,
            EnableMetrics = true
        };

        var operationTags = new List<List<KeyValuePair<string, object?>>>();

        using var meterListener = new MeterListener();
        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == SecretsMetrics.MeterName)
                listener.EnableMeasurementEvents(instrument);
        };
        meterListener.SetMeasurementEventCallback<long>((instrument, _, tags, _) =>
        {
            if (instrument.Name == "encina.secrets.operations")
                operationTags.Add(ReadTags(tags));
        });
        meterListener.Start();

        var metrics = CreateMetrics();
        var logger = NullLogger<InstrumentedSecretProvider>.Instance;
        var provider = new InstrumentedSecretProvider(inner, options, metrics, logger);

#pragma warning disable CA2012
        inner.ListSecretsAsync(Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, IEnumerable<string>>>(
                Either<EncinaError, IEnumerable<string>>.Right([])));
#pragma warning restore CA2012

        // Act
        await provider.ListSecretsAsync();

        // Assert - even with RecordSecretNames=true, list has no individual name to record
        operationTags.Should().NotBeEmpty();
        operationTags.Should().AllSatisfy(tags =>
            tags.Should().NotContain(t => t.Key == "secrets.name"));
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Creates a provider with all observability disabled - for delegation and result pass-through tests.
    /// </summary>
    private static InstrumentedSecretProvider CreateProvider(ISecretProvider inner)
    {
        var options = new SecretsInstrumentationOptions
        {
            RecordSecretNames = false,
            EnableTracing = false,
            EnableMetrics = false
        };
        var logger = NullLogger<InstrumentedSecretProvider>.Instance;
        return new InstrumentedSecretProvider(inner, options, metrics: null, logger);
    }

    /// <summary>
    /// Creates a provider with a specific logger, all observability disabled - for logging tests.
    /// </summary>
    private static InstrumentedSecretProvider CreateProviderWithLogger(
        ISecretProvider inner,
        ILogger<InstrumentedSecretProvider> logger)
    {
        var options = new SecretsInstrumentationOptions
        {
            RecordSecretNames = false,
            EnableTracing = false,
            EnableMetrics = false
        };
        return new InstrumentedSecretProvider(inner, options, metrics: null, logger);
    }

    /// <summary>
    /// Creates a real <see cref="SecretsMetrics"/> instance backed by a DI-managed
    /// <see cref="IMeterFactory"/>. The global <see cref="MeterListener"/> captures
    /// measurements from meters created by any factory.
    /// </summary>
    private static SecretsMetrics CreateMetrics()
    {
        var services = new ServiceCollection();
        services.AddMetrics();
        var sp = services.BuildServiceProvider();
        var meterFactory = sp.GetRequiredService<IMeterFactory>();
        return new SecretsMetrics(meterFactory);
    }

    /// <summary>
    /// Converts a <see cref="ReadOnlySpan{T}"/> of tags into an
    /// <see cref="IReadOnlyList{T}"/> so that it can be captured by closures and
    /// asserted after the callback returns.
    /// </summary>
    private static List<KeyValuePair<string, object?>> ReadTags(
        ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        var list = new List<KeyValuePair<string, object?>>(tags.Length);
        foreach (var tag in tags)
        {
            list.Add(tag);
        }

        return list;
    }

    #endregion
}
