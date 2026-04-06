using Encina.Caching;
using Encina.Compliance.DataResidency.Aggregates;
using Encina.Compliance.DataResidency.ReadModels;
using Encina.Compliance.DataResidency.Services;
using Encina.Marten;
using Encina.Marten.Projections;

using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

namespace Encina.GuardTests.Compliance.DataResidency;

/// <summary>
/// Guard tests for <see cref="DefaultDataLocationService"/> verifying constructor null checks.
/// </summary>
public class DefaultDataLocationServiceGuardTests
{
    private readonly IAggregateRepository<DataLocationAggregate> _repository =
        Substitute.For<IAggregateRepository<DataLocationAggregate>>();

    private readonly IReadModelRepository<DataLocationReadModel> _readModelRepository =
        Substitute.For<IReadModelRepository<DataLocationReadModel>>();

    private readonly ICacheProvider _cache = Substitute.For<ICacheProvider>();
    private readonly TimeProvider _timeProvider = TimeProvider.System;

    [Fact]
    public void Constructor_NullRepository_ThrowsArgumentNullException()
    {
        var act = () => new DefaultDataLocationService(
            null!, _readModelRepository, _cache, _timeProvider,
            NullLogger<DefaultDataLocationService>.Instance);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("repository");
    }

    [Fact]
    public void Constructor_NullReadModelRepository_ThrowsArgumentNullException()
    {
        var act = () => new DefaultDataLocationService(
            _repository, null!, _cache, _timeProvider,
            NullLogger<DefaultDataLocationService>.Instance);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("readModelRepository");
    }

    [Fact]
    public void Constructor_NullCache_ThrowsArgumentNullException()
    {
        var act = () => new DefaultDataLocationService(
            _repository, _readModelRepository, null!, _timeProvider,
            NullLogger<DefaultDataLocationService>.Instance);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("cache");
    }

    [Fact]
    public void Constructor_NullTimeProvider_ThrowsArgumentNullException()
    {
        var act = () => new DefaultDataLocationService(
            _repository, _readModelRepository, _cache, null!,
            NullLogger<DefaultDataLocationService>.Instance);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("timeProvider");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new DefaultDataLocationService(
            _repository, _readModelRepository, _cache, _timeProvider,
            null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }
}
