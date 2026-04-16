#pragma warning disable CA2012 // Use ValueTasks correctly (NSubstitute Returns with ValueTask)

using Encina.Caching;
using Encina.Compliance.NIS2;
using Encina.Compliance.NIS2.Abstractions;
using Encina.Compliance.NIS2.Model;
using Encina.Testing.Time;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.NIS2;

/// <summary>
/// Unit tests for <see cref="DefaultNIS2ComplianceValidator"/>.
/// </summary>
public class DefaultNIS2ComplianceValidatorTests
{
    private readonly FakeTimeProvider _timeProvider = new(new DateTimeOffset(2026, 3, 1, 12, 0, 0, TimeSpan.Zero));
    private readonly IServiceProvider _serviceProvider = Substitute.For<IServiceProvider>();
    private readonly ILogger<DefaultNIS2ComplianceValidator> _logger = NullLogger<DefaultNIS2ComplianceValidator>.Instance;

    private static NIS2Options CreateDefaultOptions() => new()
    {
        EntityType = NIS2EntityType.Essential,
        Sector = NIS2Sector.DigitalInfrastructure
    };

    private DefaultNIS2ComplianceValidator CreateSut(
        IEnumerable<INIS2MeasureEvaluator> evaluators,
        NIS2Options? options = null) =>
        new(
            evaluators,
            Options.Create(options ?? CreateDefaultOptions()),
            _timeProvider,
            _serviceProvider,
            _logger);

    private static INIS2MeasureEvaluator CreateSatisfiedEvaluator(NIS2Measure measure)
    {
        var evaluator = Substitute.For<INIS2MeasureEvaluator>();
        evaluator.Measure.Returns(measure);
        evaluator.EvaluateAsync(Arg.Any<NIS2MeasureContext>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => new ValueTask<Either<EncinaError, NIS2MeasureResult>>(
                Right<EncinaError, NIS2MeasureResult>(
                    NIS2MeasureResult.Satisfied(measure, "OK"))));
        return evaluator;
    }

    private static INIS2MeasureEvaluator CreateNotSatisfiedEvaluator(NIS2Measure measure)
    {
        var evaluator = Substitute.For<INIS2MeasureEvaluator>();
        evaluator.Measure.Returns(measure);
        evaluator.EvaluateAsync(Arg.Any<NIS2MeasureContext>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => new ValueTask<Either<EncinaError, NIS2MeasureResult>>(
                Right<EncinaError, NIS2MeasureResult>(
                    NIS2MeasureResult.NotSatisfied(measure, "Not met", ["Fix it"]))));
        return evaluator;
    }

    #region ValidateAsync

    [Fact]
    public async Task ValidateAsync_AllEvaluatorsPass_ShouldBeCompliant()
    {
        // Arrange
        var evaluators = new[]
        {
            CreateSatisfiedEvaluator(NIS2Measure.RiskAnalysisAndSecurityPolicies),
            CreateSatisfiedEvaluator(NIS2Measure.IncidentHandling),
            CreateSatisfiedEvaluator(NIS2Measure.BusinessContinuity)
        };
        var sut = CreateSut(evaluators);

        // Act
        var result = await sut.ValidateAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        var compliance = result.Match(r => r, _ => null!);
        compliance.IsCompliant.ShouldBeTrue();
        compliance.MeasureResults.Count.ShouldBe(3);
        compliance.MissingMeasures.ShouldBeEmpty();
        compliance.CompliancePercentage.ShouldBe(100);
        compliance.EntityType.ShouldBe(NIS2EntityType.Essential);
        compliance.Sector.ShouldBe(NIS2Sector.DigitalInfrastructure);
    }

