using Encina.Compliance.Retention.Aggregates;
using Encina.Compliance.Retention.ReadModels;
using Encina.Compliance.Retention.Services;
using Encina.Marten;
using Encina.Marten.Projections;

namespace Encina.GuardTests.Compliance.Retention;

/// <summary>
/// Guard tests for <see cref="DefaultRetentionRecordService"/> constructor null parameter handling.
/// </summary>
public sealed class DefaultRetentionRecordServiceGuardTests
{
    private readonly IAggregateRepository<RetentionRecordAggregate> _repository =
        Substitute.For<IAggregateRepository<RetentionRecordAggregate>>();

    private readonly IReadModelRepository<RetentionRecordReadModel> _readModelRepository =
        Substitute.For<IReadModelRepository<RetentionRecordReadModel>>();

    private readonly ICacheProvider _cache = Substitute.For<ICacheProvider>();
    private readonly TimeProvider _timeProvider = TimeProvider.System;

    private readonly ILogger<DefaultRetentionRecordService> _logger =
        NullLogger<DefaultRetentionRecordService>.Instance;

    #region Constructor Guards

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when repository is null.
    /// </summary>
    [Fact]
    public void Constructor_NullRepository_ThrowsArgumentNullException()
    {
        var act = () => new DefaultRetentionRecordService(
            null!,
            _readModelRepository,
            _cache,
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
        var act = () => new DefaultRetentionRecordService(
            _repository,
            null!,
            _cache,
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
        var act = () => new DefaultRetentionRecordService(
            _repository,
            _readModelRepository,
            null!,
            _timeProvider,
            _logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("cache");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when timeProvider is null.
    /// </summary>
    [Fact]
    public void Constructor_NullTimeProvider_ThrowsArgumentNullException()
    {
        var act = () => new DefaultRetentionRecordService(
            _repository,
            _readModelRepository,
            _cache,
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
        var act = () => new DefaultRetentionRecordService(
            _repository,
            _readModelRepository,
            _cache,
            _timeProvider,
            null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    #endregion
}
