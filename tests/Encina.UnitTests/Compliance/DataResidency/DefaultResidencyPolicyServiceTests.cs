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
/// Unit tests for <see cref="DefaultResidencyPolicyService"/> covering all command,
/// query, and evaluation operations including cache interactions and error paths.
/// </summary>
public class DefaultResidencyPolicyServiceTests
{
    private readonly IAggregateRepository<ResidencyPolicyAggregate> _repository;
    private readonly IReadModelRepository<ResidencyPolicyReadModel> _readModelRepository;
    private readonly ICacheProvider _cache;
    private readonly FakeTimeProvider _timeProvider;
    private readonly ILogger<DefaultResidencyPolicyService> _logger;
    private readonly DefaultResidencyPolicyService _sut;

    public DefaultResidencyPolicyServiceTests()
    {
        _repository = Substitute.For<IAggregateRepository<ResidencyPolicyAggregate>>();
        _readModelRepository = Substitute.For<IReadModelRepository<ResidencyPolicyReadModel>>();
        _cache = Substitute.For<ICacheProvider>();
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.Zero));
        _logger = NullLogger<DefaultResidencyPolicyService>.Instance;

        _sut = new DefaultResidencyPolicyService(
            _repository, _readModelRepository, _cache, _timeProvider, _logger);
    }

    // ================================================================
    // CreatePolicyAsync
    // ================================================================

    [Fact]
    public async Task CreatePolicyAsync_Success_ShouldReturnGuid()
    {
        // Arrange
        _repository.CreateAsync(Arg.Any<ResidencyPolicyAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));

        // Act
        var result = await _sut.CreatePolicyAsync(
            "healthcare-data", ["DE", "FR"], true,
            [TransferLegalBasis.AdequacyDecision], null, null, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        ((Guid)result).ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public async Task CreatePolicyAsync_RepositoryFails_ShouldReturnError()
    {
        // Arrange
        _repository.CreateAsync(Arg.Any<ResidencyPolicyAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, Unit>(EncinaErrors.Create(code: "err", message: "Failed")));

        // Act
        var result = await _sut.CreatePolicyAsync(
            "data", ["DE"], false, [], null, null, CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task CreatePolicyAsync_Exception_ShouldReturnServiceError()
    {
        // Arrange
        _repository.CreateAsync(Arg.Any<ResidencyPolicyAggregate>(), Arg.Any<CancellationToken>())
            .Throws(new TimeoutException("DB down"));

        // Act
        var result = await _sut.CreatePolicyAsync(
            "data", ["DE"], false, [], null, null, CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
        ((EncinaError)result).Message.ShouldContain("CreatePolicy");
    }

    // ================================================================
    // UpdatePolicyAsync
    // ================================================================

    [Fact]
    public async Task UpdatePolicyAsync_Success_ShouldReturnUnit()
    {
        // Arrange
        var aggregate = ResidencyPolicyAggregate.Create(
            Guid.NewGuid(), "data", ["DE"], false, []);

        _repository.LoadAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, ResidencyPolicyAggregate>(aggregate));
        _repository.SaveAsync(Arg.Any<ResidencyPolicyAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));

        // Act
        var result = await _sut.UpdatePolicyAsync(
            aggregate.Id, ["DE", "FR"], true, [TransferLegalBasis.StandardContractualClauses],
            CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task UpdatePolicyAsync_NotFound_ShouldReturnError()
    {
        // Arrange
        _repository.LoadAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, ResidencyPolicyAggregate>(EncinaErrors.Create(code: "nf", message: "nf")));

        // Act
        var result = await _sut.UpdatePolicyAsync(
            Guid.NewGuid(), ["DE"], false, [], CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task UpdatePolicyAsync_DeletedPolicy_ShouldReturnInvalidStateError()
    {
        // Arrange
        var aggregate = ResidencyPolicyAggregate.Create(
            Guid.NewGuid(), "data", ["DE"], false, []);
        aggregate.Delete("no longer needed");

        _repository.LoadAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, ResidencyPolicyAggregate>(aggregate));

        // Act
        var result = await _sut.UpdatePolicyAsync(
            aggregate.Id, ["FR"], false, [], CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task UpdatePolicyAsync_Exception_ShouldReturnServiceError()
    {
        // Arrange
        _repository.LoadAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Throws(new TimeoutException("Unexpected"));

        // Act
        var result = await _sut.UpdatePolicyAsync(
            Guid.NewGuid(), ["DE"], false, [], CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    // ================================================================
    // DeletePolicyAsync
    // ================================================================

    [Fact]
    public async Task DeletePolicyAsync_Success_ShouldReturnUnit()
    {
        // Arrange
        var aggregate = ResidencyPolicyAggregate.Create(
            Guid.NewGuid(), "data", ["DE"], false, []);

        _repository.LoadAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, ResidencyPolicyAggregate>(aggregate));
        _repository.SaveAsync(Arg.Any<ResidencyPolicyAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));

        // Act
        var result = await _sut.DeletePolicyAsync(aggregate.Id, "no longer needed", CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task DeletePolicyAsync_AlreadyDeleted_ShouldReturnInvalidStateError()
    {
        // Arrange
        var aggregate = ResidencyPolicyAggregate.Create(
            Guid.NewGuid(), "data", ["DE"], false, []);
        aggregate.Delete("first");

        _repository.LoadAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, ResidencyPolicyAggregate>(aggregate));

        // Act
        var result = await _sut.DeletePolicyAsync(aggregate.Id, "second", CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    // ================================================================
    // GetPolicyAsync (query with cache)
    // ================================================================

    [Fact]
    public async Task GetPolicyAsync_CacheHit_ShouldReturnCachedValue()
    {
        // Arrange
        var policyId = Guid.NewGuid();
        var cached = new ResidencyPolicyReadModel { Id = policyId, DataCategory = "data" };

        _cache.GetAsync<ResidencyPolicyReadModel>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(cached);

        // Act
        var result = await _sut.GetPolicyAsync(policyId, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        ((ResidencyPolicyReadModel)result).Id.ShouldBe(policyId);
    }

    [Fact]
    public async Task GetPolicyAsync_CacheMiss_ShouldQueryRepository()
    {
        // Arrange
        var policyId = Guid.NewGuid();
        var readModel = new ResidencyPolicyReadModel { Id = policyId, DataCategory = "data" };

        _cache.GetAsync<ResidencyPolicyReadModel>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((ResidencyPolicyReadModel?)null);
        _readModelRepository.GetByIdAsync(policyId, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, ResidencyPolicyReadModel>(readModel));

        // Act
        var result = await _sut.GetPolicyAsync(policyId, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task GetPolicyAsync_NotFound_ShouldReturnPolicyNotFoundError()
    {
        // Arrange
        _cache.GetAsync<ResidencyPolicyReadModel>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((ResidencyPolicyReadModel?)null);
        _readModelRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, ResidencyPolicyReadModel>(EncinaErrors.Create(code: "nf", message: "nf")));

        // Act
        var result = await _sut.GetPolicyAsync(Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    // ================================================================
    // GetPolicyByCategoryAsync
    // ================================================================

    [Fact]
    public async Task GetPolicyByCategoryAsync_Found_ShouldReturnPolicy()
    {
        // Arrange
        var readModel = new ResidencyPolicyReadModel
        {
            Id = Guid.NewGuid(),
            DataCategory = "healthcare-data",
            IsActive = true
        };

        _readModelRepository.QueryAsync(
            Arg.Any<Func<IQueryable<ResidencyPolicyReadModel>, IQueryable<ResidencyPolicyReadModel>>>(),
            Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<ResidencyPolicyReadModel>>(
                new List<ResidencyPolicyReadModel> { readModel }));

        // Act
        var result = await _sut.GetPolicyByCategoryAsync("healthcare-data", CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task GetPolicyByCategoryAsync_NotFound_ShouldReturnError()
    {
        // Arrange
        _readModelRepository.QueryAsync(
            Arg.Any<Func<IQueryable<ResidencyPolicyReadModel>, IQueryable<ResidencyPolicyReadModel>>>(),
            Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<ResidencyPolicyReadModel>>(
                new List<ResidencyPolicyReadModel>()));

        // Act
        var result = await _sut.GetPolicyByCategoryAsync("unknown-data", CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    // ================================================================
    // GetAllPoliciesAsync
    // ================================================================

    [Fact]
    public async Task GetAllPoliciesAsync_ShouldReturnActivePolicies()
    {
        // Arrange
        _readModelRepository.QueryAsync(
            Arg.Any<Func<IQueryable<ResidencyPolicyReadModel>, IQueryable<ResidencyPolicyReadModel>>>(),
            Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<ResidencyPolicyReadModel>>(
                new List<ResidencyPolicyReadModel>()));

        // Act
        var result = await _sut.GetAllPoliciesAsync(CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task GetAllPoliciesAsync_Exception_ShouldReturnServiceError()
    {
        // Arrange
        _readModelRepository.QueryAsync(
            Arg.Any<Func<IQueryable<ResidencyPolicyReadModel>, IQueryable<ResidencyPolicyReadModel>>>(),
            Arg.Any<CancellationToken>())
            .Throws(new TimeoutException("DB error"));

        // Act
        var result = await _sut.GetAllPoliciesAsync(CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    // ================================================================
    // GetPolicyHistoryAsync (unavailable)
    // ================================================================

    [Fact]
    public async Task GetPolicyHistoryAsync_ShouldReturnEventHistoryUnavailableError()
    {
        // Act
        var result = await _sut.GetPolicyHistoryAsync(Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    // ================================================================
    // IsAllowedAsync (evaluation)
    // ================================================================

    [Fact]
    public async Task IsAllowedAsync_AllowedRegion_ShouldReturnTrue()
    {
        // Arrange
        var readModel = new ResidencyPolicyReadModel
        {
            DataCategory = "data",
            AllowedRegionCodes = ["DE", "FR"],
            IsActive = true
        };

        _readModelRepository.QueryAsync(
            Arg.Any<Func<IQueryable<ResidencyPolicyReadModel>, IQueryable<ResidencyPolicyReadModel>>>(),
            Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<ResidencyPolicyReadModel>>(
                new List<ResidencyPolicyReadModel> { readModel }));

        // Act
        var result = await _sut.IsAllowedAsync("data", RegionRegistry.DE, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        ((bool)result).ShouldBeTrue();
    }

    [Fact]
    public async Task IsAllowedAsync_DisallowedRegion_ShouldReturnFalse()
    {
        // Arrange
        var readModel = new ResidencyPolicyReadModel
        {
            DataCategory = "data",
            AllowedRegionCodes = ["DE", "FR"],
            IsActive = true
        };

        _readModelRepository.QueryAsync(
            Arg.Any<Func<IQueryable<ResidencyPolicyReadModel>, IQueryable<ResidencyPolicyReadModel>>>(),
            Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<ResidencyPolicyReadModel>>(
                new List<ResidencyPolicyReadModel> { readModel }));

        // Act
        var result = await _sut.IsAllowedAsync("data", RegionRegistry.CN, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        ((bool)result).ShouldBeFalse();
    }

    [Fact]
    public async Task IsAllowedAsync_EmptyAllowedRegions_ShouldReturnTrue()
    {
        // Arrange
        var readModel = new ResidencyPolicyReadModel
        {
            DataCategory = "data",
            AllowedRegionCodes = [],
            IsActive = true
        };

        _readModelRepository.QueryAsync(
            Arg.Any<Func<IQueryable<ResidencyPolicyReadModel>, IQueryable<ResidencyPolicyReadModel>>>(),
            Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<ResidencyPolicyReadModel>>(
                new List<ResidencyPolicyReadModel> { readModel }));

        // Act
        var result = await _sut.IsAllowedAsync("data", RegionRegistry.US, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        ((bool)result).ShouldBeTrue();
    }

    // ================================================================
    // GetAllowedRegionsAsync (evaluation)
    // ================================================================

    [Fact]
    public async Task GetAllowedRegionsAsync_WithKnownRegions_ShouldReturnRegions()
    {
        // Arrange
        var readModel = new ResidencyPolicyReadModel
        {
            DataCategory = "data",
            AllowedRegionCodes = ["DE", "FR"],
            IsActive = true
        };

        _readModelRepository.QueryAsync(
            Arg.Any<Func<IQueryable<ResidencyPolicyReadModel>, IQueryable<ResidencyPolicyReadModel>>>(),
            Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<ResidencyPolicyReadModel>>(
                new List<ResidencyPolicyReadModel> { readModel }));

        // Act
        var result = await _sut.GetAllowedRegionsAsync("data", CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: regions => regions.Count.ShouldBe(2),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task GetAllowedRegionsAsync_EmptyAllowedRegions_ShouldReturnEmptyList()
    {
        // Arrange
        var readModel = new ResidencyPolicyReadModel
        {
            DataCategory = "data",
            AllowedRegionCodes = [],
            IsActive = true
        };

        _readModelRepository.QueryAsync(
            Arg.Any<Func<IQueryable<ResidencyPolicyReadModel>, IQueryable<ResidencyPolicyReadModel>>>(),
            Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<ResidencyPolicyReadModel>>(
                new List<ResidencyPolicyReadModel> { readModel }));

        // Act
        var result = await _sut.GetAllowedRegionsAsync("data", CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: regions => regions.Count.ShouldBe(0),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task GetAllowedRegionsAsync_Exception_ShouldReturnServiceError()
    {
        // Arrange
        _readModelRepository.QueryAsync(
            Arg.Any<Func<IQueryable<ResidencyPolicyReadModel>, IQueryable<ResidencyPolicyReadModel>>>(),
            Arg.Any<CancellationToken>())
            .Throws(new TimeoutException("DB error"));

        // Act
        var result = await _sut.GetAllowedRegionsAsync("data", CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }
}
