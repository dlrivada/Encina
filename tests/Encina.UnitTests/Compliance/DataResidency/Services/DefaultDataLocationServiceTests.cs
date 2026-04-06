#pragma warning disable CA2012

using Encina.Caching;
using Encina.Compliance.DataResidency.Aggregates;
using Encina.Compliance.DataResidency.Model;
using Encina.Compliance.DataResidency.ReadModels;
using Encina.Compliance.DataResidency.Services;
using Encina.Marten;
using Encina.Marten.Projections;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.DataResidency.Services;

public class DefaultDataLocationServiceTests
{
    private readonly IAggregateRepository<DataLocationAggregate> _repository;
    private readonly IReadModelRepository<DataLocationReadModel> _readModelRepository;
    private readonly ICacheProvider _cache;
    private readonly DefaultDataLocationService _sut;

    public DefaultDataLocationServiceTests()
    {
        _repository = Substitute.For<IAggregateRepository<DataLocationAggregate>>();
        _readModelRepository = Substitute.For<IReadModelRepository<DataLocationReadModel>>();
        _cache = Substitute.For<ICacheProvider>();

        _sut = new DefaultDataLocationService(
            _repository, _readModelRepository, _cache, TimeProvider.System,
            NullLogger<DefaultDataLocationService>.Instance);
    }

    #region RegisterLocationAsync

    [Fact]
    public async Task RegisterLocationAsync_ValidParams_ReturnsGuid()
    {
        _repository.CreateAsync(Arg.Any<DataLocationAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, LanguageExt.Unit>(unit)));
        _cache.RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var result = await _sut.RegisterLocationAsync(
            "entity-1", "personal-data", "DE", StorageType.Primary,
            null, null, null, CancellationToken.None);

        result.IsRight.Should().BeTrue();
        result.Match(id => id.Should().NotBeEmpty(), _ => { });
    }

    [Fact]
    public async Task RegisterLocationAsync_RepositoryError_ReturnsLeft()
    {
        var error = EncinaErrors.Create("repo.error", "Failed");
        _repository.CreateAsync(Arg.Any<DataLocationAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Left<EncinaError, LanguageExt.Unit>(error)));

        var result = await _sut.RegisterLocationAsync(
            "entity-1", "personal-data", "DE", StorageType.Primary,
            null, null, null, CancellationToken.None);

        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public async Task RegisterLocationAsync_Exception_ReturnsStoreError()
    {
        _repository.CreateAsync(Arg.Any<DataLocationAggregate>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("DB down"));

        var result = await _sut.RegisterLocationAsync(
            "entity-1", "personal-data", "DE", StorageType.Primary,
            null, null, null, CancellationToken.None);

        result.IsLeft.Should().BeTrue();
    }

    #endregion

    #region GetLocationAsync

    [Fact]
    public async Task GetLocationAsync_CacheHit_ReturnsCached()
    {
        var locationId = Guid.NewGuid();
        var cached = new DataLocationReadModel
        {
            Id = locationId,
            EntityId = "entity-1",
            DataCategory = "data",
            RegionCode = "DE",
            StorageType = StorageType.Primary,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            LastModifiedAtUtc = DateTimeOffset.UtcNow
        };
        _cache.GetAsync<DataLocationReadModel>(
                $"dr:location:{locationId}", Arg.Any<CancellationToken>())
            .Returns(cached);

        var result = await _sut.GetLocationAsync(locationId, CancellationToken.None);

        result.IsRight.Should().BeTrue();
        await _repository.DidNotReceive().LoadAsync(locationId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetLocationAsync_CacheMiss_LoadsFromReadModelRepository()
    {
        var locationId = Guid.NewGuid();
        var readModel = new DataLocationReadModel
        {
            Id = locationId,
            EntityId = "entity-1",
            DataCategory = "data",
            RegionCode = "DE",
            StorageType = StorageType.Primary,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            LastModifiedAtUtc = DateTimeOffset.UtcNow
        };
        _cache.GetAsync<DataLocationReadModel>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((DataLocationReadModel?)null);
        _readModelRepository.GetByIdAsync(locationId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, DataLocationReadModel>(readModel)));
        _cache.SetAsync(
                Arg.Any<string>(), Arg.Any<DataLocationReadModel>(),
                Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var result = await _sut.GetLocationAsync(locationId, CancellationToken.None);

        result.IsRight.Should().BeTrue();
    }

    [Fact]
    public async Task GetLocationAsync_NotFound_ReturnsError()
    {
        var error = EncinaErrors.Create("not.found", "Not found");
        _cache.GetAsync<DataLocationReadModel>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((DataLocationReadModel?)null);
        _readModelRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Left<EncinaError, DataLocationReadModel>(error)));

        var result = await _sut.GetLocationAsync(Guid.NewGuid(), CancellationToken.None);

        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public async Task GetLocationAsync_StoreException_ReturnsError()
    {
        _cache.GetAsync<DataLocationReadModel>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((DataLocationReadModel?)null);
        _readModelRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("DB down"));

        var result = await _sut.GetLocationAsync(Guid.NewGuid(), CancellationToken.None);

        result.IsLeft.Should().BeTrue();
    }

    #endregion
}
