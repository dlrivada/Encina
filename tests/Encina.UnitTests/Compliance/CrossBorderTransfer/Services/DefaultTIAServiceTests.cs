#pragma warning disable CA2012

using Encina.Caching;
using Encina.Compliance.CrossBorderTransfer.Aggregates;
using Encina.Compliance.CrossBorderTransfer.Model;
using Encina.Compliance.CrossBorderTransfer.ReadModels;
using Encina.Compliance.CrossBorderTransfer.Services;
using Encina.Marten;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.CrossBorderTransfer.Services;

public class DefaultTIAServiceTests
{
    private readonly IAggregateRepository<TIAAggregate> _repository;
    private readonly ICacheProvider _cache;
    private readonly TimeProvider _timeProvider;
    private readonly DefaultTIAService _sut;

    public DefaultTIAServiceTests()
    {
        _repository = Substitute.For<IAggregateRepository<TIAAggregate>>();
        _cache = Substitute.For<ICacheProvider>();
        _timeProvider = TimeProvider.System;

        _sut = new DefaultTIAService(_repository, _cache, _timeProvider,
            NullLogger<DefaultTIAService>.Instance);
    }

    #region CreateTIAAsync

    [Fact]
    public async Task CreateTIAAsync_ValidParams_ReturnsGuid()
    {
        _repository.CreateAsync(Arg.Any<TIAAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, LanguageExt.Unit>(unit)));

        var result = await _sut.CreateTIAAsync("DE", "US", "personal-data", "admin");

