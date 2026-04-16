#pragma warning disable CA1859 // Contract tests intentionally use interface types

using Encina.Caching;
using Encina.Compliance.DPIA;
using Encina.Compliance.DPIA.Abstractions;
using Encina.Compliance.DPIA.Aggregates;
using Encina.Compliance.DPIA.ReadModels;
using Encina.Compliance.DPIA.Services;
using Encina.Marten;
using Encina.Marten.Projections;
using Shouldly;
using LanguageExt;
using Marten;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using static LanguageExt.Prelude;

namespace Encina.ContractTests.Compliance.DPIA;

/// <summary>
/// Contract tests verifying that <see cref="IDPIAService"/> behavioral guarantees
/// are upheld by the <see cref="DefaultDPIAService"/> implementation.
/// </summary>
[Trait("Category", "Contract")]
public class IDPIAServiceContractTests
{
    private static readonly DateTimeOffset FixedUtcNow = new(2026, 3, 16, 12, 0, 0, TimeSpan.Zero);

    private readonly IAggregateRepository<DPIAAggregate> _aggregateRepository =
        Substitute.For<IAggregateRepository<DPIAAggregate>>();

    private readonly IReadModelRepository<DPIAReadModel> _readModelRepository =
        Substitute.For<IReadModelRepository<DPIAReadModel>>();

    private readonly IDPIAAssessmentEngine _assessmentEngine =
        Substitute.For<IDPIAAssessmentEngine>();

    private readonly IDocumentSession _session =
        Substitute.For<IDocumentSession>();

    private readonly ICacheProvider _cache =
        Substitute.For<ICacheProvider>();

    private readonly FakeTimeProvider _timeProvider = new(FixedUtcNow);

    private readonly IDPIAService _sut;

    public IDPIAServiceContractTests()
    {
        _cache.GetAsync<DPIAReadModel>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((DPIAReadModel?)null);

        var options = Options.Create(new DPIAOptions());
        var logger = NullLogger<DefaultDPIAService>.Instance;

        _sut = new DefaultDPIAService(
            _aggregateRepository,
            _readModelRepository,
            _assessmentEngine,
            _session,
            _cache,
            _timeProvider,
            options,
            logger);
    }

    #region CreateAssessmentAsync

    [Fact]
    public async Task CreateAssessmentAsync_WithValidData_ReturnsRightWithNonEmptyGuid()
    {
        // Arrange
        _aggregateRepository.CreateAsync(Arg.Any<DPIAAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, Unit>(Unit.Default)));

        // Act
        var result = await _sut.CreateAssessmentAsync("MyApp.Commands.ProcessData");

        // Assert
        result.IsRight.ShouldBeTrue("CreateAssessmentAsync with valid data should return Right");
        result.IfRight(id => id.ShouldNotBe(Guid.Empty, "the returned Guid should not be empty"));
    }

    #endregion

    #region GetAssessmentAsync

    [Fact]
    public async Task GetAssessmentAsync_WithNonExistentId_ReturnsLeft()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        _readModelRepository.GetByIdAsync(nonExistentId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Left<EncinaError, DPIAReadModel>(
                DPIAErrors.AssessmentNotFound(nonExistentId))));

        // Act
        var result = await _sut.GetAssessmentAsync(nonExistentId);

        // Assert
        result.IsLeft.ShouldBeTrue("GetAssessmentAsync with non-existent ID should return Left");
    }

    #endregion

    #region ApproveAssessmentAsync

    [Fact]
    public async Task ApproveAssessmentAsync_WithNonExistentId_ReturnsLeft()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        _aggregateRepository.LoadAsync(nonExistentId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Left<EncinaError, DPIAAggregate>(
                DPIAErrors.AssessmentNotFound(nonExistentId))));

        // Act
        var result = await _sut.ApproveAssessmentAsync(nonExistentId, "admin");

        // Assert
        result.IsLeft.ShouldBeTrue("ApproveAssessmentAsync with non-existent ID should return Left");
    }

    #endregion

    #region GetAllAssessmentsAsync

    [Fact]
    public async Task GetAllAssessmentsAsync_ReturnsRight()
    {
        // Arrange
        _readModelRepository.QueryAsync(
                Arg.Any<Func<IQueryable<DPIAReadModel>, IQueryable<DPIAReadModel>>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(
                Right<EncinaError, IReadOnlyList<DPIAReadModel>>(
                    (IReadOnlyList<DPIAReadModel>)System.Array.Empty<DPIAReadModel>())));

        // Act
        var result = await _sut.GetAllAssessmentsAsync();

        // Assert
        result.IsRight.ShouldBeTrue("GetAllAssessmentsAsync should return Right");
    }

    #endregion

    #region GetExpiredAssessmentsAsync

    [Fact]
    public async Task GetExpiredAssessmentsAsync_ReturnsRight()
    {
        // Arrange
        _readModelRepository.QueryAsync(
                Arg.Any<Func<IQueryable<DPIAReadModel>, IQueryable<DPIAReadModel>>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(
                Right<EncinaError, IReadOnlyList<DPIAReadModel>>(
                    (IReadOnlyList<DPIAReadModel>)System.Array.Empty<DPIAReadModel>())));

        // Act
        var result = await _sut.GetExpiredAssessmentsAsync();

        // Assert
        result.IsRight.ShouldBeTrue("GetExpiredAssessmentsAsync should return Right");
    }

    #endregion
}
