using Encina.Caching;
using Encina.Compliance.BreachNotification.Aggregates;
using Encina.Compliance.BreachNotification.ReadModels;
using Encina.Compliance.BreachNotification.Services;
using Encina.Marten;
using Encina.Marten.Projections;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Encina.GuardTests.Compliance.BreachNotification;

/// <summary>
/// Guard tests for <see cref="DefaultBreachNotificationService"/> constructor null parameter handling.
/// </summary>
public sealed class DefaultBreachNotificationServiceGuardTests
{
    private readonly IAggregateRepository<BreachAggregate> _repository =
        Substitute.For<IAggregateRepository<BreachAggregate>>();

    private readonly IReadModelRepository<BreachReadModel> _readModelRepository =
        Substitute.For<IReadModelRepository<BreachReadModel>>();

    private readonly ICacheProvider _cache = Substitute.For<ICacheProvider>();
    private readonly TimeProvider _timeProvider = TimeProvider.System;

    private readonly ILogger<DefaultBreachNotificationService> _logger =
        NullLogger<DefaultBreachNotificationService>.Instance;

    #region Constructor Guards

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when repository is null.
    /// </summary>
    [Fact]
    public void Constructor_NullRepository_ThrowsArgumentNullException()
    {
        var act = () => new DefaultBreachNotificationService(
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
        var act = () => new DefaultBreachNotificationService(
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
        var act = () => new DefaultBreachNotificationService(
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
        var act = () => new DefaultBreachNotificationService(
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
        var act = () => new DefaultBreachNotificationService(
            _repository,
            _readModelRepository,
            _cache,
            _timeProvider,
            null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    #endregion
}
