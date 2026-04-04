using Encina.Caching;
using Encina.Compliance.DataResidency.Aggregates;
using Encina.Compliance.DataResidency.Model;
using Encina.Compliance.DataResidency.ReadModels;
using Encina.Compliance.DataResidency.Services;
using Encina.Marten;
using Encina.Marten.Projections;

using LanguageExt;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using Shouldly;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.DataResidency;

/// <summary>
/// Unit tests for <see cref="DefaultDataLocationService"/> covering all command
/// and query operations including cache interactions and error paths.
/// </summary>
public class DefaultDataLocationServiceTests
{
    private readonly IAggregateRepository<DataLocationAggregate> _repository;
    private readonly IReadModelRepository<DataLocationReadModel> _readModelRepository;
    private readonly ICacheProvider _cache;
    private readonly FakeTimeProvider _timeProvider;
    private readonly ILogger<DefaultDataLocationService> _logger;
    private readonly DefaultDataLocationService _sut;

    public DefaultDataLocationServiceTests()
    {
        _repository = Substitute.For<IAggregateRepository<DataLocationAggregate>>();
        _readModelRepository = Substitute.For<IReadModelRepository<DataLocationReadModel>>();
        _cache = Substitute.For<ICacheProvider>();
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.Zero));
        _logger = NullLogger<DefaultDataLocationService>.Instance;

        _sut = new DefaultDataLocationService(
            _repository, _readModelRepository, _cache, _timeProvider, _logger);
    }

    // ================================================================
    // RegisterLocationAsync
    // ================================================================

    [Fact]
    public async Task RegisterLocationAsync_Success_ShouldReturnGuid()
    {
        // Arrange
        _repository.CreateAsync(Arg.Any<DataLocationAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));

        // Act
        var result = await _sut.RegisterLocationAsync(
            "entity-1", "personal-data", "DE", StorageType.Primary,
            null, null, null, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        var id = (Guid)result;
        id.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public async Task RegisterLocationAsync_RepositoryFails_ShouldReturnError()
    {
        // Arrange
        var error = EncinaErrors.Create(code: "test.error", message: "Repository failed");
        _repository.CreateAsync(Arg.Any<DataLocationAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, Unit>(error));

        // Act
        var result = await _sut.RegisterLocationAsync(
            "entity-1", "personal-data", "DE", StorageType.Primary,
            null, null, null, CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task RegisterLocationAsync_Exception_ShouldReturnServiceError()
    {
        // Arrange
        _repository.CreateAsync(Arg.Any<DataLocationAggregate>(), Arg.Any<CancellationToken>())
            .Throws(new TimeoutException("DB down"));

        // Act
        var result = await _sut.RegisterLocationAsync(
            "entity-1", "personal-data", "DE", StorageType.Primary,
            null, null, null, CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
        ((EncinaError)result).Message.ShouldContain("RegisterLocation");
    }

    [Fact]
    public async Task RegisterLocationAsync_WithMetadata_ShouldSucceed()
    {
        // Arrange
        var metadata = new Dictionary<string, string> { ["cloud"] = "azure" };
        _repository.CreateAsync(Arg.Any<DataLocationAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));

        // Act
        var result = await _sut.RegisterLocationAsync(
            "entity-1", "healthcare-data", "FR", StorageType.Replica,
            metadata, "tenant-1", "mod-1", CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    // ================================================================
    // MigrateLocationAsync
    // ================================================================

    [Fact]
    public async Task MigrateLocationAsync_Success_ShouldReturnUnit()
    {
        // Arrange
        var aggregate = DataLocationAggregate.Register(
            Guid.NewGuid(), "entity-1", "personal-data", "DE", StorageType.Primary,
            DateTimeOffset.UtcNow);

        _repository.LoadAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, DataLocationAggregate>(aggregate));
        _repository.SaveAsync(Arg.Any<DataLocationAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));

        // Act
        var result = await _sut.MigrateLocationAsync(aggregate.Id, "FR", "GDPR compliance", CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task MigrateLocationAsync_NotFound_ShouldReturnError()
    {
        // Arrange
        _repository.LoadAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, DataLocationAggregate>(EncinaErrors.Create(code: "not_found", message: "Not found")));

        // Act
        var result = await _sut.MigrateLocationAsync(Guid.NewGuid(), "FR", "reason", CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task MigrateLocationAsync_InvalidState_ShouldReturnInvalidStateError()
    {
        // Arrange — removed aggregate throws InvalidOperationException on Migrate
        var aggregate = DataLocationAggregate.Register(
            Guid.NewGuid(), "entity-1", "personal-data", "DE", StorageType.Primary,
            DateTimeOffset.UtcNow);
        aggregate.Remove("cleanup");

        _repository.LoadAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, DataLocationAggregate>(aggregate));

        // Act
        var result = await _sut.MigrateLocationAsync(aggregate.Id, "FR", "reason", CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task MigrateLocationAsync_Exception_ShouldReturnServiceError()
    {
        // Arrange
        _repository.LoadAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Unexpected"));

        // Act
        var result = await _sut.MigrateLocationAsync(Guid.NewGuid(), "FR", "reason", CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
        ((EncinaError)result).Message.ShouldContain("MigrateLocation");
    }

    // ================================================================
    // VerifyLocationAsync
    // ================================================================

    [Fact]
    public async Task VerifyLocationAsync_Success_ShouldReturnUnit()
    {
        // Arrange
        var aggregate = DataLocationAggregate.Register(
            Guid.NewGuid(), "entity-1", "personal-data", "DE", StorageType.Primary,
            DateTimeOffset.UtcNow);

        _repository.LoadAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, DataLocationAggregate>(aggregate));
        _repository.SaveAsync(Arg.Any<DataLocationAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));

        // Act
        var result = await _sut.VerifyLocationAsync(aggregate.Id, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task VerifyLocationAsync_NotFound_ShouldReturnError()
    {
        // Arrange
        _repository.LoadAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, DataLocationAggregate>(EncinaErrors.Create(code: "nf", message: "Not found")));

        // Act
        var result = await _sut.VerifyLocationAsync(Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task VerifyLocationAsync_RemovedAggregate_ShouldReturnInvalidStateError()
    {
        // Arrange
        var aggregate = DataLocationAggregate.Register(
            Guid.NewGuid(), "entity-1", "personal-data", "DE", StorageType.Primary,
            DateTimeOffset.UtcNow);
        aggregate.Remove("cleanup");

        _repository.LoadAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, DataLocationAggregate>(aggregate));

        // Act
        var result = await _sut.VerifyLocationAsync(aggregate.Id, CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task VerifyLocationAsync_Exception_ShouldReturnServiceError()
    {
        // Arrange
        _repository.LoadAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Throws(new TimeoutException("Unexpected"));

        // Act
        var result = await _sut.VerifyLocationAsync(Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    // ================================================================
    // RemoveLocationAsync
    // ================================================================

    [Fact]
    public async Task RemoveLocationAsync_Success_ShouldReturnUnit()
    {
        // Arrange
        var aggregate = DataLocationAggregate.Register(
            Guid.NewGuid(), "entity-1", "personal-data", "DE", StorageType.Primary,
            DateTimeOffset.UtcNow);

        _repository.LoadAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, DataLocationAggregate>(aggregate));
        _repository.SaveAsync(Arg.Any<DataLocationAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));

        // Act
        var result = await _sut.RemoveLocationAsync(aggregate.Id, "GDPR erasure", CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task RemoveLocationAsync_NotFound_ShouldReturnError()
    {
        // Arrange
        _repository.LoadAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, DataLocationAggregate>(EncinaErrors.Create(code: "nf", message: "Not found")));

        // Act
        var result = await _sut.RemoveLocationAsync(Guid.NewGuid(), "reason", CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task RemoveLocationAsync_AlreadyRemoved_ShouldReturnInvalidStateError()
    {
        // Arrange
        var aggregate = DataLocationAggregate.Register(
            Guid.NewGuid(), "entity-1", "personal-data", "DE", StorageType.Primary,
            DateTimeOffset.UtcNow);
        aggregate.Remove("first");

        _repository.LoadAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, DataLocationAggregate>(aggregate));

        // Act
        var result = await _sut.RemoveLocationAsync(aggregate.Id, "second", CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task RemoveLocationAsync_Exception_ShouldReturnServiceError()
    {
        // Arrange
        _repository.LoadAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Throws(new TimeoutException("Unexpected"));

        // Act
        var result = await _sut.RemoveLocationAsync(Guid.NewGuid(), "reason", CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    // ================================================================
    // RemoveByEntityAsync
    // ================================================================

    [Fact]
    public async Task RemoveByEntityAsync_WithLocations_ShouldRemoveAll()
    {
        // Arrange
        var locations = new List<DataLocationReadModel>
        {
            new() { Id = Guid.NewGuid(), EntityId = "entity-1", IsRemoved = false },
            new() { Id = Guid.NewGuid(), EntityId = "entity-1", IsRemoved = false }
        };

        _readModelRepository.QueryAsync(
            Arg.Any<Func<IQueryable<DataLocationReadModel>, IQueryable<DataLocationReadModel>>>(),
            Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<DataLocationReadModel>>(locations));

        // Setup each load/save for the RemoveLocationAsync calls
        foreach (var loc in locations)
        {
            var aggregate = DataLocationAggregate.Register(
                loc.Id, "entity-1", "personal-data", "DE", StorageType.Primary,
                DateTimeOffset.UtcNow);

            _repository.LoadAsync(loc.Id, Arg.Any<CancellationToken>())
                .Returns(Right<EncinaError, DataLocationAggregate>(aggregate));
            _repository.SaveAsync(Arg.Any<DataLocationAggregate>(), Arg.Any<CancellationToken>())
                .Returns(Right<EncinaError, Unit>(Unit.Default));
        }

        // Act
        var result = await _sut.RemoveByEntityAsync("entity-1", "GDPR erasure", CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        ((int)result).ShouldBe(2);
    }

    [Fact]
    public async Task RemoveByEntityAsync_NoLocations_ShouldReturnZero()
    {
        // Arrange
        _readModelRepository.QueryAsync(
            Arg.Any<Func<IQueryable<DataLocationReadModel>, IQueryable<DataLocationReadModel>>>(),
            Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<DataLocationReadModel>>(new List<DataLocationReadModel>()));

        // Act
        var result = await _sut.RemoveByEntityAsync("entity-1", "cleanup", CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        ((int)result).ShouldBe(0);
    }

    [Fact]
    public async Task RemoveByEntityAsync_Exception_ShouldReturnServiceError()
    {
        // Arrange
        _readModelRepository.QueryAsync(
            Arg.Any<Func<IQueryable<DataLocationReadModel>, IQueryable<DataLocationReadModel>>>(),
            Arg.Any<CancellationToken>())
            .Throws(new TimeoutException("Unexpected"));

        // Act
        var result = await _sut.RemoveByEntityAsync("entity-1", "reason", CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    // ================================================================
    // DetectViolationAsync
    // ================================================================

    [Fact]
    public async Task DetectViolationAsync_Success_ShouldReturnUnit()
    {
        // Arrange
        var aggregate = DataLocationAggregate.Register(
            Guid.NewGuid(), "entity-1", "personal-data", "DE", StorageType.Primary,
            DateTimeOffset.UtcNow);

        _repository.LoadAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, DataLocationAggregate>(aggregate));
        _repository.SaveAsync(Arg.Any<DataLocationAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));

        // Act
        var result = await _sut.DetectViolationAsync(
            aggregate.Id, "personal-data", "CN", "Data found in non-compliant region", CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task DetectViolationAsync_NotFound_ShouldReturnError()
    {
        // Arrange
        _repository.LoadAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, DataLocationAggregate>(EncinaErrors.Create(code: "nf", message: "Not found")));

        // Act
        var result = await _sut.DetectViolationAsync(
            Guid.NewGuid(), "personal-data", "CN", "details", CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task DetectViolationAsync_AlreadyHasViolation_ShouldReturnInvalidStateError()
    {
        // Arrange
        var aggregate = DataLocationAggregate.Register(
            Guid.NewGuid(), "entity-1", "personal-data", "DE", StorageType.Primary,
            DateTimeOffset.UtcNow);
        aggregate.DetectViolation("personal-data", "CN", "first violation");

        _repository.LoadAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, DataLocationAggregate>(aggregate));

        // Act
        var result = await _sut.DetectViolationAsync(
            aggregate.Id, "personal-data", "RU", "second violation", CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    // ================================================================
    // ResolveViolationAsync
    // ================================================================

    [Fact]
    public async Task ResolveViolationAsync_Success_ShouldReturnUnit()
    {
        // Arrange
        var aggregate = DataLocationAggregate.Register(
            Guid.NewGuid(), "entity-1", "personal-data", "DE", StorageType.Primary,
            DateTimeOffset.UtcNow);
        aggregate.DetectViolation("personal-data", "CN", "violation");

        _repository.LoadAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, DataLocationAggregate>(aggregate));
        _repository.SaveAsync(Arg.Any<DataLocationAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));

        // Act
        var result = await _sut.ResolveViolationAsync(aggregate.Id, "Migrated to DE", CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task ResolveViolationAsync_NoActiveViolation_ShouldReturnInvalidStateError()
    {
        // Arrange
        var aggregate = DataLocationAggregate.Register(
            Guid.NewGuid(), "entity-1", "personal-data", "DE", StorageType.Primary,
            DateTimeOffset.UtcNow);

        _repository.LoadAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, DataLocationAggregate>(aggregate));

        // Act
        var result = await _sut.ResolveViolationAsync(aggregate.Id, "resolution", CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    // ================================================================
    // GetLocationAsync (query with cache)
    // ================================================================

    [Fact]
    public async Task GetLocationAsync_CacheHit_ShouldReturnCachedValue()
    {
        // Arrange
        var locationId = Guid.NewGuid();
        var cachedModel = new DataLocationReadModel { Id = locationId, EntityId = "entity-1" };

        _cache.GetAsync<DataLocationReadModel>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(cachedModel);

        // Act
        var result = await _sut.GetLocationAsync(locationId, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        ((DataLocationReadModel)result).Id.ShouldBe(locationId);
        await _readModelRepository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetLocationAsync_CacheMiss_ShouldQueryRepository()
    {
        // Arrange
        var locationId = Guid.NewGuid();
        var readModel = new DataLocationReadModel { Id = locationId, EntityId = "entity-1" };

        _cache.GetAsync<DataLocationReadModel>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((DataLocationReadModel?)null);
        _readModelRepository.GetByIdAsync(locationId, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, DataLocationReadModel>(readModel));

        // Act
        var result = await _sut.GetLocationAsync(locationId, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        await _cache.Received(1).SetAsync(
            Arg.Any<string>(), readModel, Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetLocationAsync_NotFound_ShouldReturnLocationNotFoundError()
    {
        // Arrange
        _cache.GetAsync<DataLocationReadModel>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((DataLocationReadModel?)null);
        _readModelRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, DataLocationReadModel>(EncinaErrors.Create(code: "nf", message: "Not found")));

        // Act
        var result = await _sut.GetLocationAsync(Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    // ================================================================
    // GetByEntityAsync, GetByRegionAsync, GetByCategoryAsync, GetViolationsAsync
    // ================================================================

    [Fact]
    public async Task GetByEntityAsync_ShouldQueryReadModel()
    {
        // Arrange
        var models = new List<DataLocationReadModel>
        {
            new() { Id = Guid.NewGuid(), EntityId = "entity-1" }
        };
        _readModelRepository.QueryAsync(
            Arg.Any<Func<IQueryable<DataLocationReadModel>, IQueryable<DataLocationReadModel>>>(),
            Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<DataLocationReadModel>>(models));

        // Act
        var result = await _sut.GetByEntityAsync("entity-1", CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task GetByRegionAsync_ShouldQueryReadModel()
    {
        // Arrange
        _readModelRepository.QueryAsync(
            Arg.Any<Func<IQueryable<DataLocationReadModel>, IQueryable<DataLocationReadModel>>>(),
            Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<DataLocationReadModel>>(new List<DataLocationReadModel>()));

        // Act
        var result = await _sut.GetByRegionAsync("DE", CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task GetByCategoryAsync_ShouldQueryReadModel()
    {
        // Arrange
        _readModelRepository.QueryAsync(
            Arg.Any<Func<IQueryable<DataLocationReadModel>, IQueryable<DataLocationReadModel>>>(),
            Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<DataLocationReadModel>>(new List<DataLocationReadModel>()));

        // Act
        var result = await _sut.GetByCategoryAsync("personal-data", CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task GetViolationsAsync_ShouldReturnViolations()
    {
        // Arrange
        _readModelRepository.QueryAsync(
            Arg.Any<Func<IQueryable<DataLocationReadModel>, IQueryable<DataLocationReadModel>>>(),
            Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<DataLocationReadModel>>(new List<DataLocationReadModel>()));

        // Act
        var result = await _sut.GetViolationsAsync(CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task GetByEntityAsync_Exception_ShouldReturnServiceError()
    {
        // Arrange
        _readModelRepository.QueryAsync(
            Arg.Any<Func<IQueryable<DataLocationReadModel>, IQueryable<DataLocationReadModel>>>(),
            Arg.Any<CancellationToken>())
            .Throws(new TimeoutException("DB error"));

        // Act
        var result = await _sut.GetByEntityAsync("entity-1", CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task GetByRegionAsync_Exception_ShouldReturnServiceError()
    {
        // Arrange
        _readModelRepository.QueryAsync(
            Arg.Any<Func<IQueryable<DataLocationReadModel>, IQueryable<DataLocationReadModel>>>(),
            Arg.Any<CancellationToken>())
            .Throws(new TimeoutException("DB error"));

        // Act
        var result = await _sut.GetByRegionAsync("DE", CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task GetByCategoryAsync_Exception_ShouldReturnServiceError()
    {
        // Arrange
        _readModelRepository.QueryAsync(
            Arg.Any<Func<IQueryable<DataLocationReadModel>, IQueryable<DataLocationReadModel>>>(),
            Arg.Any<CancellationToken>())
            .Throws(new TimeoutException("DB error"));

        // Act
        var result = await _sut.GetByCategoryAsync("personal-data", CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task GetViolationsAsync_Exception_ShouldReturnServiceError()
    {
        // Arrange
        _readModelRepository.QueryAsync(
            Arg.Any<Func<IQueryable<DataLocationReadModel>, IQueryable<DataLocationReadModel>>>(),
            Arg.Any<CancellationToken>())
            .Throws(new TimeoutException("DB error"));

        // Act
        var result = await _sut.GetViolationsAsync(CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    // ================================================================
    // GetLocationHistoryAsync (unavailable)
    // ================================================================

    [Fact]
    public async Task GetLocationHistoryAsync_ShouldReturnEventHistoryUnavailableError()
    {
        // Act
        var result = await _sut.GetLocationHistoryAsync(Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }
}