        result.IsRight.Should().BeTrue();
        result.Match(id => id.Should().NotBeEmpty(), _ => { });
    }

    [Fact]
    public async Task CreateTIAAsync_RepositoryError_ReturnsLeft()
    {
        var error = EncinaErrors.Create("repo.error", "Failed");
        _repository.CreateAsync(Arg.Any<TIAAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Left<EncinaError, LanguageExt.Unit>(error)));

        var result = await _sut.CreateTIAAsync("DE", "US", "personal-data", "admin");

        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public async Task CreateTIAAsync_RepositoryThrows_ReturnsStoreError()
    {
        _repository.CreateAsync(Arg.Any<TIAAggregate>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("DB down"));

        var result = await _sut.CreateTIAAsync("DE", "US", "personal-data", "admin");

        result.IsLeft.Should().BeTrue();
    }

    #endregion

    #region AssessRiskAsync

    [Fact]
    public async Task AssessRiskAsync_ValidAggregate_ReturnsUnit()
    {
        var aggregate = TIAAggregate.Create(Guid.NewGuid(), "DE", "US", "data", "admin");
        _repository.LoadAsync(aggregate.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, TIAAggregate>(aggregate)));
        _repository.SaveAsync(Arg.Any<TIAAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, LanguageExt.Unit>(unit)));
        _cache.RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var result = await _sut.AssessRiskAsync(aggregate.Id, 0.5, "findings", "assessor");

        result.IsRight.Should().BeTrue();
    }

    [Fact]
    public async Task AssessRiskAsync_AggregateNotFound_ReturnsTIANotFound()
    {
        var error = EncinaErrors.Create("not.found", "Not found");
        _repository.LoadAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Left<EncinaError, TIAAggregate>(error)));

        var result = await _sut.AssessRiskAsync(Guid.NewGuid(), 0.5, null, "assessor");

        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public async Task AssessRiskAsync_InvalidStateTransition_ReturnsError()
    {
        _repository.LoadAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Invalid state"));

        var result = await _sut.AssessRiskAsync(Guid.NewGuid(), 0.5, null, "assessor");

        result.IsLeft.Should().BeTrue();
    }

    #endregion

    #region RequireSupplementaryMeasureAsync

    [Fact]
    public async Task RequireSupplementaryMeasureAsync_ValidAggregate_ReturnsUnit()
    {
        var aggregate = TIAAggregate.Create(Guid.NewGuid(), "DE", "US", "data", "admin");
        aggregate.AssessRisk(0.8, "High risk", "assessor");
        _repository.LoadAsync(aggregate.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, TIAAggregate>(aggregate)));
        _repository.SaveAsync(Arg.Any<TIAAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, LanguageExt.Unit>(unit)));
        _cache.RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var result = await _sut.RequireSupplementaryMeasureAsync(
            aggregate.Id, SupplementaryMeasureType.Technical, "AES-256");

        result.IsRight.Should().BeTrue();
    }

    [Fact]
    public async Task RequireSupplementaryMeasureAsync_NotFound_ReturnsError()
    {
        var error = EncinaErrors.Create("not.found", "Not found");
        _repository.LoadAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Left<EncinaError, TIAAggregate>(error)));

        var result = await _sut.RequireSupplementaryMeasureAsync(
            Guid.NewGuid(), SupplementaryMeasureType.Technical, "AES-256");

        result.IsLeft.Should().BeTrue();
    }

    #endregion

    #region SubmitForDPOReviewAsync

    [Fact]
    public async Task SubmitForDPOReviewAsync_ValidAggregate_ReturnsUnit()
    {
        var aggregate = TIAAggregate.Create(Guid.NewGuid(), "DE", "US", "data", "admin");
        aggregate.AssessRisk(0.5, "OK", "assessor");
        _repository.LoadAsync(aggregate.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, TIAAggregate>(aggregate)));
        _repository.SaveAsync(Arg.Any<TIAAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, LanguageExt.Unit>(unit)));
        _cache.RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var result = await _sut.SubmitForDPOReviewAsync(aggregate.Id, "submitter");

        result.IsRight.Should().BeTrue();
    }

    #endregion

    #region CompleteDPOReviewAsync

    [Fact]
    public async Task CompleteDPOReviewAsync_Approved_ReturnsUnit()
    {
        var aggregate = TIAAggregate.Create(Guid.NewGuid(), "DE", "US", "data", "admin");
        aggregate.AssessRisk(0.5, "OK", "assessor");
        aggregate.SubmitForDPOReview("submitter");
        _repository.LoadAsync(aggregate.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, TIAAggregate>(aggregate)));
        _repository.SaveAsync(Arg.Any<TIAAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, LanguageExt.Unit>(unit)));
        _cache.RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var result = await _sut.CompleteDPOReviewAsync(aggregate.Id, approved: true, "reviewer", null);

        result.IsRight.Should().BeTrue();
    }

    [Fact]
    public async Task CompleteDPOReviewAsync_Rejected_ReturnsUnit()
    {
        var aggregate = TIAAggregate.Create(Guid.NewGuid(), "DE", "US", "data", "admin");
        aggregate.AssessRisk(0.5, "OK", "assessor");
        aggregate.SubmitForDPOReview("submitter");
        _repository.LoadAsync(aggregate.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, TIAAggregate>(aggregate)));
        _repository.SaveAsync(Arg.Any<TIAAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, LanguageExt.Unit>(unit)));
        _cache.RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var result = await _sut.CompleteDPOReviewAsync(aggregate.Id, approved: false, "reviewer", "Insufficient measures");

        result.IsRight.Should().BeTrue();
    }

    #endregion

    #region GetTIAAsync

    [Fact]
    public async Task GetTIAAsync_CacheHit_ReturnsCachedReadModel()
    {
        var tiaId = Guid.NewGuid();
        var cached = new TIAReadModel
        {
            Id = tiaId,
            SourceCountryCode = "DE",
            DestinationCountryCode = "US",
            DataCategory = "data",
            Status = TIAStatus.Completed,
            RequiredSupplementaryMeasures = [],
            CreatedAtUtc = DateTimeOffset.UtcNow,
            LastModifiedAtUtc = DateTimeOffset.UtcNow
        };
        _cache.GetAsync<TIAReadModel>($"cbt:tia:{tiaId}", Arg.Any<CancellationToken>())
            .Returns(cached);

        var result = await _sut.GetTIAAsync(tiaId);

        result.IsRight.Should().BeTrue();
        await _repository.DidNotReceive().LoadAsync(tiaId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetTIAAsync_CacheMiss_LoadsFromRepository()
    {
        var aggregate = TIAAggregate.Create(Guid.NewGuid(), "DE", "US", "data", "admin");
        _cache.GetAsync<TIAReadModel>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((TIAReadModel?)null);
        _repository.LoadAsync(aggregate.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, TIAAggregate>(aggregate)));
        _cache.SetAsync(Arg.Any<string>(), Arg.Any<TIAReadModel>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var result = await _sut.GetTIAAsync(aggregate.Id);

        result.IsRight.Should().BeTrue();
    }

    [Fact]
    public async Task GetTIAAsync_NotFound_ReturnsError()
    {
        var error = EncinaErrors.Create("not.found", "Not found");
        _cache.GetAsync<TIAReadModel>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((TIAReadModel?)null);
        _repository.LoadAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Left<EncinaError, TIAAggregate>(error)));

        var result = await _sut.GetTIAAsync(Guid.NewGuid());

        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public async Task GetTIAAsync_StoreException_ReturnsStoreError()
    {
        _cache.GetAsync<TIAReadModel>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((TIAReadModel?)null);
        _repository.LoadAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("DB down"));

        var result = await _sut.GetTIAAsync(Guid.NewGuid());

        result.IsLeft.Should().BeTrue();
    }

    #endregion

    #region GetTIAByRouteAsync

    [Fact]
    public async Task GetTIAByRouteAsync_CacheHit_ReturnsCachedReadModel()
    {
        var cached = new TIAReadModel
        {
            Id = Guid.NewGuid(),
            SourceCountryCode = "DE",
            DestinationCountryCode = "US",
            DataCategory = "data",
            Status = TIAStatus.Completed,
            RequiredSupplementaryMeasures = [],
            CreatedAtUtc = DateTimeOffset.UtcNow,
            LastModifiedAtUtc = DateTimeOffset.UtcNow
        };
        _cache.GetAsync<TIAReadModel>("cbt:tia:route:DE:US:data", Arg.Any<CancellationToken>())
            .Returns(cached);

        var result = await _sut.GetTIAByRouteAsync("DE", "US", "data");

        result.IsRight.Should().BeTrue();
    }

    [Fact]
    public async Task GetTIAByRouteAsync_CacheMiss_ReturnsNotFound()
    {
        _cache.GetAsync<TIAReadModel>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((TIAReadModel?)null);

        var result = await _sut.GetTIAByRouteAsync("DE", "US", "data");

        result.IsLeft.Should().BeTrue();
    }

    #endregion
}
