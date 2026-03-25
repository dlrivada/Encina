using Encina.Caching;
using Encina.Compliance.ProcessorAgreements.Aggregates;
using Encina.Compliance.ProcessorAgreements.Model;
using Encina.Compliance.ProcessorAgreements.ReadModels;
using Encina.Compliance.ProcessorAgreements.Services;
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

namespace Encina.UnitTests.Compliance.ProcessorAgreements;

/// <summary>
/// Unit tests for <see cref="DefaultProcessorService"/>.
/// </summary>
public sealed class DefaultProcessorServiceTests
{
    private readonly IAggregateRepository<ProcessorAggregate> _repository;
    private readonly IReadModelRepository<ProcessorReadModel> _readModelRepository;
    private readonly ICacheProvider _cache;
    private readonly FakeTimeProvider _timeProvider;
    private readonly ILogger<DefaultProcessorService> _logger;
    private readonly DefaultProcessorService _sut;

    public DefaultProcessorServiceTests()
    {
        _repository = Substitute.For<IAggregateRepository<ProcessorAggregate>>();
        _readModelRepository = Substitute.For<IReadModelRepository<ProcessorReadModel>>();
        _cache = Substitute.For<ICacheProvider>();
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 3, 25, 12, 0, 0, TimeSpan.Zero));
        _logger = NullLogger<DefaultProcessorService>.Instance;

        _sut = new DefaultProcessorService(
            _repository,
            _readModelRepository,
            _cache,
            _timeProvider,
            _logger);
    }

    // ========================================================================
    // Constructor guard tests
    // ========================================================================

    [Fact]
    public void Constructor_NullRepository_ThrowsArgumentNullException()
    {
        var act = () => new DefaultProcessorService(
            null!, _readModelRepository, _cache, _timeProvider, _logger);
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("repository");
    }

    [Fact]
    public void Constructor_NullReadModelRepository_ThrowsArgumentNullException()
    {
        var act = () => new DefaultProcessorService(
            _repository, null!, _cache, _timeProvider, _logger);
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("readModelRepository");
    }

    [Fact]
    public void Constructor_NullCache_ThrowsArgumentNullException()
    {
        var act = () => new DefaultProcessorService(
            _repository, _readModelRepository, null!, _timeProvider, _logger);
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("cache");
    }

    [Fact]
    public void Constructor_NullTimeProvider_ThrowsArgumentNullException()
    {
        var act = () => new DefaultProcessorService(
            _repository, _readModelRepository, _cache, null!, _logger);
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("timeProvider");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new DefaultProcessorService(
            _repository, _readModelRepository, _cache, _timeProvider, null!);
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("logger");
    }

    // ========================================================================
    // RegisterProcessorAsync tests
    // ========================================================================

    [Fact]
    public async Task RegisterProcessorAsync_WhenRepositorySucceeds_ShouldReturnGuid()
    {
        // Arrange
        _repository.CreateAsync(Arg.Any<ProcessorAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));

        // Act
        var result = await _sut.RegisterProcessorAsync(
            "TestProcessor", "DE", "test@example.com", null, 0,
            SubProcessorAuthorizationType.Specific);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task RegisterProcessorAsync_WhenRepositoryFails_ShouldReturnError()
    {
        // Arrange
        var error = EncinaErrors.Create("test.error", "Save failed");
        _repository.CreateAsync(Arg.Any<ProcessorAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, Unit>(error));

        // Act
        var result = await _sut.RegisterProcessorAsync(
            "TestProcessor", "DE", null, null, 0,
            SubProcessorAuthorizationType.General);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

#pragma warning disable CA2201
    [Fact]
    public async Task RegisterProcessorAsync_WhenExceptionThrown_ShouldReturnStoreError()
    {
        // Arrange
        _repository.CreateAsync(Arg.Any<ProcessorAggregate>(), Arg.Any<CancellationToken>())
            .Throws(new Exception("Connection lost"));

        // Act
        var result = await _sut.RegisterProcessorAsync(
            "TestProcessor", "DE", null, null, 0,
            SubProcessorAuthorizationType.Specific);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.Match(Right: _ => "", Left: e => e.GetCode().IfNone("")).ShouldBe("processor.store_error");
    }
#pragma warning restore CA2201

    // ========================================================================
    // UpdateProcessorAsync tests
    // ========================================================================

    [Fact]
    public async Task UpdateProcessorAsync_WhenNotFound_ShouldReturnNotFoundError()
    {
        // Arrange
        var processorId = Guid.NewGuid();
        var error = EncinaErrors.Create("not_found", "Not found");
        _repository.LoadAsync(processorId, Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, ProcessorAggregate>(error));

        // Act
        var result = await _sut.UpdateProcessorAsync(
            processorId, "Updated", "US", null, SubProcessorAuthorizationType.Specific);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.Match(Right: _ => "", Left: e => e.GetCode().IfNone("")).ShouldBe("processor.not_found");
    }

    [Fact]
    public async Task UpdateProcessorAsync_WhenInvalidOperation_ShouldReturnValidationError()
    {
        // Arrange
        var processorId = Guid.NewGuid();
        _repository.LoadAsync(processorId, Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Cannot update removed processor"));

        // Act
        var result = await _sut.UpdateProcessorAsync(
            processorId, "Updated", "US", null, SubProcessorAuthorizationType.Specific);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.Match(Right: _ => "", Left: e => e.GetCode().IfNone("")).ShouldBe("processor.validation_failed");
    }

    // ========================================================================
    // RemoveProcessorAsync tests
    // ========================================================================

    [Fact]
    public async Task RemoveProcessorAsync_WhenNotFound_ShouldReturnNotFoundError()
    {
        // Arrange
        var processorId = Guid.NewGuid();
        var error = EncinaErrors.Create("not_found", "Not found");
        _repository.LoadAsync(processorId, Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, ProcessorAggregate>(error));

        // Act
        var result = await _sut.RemoveProcessorAsync(processorId, "No longer needed");

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.Match(Right: _ => "", Left: e => e.GetCode().IfNone("")).ShouldBe("processor.not_found");
    }

    [Fact]
    public async Task RemoveProcessorAsync_WhenInvalidOperation_ShouldReturnValidationError()
    {
        // Arrange
        var processorId = Guid.NewGuid();
        _repository.LoadAsync(processorId, Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Already removed"));

        // Act
        var result = await _sut.RemoveProcessorAsync(processorId, "Cleanup");

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.Match(Right: _ => "", Left: e => e.GetCode().IfNone("")).ShouldBe("processor.validation_failed");
    }

    // ========================================================================
    // GetProcessorAsync tests
    // ========================================================================

    [Fact]
    public async Task GetProcessorAsync_WhenCached_ShouldReturnCachedValue()
    {
        // Arrange
        var processorId = Guid.NewGuid();
        var readModel = new ProcessorReadModel { Id = processorId, Name = "TestProcessor", Country = "DE" };
        _cache.GetAsync<ProcessorReadModel>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(readModel);

        // Act
        var result = await _sut.GetProcessorAsync(processorId);

        // Assert
        result.IsRight.ShouldBeTrue();
        await _readModelRepository.DidNotReceive()
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetProcessorAsync_WhenNotCached_ShouldQueryRepository()
    {
        // Arrange
        var processorId = Guid.NewGuid();
        var readModel = new ProcessorReadModel { Id = processorId, Name = "TestProcessor", Country = "DE" };
        _cache.GetAsync<ProcessorReadModel>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((ProcessorReadModel?)null);
        _readModelRepository.GetByIdAsync(processorId, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, ProcessorReadModel>(readModel));

        // Act
        var result = await _sut.GetProcessorAsync(processorId);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    // ========================================================================
    // GetAllProcessorsAsync tests
    // ========================================================================

#pragma warning disable CA2201
    [Fact]
    public async Task GetAllProcessorsAsync_WhenExceptionThrown_ShouldReturnStoreError()
    {
        // Arrange
        _readModelRepository.QueryAsync(
                Arg.Any<Func<IQueryable<ProcessorReadModel>, IQueryable<ProcessorReadModel>>>(),
                Arg.Any<CancellationToken>())
            .Throws(new Exception("Query failed"));

        // Act
        var result = await _sut.GetAllProcessorsAsync();

        // Assert
        result.IsLeft.ShouldBeTrue();
    }
#pragma warning restore CA2201

    // ========================================================================
    // GetSubProcessorsAsync tests
    // ========================================================================

    [Fact]
    public async Task GetSubProcessorsAsync_WhenSucceeds_ShouldReturnSubProcessors()
    {
        // Arrange
        var processorId = Guid.NewGuid();
        var subs = new List<ProcessorReadModel>
        {
            new() { Id = Guid.NewGuid(), Name = "Sub1", Country = "US", ParentProcessorId = processorId }
        } as IReadOnlyList<ProcessorReadModel>;
        _readModelRepository.QueryAsync(
                Arg.Any<Func<IQueryable<ProcessorReadModel>, IQueryable<ProcessorReadModel>>>(),
                Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<ProcessorReadModel>>(subs));

        // Act
        var result = await _sut.GetSubProcessorsAsync(processorId);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    // ========================================================================
    // GetProcessorHistoryAsync tests
    // ========================================================================

    [Fact]
    public async Task GetProcessorHistoryAsync_ShouldReturnNotAvailableError()
    {
        // Act
        var result = await _sut.GetProcessorHistoryAsync(Guid.NewGuid());

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.Match(Right: _ => "", Left: e => e.GetCode().IfNone("")).ShouldBe("processor.store_error");
    }

    // ========================================================================
    // GetFullSubProcessorChainAsync tests
    // ========================================================================

    [Fact]
    public async Task GetFullSubProcessorChainAsync_WithNoChildren_ShouldReturnEmptyList()
    {
        // Arrange
        var processorId = Guid.NewGuid();
        var empty = new List<ProcessorReadModel>() as IReadOnlyList<ProcessorReadModel>;
        _readModelRepository.QueryAsync(
                Arg.Any<Func<IQueryable<ProcessorReadModel>, IQueryable<ProcessorReadModel>>>(),
                Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<ProcessorReadModel>>(empty));

        // Act
        var result = await _sut.GetFullSubProcessorChainAsync(processorId);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(Right: chain => chain.Count, Left: _ => -1).ShouldBe(0);
    }

#pragma warning disable CA2201
    [Fact]
    public async Task GetFullSubProcessorChainAsync_WhenExceptionThrown_ShouldReturnStoreError()
    {
        // Arrange
        var processorId = Guid.NewGuid();
        _readModelRepository.QueryAsync(
                Arg.Any<Func<IQueryable<ProcessorReadModel>, IQueryable<ProcessorReadModel>>>(),
                Arg.Any<CancellationToken>())
            .Throws(new Exception("BFS query failed"));

        // Act
        var result = await _sut.GetFullSubProcessorChainAsync(processorId);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }
#pragma warning restore CA2201
}
