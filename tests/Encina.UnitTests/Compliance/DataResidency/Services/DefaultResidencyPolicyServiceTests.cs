#pragma warning disable CA2012

using Encina.Caching;
using Encina.Compliance.DataResidency.Aggregates;
using Encina.Compliance.DataResidency.Model;
using Encina.Compliance.DataResidency.ReadModels;
using Encina.Compliance.DataResidency.Services;
using Encina.Marten;
using Encina.Marten.Projections;

using Shouldly;

using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.DataResidency.Services;

public class DefaultResidencyPolicyServiceTests
{
    private readonly IAggregateRepository<ResidencyPolicyAggregate> _repository;
    private readonly IReadModelRepository<ResidencyPolicyReadModel> _readModelRepository;
    private readonly ICacheProvider _cache;
    private readonly DefaultResidencyPolicyService _sut;

    public DefaultResidencyPolicyServiceTests()
    {
        _repository = Substitute.For<IAggregateRepository<ResidencyPolicyAggregate>>();
        _readModelRepository = Substitute.For<IReadModelRepository<ResidencyPolicyReadModel>>();
        _cache = Substitute.For<ICacheProvider>();

        _sut = new DefaultResidencyPolicyService(
            _repository, _readModelRepository, _cache, TimeProvider.System,
            NullLogger<DefaultResidencyPolicyService>.Instance);
    }

    #region CreatePolicyAsync

    [Fact]
    public async Task CreatePolicyAsync_ValidParams_ReturnsGuid()
    {
        _repository.CreateAsync(Arg.Any<ResidencyPolicyAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, LanguageExt.Unit>(unit)));
        _cache.RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var result = await _sut.CreatePolicyAsync(
            "healthcare-data", ["DE", "FR"], false, [],
            null, null, CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        result.Match(id => id.ShouldNotBe(Guid.Empty), _ => { });
    }

    [Fact]
    public async Task CreatePolicyAsync_RepositoryError_ReturnsLeft()
    {
        var error = EncinaErrors.Create("repo.error", "Failed");
        _repository.CreateAsync(Arg.Any<ResidencyPolicyAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Left<EncinaError, LanguageExt.Unit>(error)));

        var result = await _sut.CreatePolicyAsync(
            "data", ["DE"], false, [],
            null, null, CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task CreatePolicyAsync_Exception_ReturnsStoreError()
    {
        _repository.CreateAsync(Arg.Any<ResidencyPolicyAggregate>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("DB down"));

        var result = await _sut.CreatePolicyAsync(
            "data", ["DE"], false, [],
            null, null, CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region GetPolicyAsync

    [Fact]
    public async Task GetPolicyAsync_CacheHit_ReturnsCached()
    {
        var policyId = Guid.NewGuid();
        var cached = new ResidencyPolicyReadModel
        {
            Id = policyId,
            DataCategory = "data",
            AllowedRegionCodes = ["DE"],
            CreatedAtUtc = DateTimeOffset.UtcNow,
            LastModifiedAtUtc = DateTimeOffset.UtcNow
        };
        _cache.GetAsync<ResidencyPolicyReadModel>(
                $"dr:policy:{policyId}", Arg.Any<CancellationToken>())
            .Returns(cached);

        var result = await _sut.GetPolicyAsync(policyId, CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        await _repository.DidNotReceive().LoadAsync(policyId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPolicyAsync_CacheMiss_LoadsFromReadModelRepository()
    {
        var policyId = Guid.NewGuid();
        var readModel = new ResidencyPolicyReadModel
        {
            Id = policyId,
            DataCategory = "healthcare-data",
            AllowedRegionCodes = ["DE", "FR"],
            CreatedAtUtc = DateTimeOffset.UtcNow,
            LastModifiedAtUtc = DateTimeOffset.UtcNow
        };
        _cache.GetAsync<ResidencyPolicyReadModel>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((ResidencyPolicyReadModel?)null);
        _readModelRepository.GetByIdAsync(policyId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, ResidencyPolicyReadModel>(readModel)));
        _cache.SetAsync(
                Arg.Any<string>(), Arg.Any<ResidencyPolicyReadModel>(),
                Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var result = await _sut.GetPolicyAsync(policyId, CancellationToken.None);

        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task GetPolicyAsync_NotFound_ReturnsError()
    {
        var error = EncinaErrors.Create("not.found", "Not found");
        _cache.GetAsync<ResidencyPolicyReadModel>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((ResidencyPolicyReadModel?)null);
        _readModelRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Left<EncinaError, ResidencyPolicyReadModel>(error)));

        var result = await _sut.GetPolicyAsync(Guid.NewGuid(), CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task GetPolicyAsync_StoreException_ReturnsError()
    {
        _cache.GetAsync<ResidencyPolicyReadModel>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((ResidencyPolicyReadModel?)null);
        _readModelRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("DB down"));

        var result = await _sut.GetPolicyAsync(Guid.NewGuid(), CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
    }

    #endregion
}
