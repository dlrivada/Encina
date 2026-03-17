using Encina.Compliance.Retention;
using Encina.Compliance.Retention.Aggregates;
using Encina.Compliance.Retention.ReadModels;
using Encina.Compliance.Retention.Services;
using Encina.Marten;
using Encina.Marten.Projections;

namespace Encina.GuardTests.Compliance.Retention;

/// <summary>
/// Guard tests for <see cref="DefaultRetentionPolicyService"/> constructor null parameter handling.
/// </summary>
public sealed class DefaultRetentionPolicyServiceGuardTests
{
    private readonly IAggregateRepository<RetentionPolicyAggregate> _repository =
        Substitute.For<IAggregateRepository<RetentionPolicyAggregate>>();

    private readonly IReadModelRepository<RetentionPolicyReadModel> _readModelRepository =
        Substitute.For<IReadModelRepository<RetentionPolicyReadModel>>();

    private readonly ICacheProvider _cache = Substitute.For<ICacheProvider>();

    private readonly IOptions<RetentionOptions> _options =
        Substitute.For<IOptions<RetentionOptions>>();

    private readonly TimeProvider _timeProvider = TimeProvider.System;

    private readonly ILogger<DefaultRetentionPolicyService> _logger =
        NullLogger<DefaultRetentionPolicyService>.Instance;

    #region Constructor Guards

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when repository is null.
    /// </summary>
    [Fact]
    public void Constructor_NullRepository_ThrowsArgumentNullException()
    {
        var act = () => new DefaultRetentionPolicyService(
            null!,
            _readModelRepository,
            _cache,
            _options,
            _timeProvider,
            _logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("repository");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when readModelRepository is null.
    /// </summary>
    [Fact]
    public void Constructor_NullReadModelRepository_ThrowsArgumentNullException()
    {
        var act = () => new DefaultRetentionPolicyService(
            _repository,
            null!,
            _cache,
            _options,
            _timeProvider,
            _logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("readModelRepository");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when cache is null.
    /// </summary>
    [Fact]
    public void Constructor_NullCache_ThrowsArgumentNullException()
    {
        var act = () => new DefaultRetentionPolicyService(
            _repository,
            _readModelRepository,
            null!,
            _options,
            _timeProvider,
            _logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("cache");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when options is null.
    /// </summary>
    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new DefaultRetentionPolicyService(
            _repository,
            _readModelRepository,
            _cache,
            null!,
            _timeProvider,
            _logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("options");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when timeProvider is null.
    /// </summary>
    [Fact]
    public void Constructor_NullTimeProvider_ThrowsArgumentNullException()
    {
        var act = () => new DefaultRetentionPolicyService(
            _repository,
            _readModelRepository,
            _cache,
            _options,
            null!,
            _logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("timeProvider");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when logger is null.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new DefaultRetentionPolicyService(
            _repository,
            _readModelRepository,
            _cache,
            _options,
            _timeProvider,
            null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    #endregion
}
