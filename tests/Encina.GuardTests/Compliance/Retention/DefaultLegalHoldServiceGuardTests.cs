using Encina.Compliance.Retention.Abstractions;
using Encina.Compliance.Retention.Aggregates;
using Encina.Compliance.Retention.ReadModels;
using Encina.Compliance.Retention.Services;
using Encina.Marten;
using Encina.Marten.Projections;

namespace Encina.GuardTests.Compliance.Retention;

/// <summary>
/// Guard tests for <see cref="DefaultLegalHoldService"/> constructor null parameter handling.
/// </summary>
public sealed class DefaultLegalHoldServiceGuardTests
{
    private readonly IAggregateRepository<LegalHoldAggregate> _repository =
        Substitute.For<IAggregateRepository<LegalHoldAggregate>>();

    private readonly IReadModelRepository<LegalHoldReadModel> _readModelRepository =
        Substitute.For<IReadModelRepository<LegalHoldReadModel>>();

    private readonly IReadModelRepository<RetentionRecordReadModel> _recordReadModelRepository =
        Substitute.For<IReadModelRepository<RetentionRecordReadModel>>();

    private readonly IRetentionRecordService _retentionRecordService =
        Substitute.For<IRetentionRecordService>();

    private readonly ICacheProvider _cache = Substitute.For<ICacheProvider>();
    private readonly TimeProvider _timeProvider = TimeProvider.System;

    private readonly ILogger<DefaultLegalHoldService> _logger =
        NullLogger<DefaultLegalHoldService>.Instance;

    #region Constructor Guards

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when repository is null.
    /// </summary>
    [Fact]
    public void Constructor_NullRepository_ThrowsArgumentNullException()
    {
        var act = () => new DefaultLegalHoldService(
            null!,
            _readModelRepository,
            _recordReadModelRepository,
            _retentionRecordService,
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
        var act = () => new DefaultLegalHoldService(
            _repository,
            null!,
            _recordReadModelRepository,
            _retentionRecordService,
            _cache,
            _timeProvider,
            _logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("readModelRepository");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when recordReadModelRepository is null.
    /// </summary>
    [Fact]
    public void Constructor_NullRecordReadModelRepository_ThrowsArgumentNullException()
    {
        var act = () => new DefaultLegalHoldService(
            _repository,
            _readModelRepository,
            null!,
            _retentionRecordService,
            _cache,
            _timeProvider,
            _logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("recordReadModelRepository");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when retentionRecordService is null.
    /// </summary>
    [Fact]
    public void Constructor_NullRetentionRecordService_ThrowsArgumentNullException()
    {
        var act = () => new DefaultLegalHoldService(
            _repository,
            _readModelRepository,
            _recordReadModelRepository,
            null!,
            _cache,
            _timeProvider,
            _logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("retentionRecordService");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when cache is null.
    /// </summary>
    [Fact]
    public void Constructor_NullCache_ThrowsArgumentNullException()
    {
        var act = () => new DefaultLegalHoldService(
            _repository,
            _readModelRepository,
            _recordReadModelRepository,
            _retentionRecordService,
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
        var act = () => new DefaultLegalHoldService(
            _repository,
            _readModelRepository,
            _recordReadModelRepository,
            _retentionRecordService,
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
        var act = () => new DefaultLegalHoldService(
            _repository,
            _readModelRepository,
            _recordReadModelRepository,
            _retentionRecordService,
            _cache,
            _timeProvider,
            null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    #endregion
}
