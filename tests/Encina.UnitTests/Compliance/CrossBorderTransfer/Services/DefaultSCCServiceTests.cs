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

public class DefaultSCCServiceTests
{
    private readonly IAggregateRepository<SCCAgreementAggregate> _repository;
    private readonly ICacheProvider _cache;
    private readonly TimeProvider _timeProvider;
    private readonly DefaultSCCService _sut;

    private static readonly DateTimeOffset Now = new(2026, 4, 1, 10, 0, 0, TimeSpan.Zero);

    public DefaultSCCServiceTests()
    {
        _repository = Substitute.For<IAggregateRepository<SCCAgreementAggregate>>();
        _cache = Substitute.For<ICacheProvider>();
        _timeProvider = TimeProvider.System;

        _sut = new DefaultSCCService(_repository, _cache, _timeProvider,
            NullLogger<DefaultSCCService>.Instance);
    }

    #region RegisterAgreementAsync

    [Fact]
    public async Task RegisterAgreementAsync_ValidParams_ReturnsGuid()
    {
        _repository.CreateAsync(Arg.Any<SCCAgreementAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, LanguageExt.Unit>(unit)));

        var result = await _sut.RegisterAgreementAsync(
            "processor-1", SCCModule.ControllerToProcessor, "v2021", Now);

        result.IsRight.Should().BeTrue();
        result.Match(id => id.Should().NotBeEmpty(), _ => { });
    }

    [Fact]
    public async Task RegisterAgreementAsync_RepositoryError_ReturnsLeft()
    {
        var error = EncinaErrors.Create("repo.error", "Failed");
        _repository.CreateAsync(Arg.Any<SCCAgreementAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Left<EncinaError, LanguageExt.Unit>(error)));

        var result = await _sut.RegisterAgreementAsync(
            "processor-1", SCCModule.ControllerToProcessor, "v2021", Now);

        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public async Task RegisterAgreementAsync_RepositoryThrows_ReturnsStoreError()
    {
        _repository.CreateAsync(Arg.Any<SCCAgreementAggregate>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("DB down"));

        var result = await _sut.RegisterAgreementAsync(
            "processor-1", SCCModule.ControllerToProcessor, "v2021", Now);

        result.IsLeft.Should().BeTrue();
    }

    #endregion

    #region AddSupplementaryMeasureAsync

    [Fact]
    public async Task AddSupplementaryMeasureAsync_ValidAggregate_ReturnsUnit()
    {
        var aggregate = SCCAgreementAggregate.Register(
            Guid.NewGuid(), "processor-1", SCCModule.ControllerToProcessor, "v2021", Now);
        _repository.LoadAsync(aggregate.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, SCCAgreementAggregate>(aggregate)));
        _repository.SaveAsync(Arg.Any<SCCAgreementAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, LanguageExt.Unit>(unit)));
        _cache.RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var result = await _sut.AddSupplementaryMeasureAsync(
            aggregate.Id, SupplementaryMeasureType.Technical, "AES-256");

        result.IsRight.Should().BeTrue();
    }

    [Fact]
    public async Task AddSupplementaryMeasureAsync_NotFound_ReturnsError()
    {
        var error = EncinaErrors.Create("not.found", "Not found");
        _repository.LoadAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Left<EncinaError, SCCAgreementAggregate>(error)));

        var result = await _sut.AddSupplementaryMeasureAsync(
            Guid.NewGuid(), SupplementaryMeasureType.Technical, "AES-256");

        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public async Task AddSupplementaryMeasureAsync_AlreadyRevoked_ReturnsError()
    {
        _repository.LoadAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Already revoked"));

        var result = await _sut.AddSupplementaryMeasureAsync(
            Guid.NewGuid(), SupplementaryMeasureType.Technical, "AES-256");

        result.IsLeft.Should().BeTrue();
    }

    #endregion

    #region RevokeAgreementAsync

    [Fact]
    public async Task RevokeAgreementAsync_ValidAggregate_ReturnsUnit()
    {
        var aggregate = SCCAgreementAggregate.Register(
            Guid.NewGuid(), "processor-1", SCCModule.ControllerToProcessor, "v2021", Now);
        _repository.LoadAsync(aggregate.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, SCCAgreementAggregate>(aggregate)));
        _repository.SaveAsync(Arg.Any<SCCAgreementAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, LanguageExt.Unit>(unit)));
        _cache.RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var result = await _sut.RevokeAgreementAsync(aggregate.Id, "Breach detected", "admin");

        result.IsRight.Should().BeTrue();
    }

    [Fact]
    public async Task RevokeAgreementAsync_AlreadyRevoked_ReturnsError()
    {
        _repository.LoadAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Already revoked"));

        var result = await _sut.RevokeAgreementAsync(Guid.NewGuid(), "Breach", "admin");

        result.IsLeft.Should().BeTrue();
    }

    #endregion

    #region GetAgreementAsync

    [Fact]
    public async Task GetAgreementAsync_CacheHit_ReturnsCachedReadModel()
    {
        var agreementId = Guid.NewGuid();
        var cached = new SCCAgreementReadModel
        {
            Id = agreementId,
            ProcessorId = "proc-1",
            Module = SCCModule.ControllerToProcessor,
            Version = "v2021",
            ExecutedAtUtc = Now,
            IsRevoked = false,
            SupplementaryMeasures = []
        };
        _cache.GetAsync<SCCAgreementReadModel>($"cbt:scc:{agreementId}", Arg.Any<CancellationToken>())
            .Returns(cached);

        var result = await _sut.GetAgreementAsync(agreementId);

        result.IsRight.Should().BeTrue();
        await _repository.DidNotReceive().LoadAsync(agreementId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAgreementAsync_CacheMiss_LoadsFromRepository()
    {
        var aggregate = SCCAgreementAggregate.Register(
            Guid.NewGuid(), "proc-1", SCCModule.ControllerToProcessor, "v2021", Now);
        _cache.GetAsync<SCCAgreementReadModel>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((SCCAgreementReadModel?)null);
        _repository.LoadAsync(aggregate.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, SCCAgreementAggregate>(aggregate)));
        _cache.SetAsync(Arg.Any<string>(), Arg.Any<SCCAgreementReadModel>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var result = await _sut.GetAgreementAsync(aggregate.Id);

        result.IsRight.Should().BeTrue();
    }

    [Fact]
    public async Task GetAgreementAsync_NotFound_ReturnsError()
    {
        var error = EncinaErrors.Create("not.found", "Not found");
        _cache.GetAsync<SCCAgreementReadModel>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((SCCAgreementReadModel?)null);
        _repository.LoadAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Left<EncinaError, SCCAgreementAggregate>(error)));

        var result = await _sut.GetAgreementAsync(Guid.NewGuid());

        result.IsLeft.Should().BeTrue();
    }

    #endregion

    #region ValidateAgreementAsync

    [Fact]
    public async Task ValidateAgreementAsync_NoProjection_ReturnsInvalidResult()
    {
        var result = await _sut.ValidateAgreementAsync("processor-1", SCCModule.ControllerToProcessor);

        result.IsRight.Should().BeTrue();
        result.Match(
            r => r.IsValid.Should().BeFalse(),
            _ => { });
    }

    #endregion
}
