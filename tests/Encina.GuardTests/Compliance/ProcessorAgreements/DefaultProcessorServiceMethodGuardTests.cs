using Encina.Compliance.ProcessorAgreements.Aggregates;
using Encina.Compliance.ProcessorAgreements.ReadModels;
using Encina.Compliance.ProcessorAgreements.Services;
using Encina.Marten;
using Encina.Marten.Projections;

namespace Encina.GuardTests.Compliance.ProcessorAgreements;

/// <summary>
/// Method-level guard tests for <see cref="DefaultProcessorService"/>.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="DefaultProcessorService"/> is an <c>internal sealed</c> class accessible via
/// <c>InternalsVisibleTo("Encina.GuardTests")</c>.
/// </para>
/// <para>
/// The public methods of <see cref="DefaultProcessorService"/> (RegisterProcessorAsync,
/// UpdateProcessorAsync, GetProcessorAsync, etc.) do NOT use
/// <c>ArgumentNullException.ThrowIfNull</c> on their parameters; they rely on
/// constructor-injected dependencies and use try/catch for runtime errors.
/// This test class validates constructor guard completeness and documents
/// the absence of method-level guards.
/// </para>
/// </remarks>
public sealed class DefaultProcessorServiceMethodGuardTests
{
    private readonly IAggregateRepository<ProcessorAggregate> _repository =
        Substitute.For<IAggregateRepository<ProcessorAggregate>>();

    private readonly IReadModelRepository<ProcessorReadModel> _readModelRepository =
        Substitute.For<IReadModelRepository<ProcessorReadModel>>();

    private readonly ICacheProvider _cache = Substitute.For<ICacheProvider>();
    private readonly TimeProvider _timeProvider = TimeProvider.System;

    private readonly ILogger<DefaultProcessorService> _logger =
        NullLogger<DefaultProcessorService>.Instance;

    #region Constructor Guard Completeness

    /// <summary>
    /// Verifies that the constructor accepts all valid non-null parameters without throwing.
    /// </summary>
    [Fact]
    public void Constructor_AllValidParameters_DoesNotThrow()
    {
        var act = () => new DefaultProcessorService(
            _repository, _readModelRepository, _cache, _timeProvider, _logger);

        Should.NotThrow(act);
    }

    /// <summary>
    /// Verifies that all five constructor parameters are individually guarded by
    /// trying each null permutation and expecting ArgumentNullException.
    /// </summary>
    [Fact]
    public void Constructor_EachParameter_IsGuarded()
    {
        // repository
        Should.Throw<ArgumentNullException>(
            () => new DefaultProcessorService(null!, _readModelRepository, _cache, _timeProvider, _logger))
            .ParamName.ShouldBe("repository");

        // readModelRepository
        Should.Throw<ArgumentNullException>(
            () => new DefaultProcessorService(_repository, null!, _cache, _timeProvider, _logger))
            .ParamName.ShouldBe("readModelRepository");

        // cache
        Should.Throw<ArgumentNullException>(
            () => new DefaultProcessorService(_repository, _readModelRepository, null!, _timeProvider, _logger))
            .ParamName.ShouldBe("cache");

        // timeProvider
        Should.Throw<ArgumentNullException>(
            () => new DefaultProcessorService(_repository, _readModelRepository, _cache, null!, _logger))
            .ParamName.ShouldBe("timeProvider");

        // logger
        Should.Throw<ArgumentNullException>(
            () => new DefaultProcessorService(_repository, _readModelRepository, _cache, _timeProvider, null!))
            .ParamName.ShouldBe("logger");
    }

    #endregion
}