    [Fact]
    public async Task ValidateAsync_SomeEvaluatorsFail_ShouldNotBeCompliant()
    {
        // Arrange
        var evaluators = new[]
        {
            CreateSatisfiedEvaluator(NIS2Measure.RiskAnalysisAndSecurityPolicies),
            CreateNotSatisfiedEvaluator(NIS2Measure.IncidentHandling),
            CreateSatisfiedEvaluator(NIS2Measure.BusinessContinuity)
        };
        var sut = CreateSut(evaluators);

        // Act
        var result = await sut.ValidateAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        var compliance = result.Match(r => r, _ => null!);
        compliance.IsCompliant.ShouldBeFalse();
        compliance.MeasureResults.Count.ShouldBe(3);
        compliance.MissingMeasures.ShouldHaveSingleItem().ShouldBe(NIS2Measure.IncidentHandling);
        compliance.MissingCount.ShouldBe(1);
    }

    [Fact]
    public async Task ValidateAsync_EvaluatorThrows_ShouldTreatAsNotSatisfied()
    {
        // Arrange
        var throwingEvaluator = Substitute.For<INIS2MeasureEvaluator>();
        throwingEvaluator.Measure.Returns(NIS2Measure.CyberHygiene);
        throwingEvaluator.EvaluateAsync(Arg.Any<NIS2MeasureContext>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Evaluator crashed"));

        var evaluators = new[]
        {
            CreateSatisfiedEvaluator(NIS2Measure.RiskAnalysisAndSecurityPolicies),
            throwingEvaluator,
            CreateSatisfiedEvaluator(NIS2Measure.BusinessContinuity)
        };
        var sut = CreateSut(evaluators);

        // Act
        var result = await sut.ValidateAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        var compliance = result.Match(r => r, _ => null!);
        compliance.IsCompliant.ShouldBeFalse();
        compliance.MissingMeasures.ShouldContain(NIS2Measure.CyberHygiene);
        var failedResult = compliance.MeasureResults.Single(m => m.Measure == NIS2Measure.CyberHygiene);
        failedResult.IsSatisfied.ShouldBeFalse();
        failedResult.Details.ShouldContain("Evaluator crashed");
    }

    #endregion

    #region GetMissingRequirementsAsync

    [Fact]
    public async Task GetMissingRequirementsAsync_ShouldReturnMissingMeasures()
    {
        // Arrange
        var evaluators = new[]
        {
            CreateSatisfiedEvaluator(NIS2Measure.RiskAnalysisAndSecurityPolicies),
            CreateNotSatisfiedEvaluator(NIS2Measure.IncidentHandling),
            CreateNotSatisfiedEvaluator(NIS2Measure.Cryptography)
        };
        var sut = CreateSut(evaluators);

        // Act
        var result = await sut.GetMissingRequirementsAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        var missing = result.Match(r => r, _ => null!);
        missing.Count.ShouldBe(2);
        missing.ShouldContain(NIS2Measure.IncidentHandling);
        missing.ShouldContain(NIS2Measure.Cryptography);
    }

    #endregion

    #region Caching Integration

    [Fact]
    public async Task ValidateAsync_CacheHit_ShouldReturnCachedWithoutEvaluating()
    {
        // Arrange — set up cache to return a pre-existing result
        var cachedResult = NIS2ComplianceResult.Create(
            NIS2EntityType.Essential,
            NIS2Sector.DigitalInfrastructure,
            [NIS2MeasureResult.Satisfied(NIS2Measure.RiskAnalysisAndSecurityPolicies, "cached")],
            DateTimeOffset.UtcNow);

        var cache = Substitute.For<ICacheProvider>();
        cache.GetAsync<NIS2ComplianceResult>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<NIS2ComplianceResult?>(cachedResult));

        var sp = Substitute.For<IServiceProvider>();
        sp.GetService(typeof(ICacheProvider)).Returns(cache);

        var evaluator = CreateSatisfiedEvaluator(NIS2Measure.RiskAnalysisAndSecurityPolicies);
        var options = CreateDefaultOptions();
        options.ComplianceCacheTTL = TimeSpan.FromMinutes(5);
        var sut = CreateSut([evaluator], options);
        // Replace service provider with one that has cache
        sut = new DefaultNIS2ComplianceValidator(
            [evaluator], Options.Create(options), _timeProvider, sp, _logger);

        // Act
        var result = await sut.ValidateAsync();

        // Assert — returned cached result, evaluator not called
        result.IsRight.ShouldBeTrue();
        await evaluator.DidNotReceive().EvaluateAsync(
            Arg.Any<NIS2MeasureContext>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ValidateAsync_CacheMiss_ShouldEvaluateAndCache()
    {
        // Arrange — cache returns null (miss)
        var cache = Substitute.For<ICacheProvider>();
        cache.GetAsync<NIS2ComplianceResult>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<NIS2ComplianceResult?>(null));

        var sp = Substitute.For<IServiceProvider>();
        sp.GetService(typeof(ICacheProvider)).Returns(cache);

        var evaluator = CreateSatisfiedEvaluator(NIS2Measure.RiskAnalysisAndSecurityPolicies);
        var options = CreateDefaultOptions();
        options.ComplianceCacheTTL = TimeSpan.FromMinutes(5);
        var sut = new DefaultNIS2ComplianceValidator(
            [evaluator], Options.Create(options), _timeProvider, sp, _logger);

        // Act
        var result = await sut.ValidateAsync();

        // Assert — evaluator WAS called
        result.IsRight.ShouldBeTrue();
        await evaluator.Received(1).EvaluateAsync(
            Arg.Any<NIS2MeasureContext>(), Arg.Any<CancellationToken>());

        // Assert — result WAS cached
        await cache.Received(1).SetAsync(
            Arg.Any<string>(),
            Arg.Any<NIS2ComplianceResult>(),
            Arg.Is<TimeSpan?>(t => t == TimeSpan.FromMinutes(5)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ValidateAsync_NoCacheProvider_ShouldEvaluateWithoutCaching()
    {
        // Arrange — no ICacheProvider registered
        var evaluator = CreateSatisfiedEvaluator(NIS2Measure.RiskAnalysisAndSecurityPolicies);
        var sut = CreateSut([evaluator]);

        // Act
        var result = await sut.ValidateAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        await evaluator.Received(1).EvaluateAsync(
            Arg.Any<NIS2MeasureContext>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ValidateAsync_ZeroCacheTTL_ShouldNotAttemptCaching()
    {
        // Arrange — ComplianceCacheTTL = 0 means caching disabled
        var cache = Substitute.For<ICacheProvider>();
        var sp = Substitute.For<IServiceProvider>();
        sp.GetService(typeof(ICacheProvider)).Returns(cache);

        var evaluator = CreateSatisfiedEvaluator(NIS2Measure.RiskAnalysisAndSecurityPolicies);
        var options = CreateDefaultOptions();
        options.ComplianceCacheTTL = TimeSpan.Zero;
        var sut = new DefaultNIS2ComplianceValidator(
            [evaluator], Options.Create(options), _timeProvider, sp, _logger);

        // Act
        await sut.ValidateAsync();

        // Assert — cache never touched even though it's registered
        await cache.DidNotReceive().GetAsync<NIS2ComplianceResult>(
            Arg.Any<string>(), Arg.Any<CancellationToken>());
        await cache.DidNotReceive().SetAsync(
            Arg.Any<string>(), Arg.Any<NIS2ComplianceResult>(),
            Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ValidateAsync_CacheWriteFails_ShouldStillReturnResult()
    {
        // Arrange — cache get returns null, cache set throws
        var cache = Substitute.For<ICacheProvider>();
        cache.GetAsync<NIS2ComplianceResult>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<NIS2ComplianceResult?>(null));
        cache.SetAsync(Arg.Any<string>(), Arg.Any<NIS2ComplianceResult>(),
                Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Redis down"));

        var sp = Substitute.For<IServiceProvider>();
        sp.GetService(typeof(ICacheProvider)).Returns(cache);

        var evaluator = CreateSatisfiedEvaluator(NIS2Measure.RiskAnalysisAndSecurityPolicies);
        var options = CreateDefaultOptions();
        options.ComplianceCacheTTL = TimeSpan.FromMinutes(5);
        var sut = new DefaultNIS2ComplianceValidator(
            [evaluator], Options.Create(options), _timeProvider, sp, _logger);

        // Act — cache write failure should not affect the result
        var result = await sut.ValidateAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    #endregion

    #region Multi-Tenancy Integration

    [Fact]
    public async Task ValidateAsync_WithTenantId_ShouldIncludeTenantInCacheKey()
    {
        // Arrange — IRequestContext provides TenantId
        var requestContext = Substitute.For<IRequestContext>();
        requestContext.TenantId.Returns("tenant-42");

        var cache = Substitute.For<ICacheProvider>();
        cache.GetAsync<NIS2ComplianceResult>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<NIS2ComplianceResult?>(null));

        var sp = Substitute.For<IServiceProvider>();
        sp.GetService(typeof(ICacheProvider)).Returns(cache);
        sp.GetService(typeof(IRequestContext)).Returns(requestContext);

        var evaluator = CreateSatisfiedEvaluator(NIS2Measure.RiskAnalysisAndSecurityPolicies);
        var options = CreateDefaultOptions();
        options.ComplianceCacheTTL = TimeSpan.FromMinutes(5);
        var sut = new DefaultNIS2ComplianceValidator(
            [evaluator], Options.Create(options), _timeProvider, sp, _logger);

        // Act
        await sut.ValidateAsync();

        // Assert — cache key should contain tenant-42
        await cache.Received(1).GetAsync<NIS2ComplianceResult>(
            Arg.Is<string>(k => k.Contains("tenant-42")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ValidateAsync_WithoutTenantId_ShouldUseCacheKeyWithoutTenant()
    {
        // Arrange — no IRequestContext registered
        var cache = Substitute.For<ICacheProvider>();
        cache.GetAsync<NIS2ComplianceResult>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<NIS2ComplianceResult?>(null));

        var sp = Substitute.For<IServiceProvider>();
        sp.GetService(typeof(ICacheProvider)).Returns(cache);
        // No IRequestContext

        var evaluator = CreateSatisfiedEvaluator(NIS2Measure.RiskAnalysisAndSecurityPolicies);
        var options = CreateDefaultOptions();
        options.ComplianceCacheTTL = TimeSpan.FromMinutes(5);
        var sut = new DefaultNIS2ComplianceValidator(
            [evaluator], Options.Create(options), _timeProvider, sp, _logger);

        // Act
        await sut.ValidateAsync();

        // Assert — cache key should NOT contain "tenant"
        await cache.Received(1).GetAsync<NIS2ComplianceResult>(
            Arg.Is<string>(k => !k.Contains("tenant") && k.Contains("Essential") && k.Contains("DigitalInfrastructure")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ValidateAsync_WithTenantId_ShouldPropagateToContext()
    {
        // Arrange — IRequestContext provides TenantId, capture context
        var requestContext = Substitute.For<IRequestContext>();
        requestContext.TenantId.Returns("tenant-abc");

        var sp = Substitute.For<IServiceProvider>();
        sp.GetService(typeof(IRequestContext)).Returns(requestContext);

        NIS2MeasureContext? capturedContext = null;
        var evaluator = Substitute.For<INIS2MeasureEvaluator>();
        evaluator.Measure.Returns(NIS2Measure.RiskAnalysisAndSecurityPolicies);
        evaluator.EvaluateAsync(Arg.Any<NIS2MeasureContext>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                capturedContext = callInfo.Arg<NIS2MeasureContext>();
                return new ValueTask<Either<EncinaError, NIS2MeasureResult>>(
                    Right<EncinaError, NIS2MeasureResult>(
                        NIS2MeasureResult.Satisfied(NIS2Measure.RiskAnalysisAndSecurityPolicies, "OK")));
            });

        var sut = new DefaultNIS2ComplianceValidator(
            [evaluator], Options.Create(CreateDefaultOptions()), _timeProvider, sp, _logger);

        // Act
        await sut.ValidateAsync();

        // Assert — TenantId should be propagated to the evaluator context
        capturedContext.ShouldNotBeNull();
        capturedContext!.TenantId.ShouldBe("tenant-abc");
    }

    #endregion
}
