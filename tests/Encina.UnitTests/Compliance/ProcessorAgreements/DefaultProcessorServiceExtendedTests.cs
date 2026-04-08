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
/// Extended unit tests for <see cref="DefaultProcessorService"/> covering UpdateProcessorAsync (success),
/// RemoveProcessorAsync (success), GetAllProcessorsAsync (success), GetSubProcessorsAsync (error),
/// and GetFullSubProcessorChainAsync (with children).
/// </summary>
public sealed class DefaultProcessorServiceExtendedTests
{
    private readonly IAggregateRepository<ProcessorAggregate> _repository;
    private readonly IReadModelRepository<ProcessorReadModel> _readModelRepository;
    private readonly ICacheProvider _cache;
    private readonly FakeTimeProvider _timeProvider;
    private readonly ILogger<DefaultProcessorService> _logger;
    private readonly DefaultProcessorService _sut;

    public DefaultProcessorServiceExtendedTests()
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
    // UpdateProcessorAsync tests (success path)
    // ========================================================================

#pragma warning disable CA2012
    [Fact]
    public async Task UpdateProcessorAsync_WhenAggregateFoundAndSaveSucceeds_ShouldReturnRight()
    {
        // Arrange
        var processorId = Guid.NewGuid();
        var aggregate = CreateActiveProcessorAggregate(processorId);

        _repository.LoadAsync(processorId, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, ProcessorAggregate>(aggregate));
        _repository.SaveAsync(Arg.Any<ProcessorAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));

        // Act
        var result = await _sut.UpdateProcessorAsync(
            processorId, "UpdatedName", "US", "updated@example.com",
            SubProcessorAuthorizationType.General);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task UpdateProcessorAsync_WhenSaveFails_ShouldReturnError()
    {
        // Arrange
        var processorId = Guid.NewGuid();
        var aggregate = CreateActiveProcessorAggregate(processorId);
        var saveError = EncinaErrors.Create("save.error", "Save failed");

        _repository.LoadAsync(processorId, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, ProcessorAggregate>(aggregate));
        _repository.SaveAsync(Arg.Any<ProcessorAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, Unit>(saveError));

        // Act
        var result = await _sut.UpdateProcessorAsync(
            processorId, "UpdatedName", "US", null,
            SubProcessorAuthorizationType.Specific);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

#pragma warning disable CA2201
    [Fact]
    public async Task UpdateProcessorAsync_WhenGenericExceptionThrown_ShouldReturnStoreError()
    {
        // Arrange
        var processorId = Guid.NewGuid();
        _repository.LoadAsync(processorId, Arg.Any<CancellationToken>())
            .Throws(new Exception("Connection lost"));

        // Act
        var result = await _sut.UpdateProcessorAsync(
            processorId, "Updated", "US", null,
            SubProcessorAuthorizationType.Specific);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.Match(Right: _ => "", Left: e => e.GetCode().IfNone("")).ShouldBe("processor.store_error");
    }
#pragma warning restore CA2201

    // ========================================================================
    // RemoveProcessorAsync tests (success path)
    // ========================================================================

    [Fact]
    public async Task RemoveProcessorAsync_WhenAggregateFoundAndSaveSucceeds_ShouldReturnRight()
    {
        // Arrange
        var processorId = Guid.NewGuid();
        var aggregate = CreateActiveProcessorAggregate(processorId);

        _repository.LoadAsync(processorId, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, ProcessorAggregate>(aggregate));
        _repository.SaveAsync(Arg.Any<ProcessorAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));

        // Act
        var result = await _sut.RemoveProcessorAsync(processorId, "No longer needed");

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task RemoveProcessorAsync_WhenSaveFails_ShouldReturnError()
    {
        // Arrange
        var processorId = Guid.NewGuid();
        var aggregate = CreateActiveProcessorAggregate(processorId);
        var saveError = EncinaErrors.Create("save.error", "Save failed");

        _repository.LoadAsync(processorId, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, ProcessorAggregate>(aggregate));
        _repository.SaveAsync(Arg.Any<ProcessorAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, Unit>(saveError));

        // Act
        var result = await _sut.RemoveProcessorAsync(processorId, "Cleanup");

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

#pragma warning disable CA2201
    [Fact]
    public async Task RemoveProcessorAsync_WhenGenericExceptionThrown_ShouldReturnStoreError()
    {
        // Arrange
        var processorId = Guid.NewGuid();
        _repository.LoadAsync(processorId, Arg.Any<CancellationToken>())
            .Throws(new Exception("Connection lost"));

        // Act
        var result = await _sut.RemoveProcessorAsync(processorId, "Cleanup");

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.Match(Right: _ => "", Left: e => e.GetCode().IfNone("")).ShouldBe("processor.store_error");
    }
#pragma warning restore CA2201

    // ========================================================================
    // GetAllProcessorsAsync tests (success path)
    // ========================================================================

    [Fact]
    public async Task GetAllProcessorsAsync_WhenSucceeds_ShouldReturnProcessorList()
    {
        // Arrange
        var processors = new List<ProcessorReadModel>
        {
            new() { Id = Guid.NewGuid(), Name = "Processor1", Country = "DE" },
            new() { Id = Guid.NewGuid(), Name = "Processor2", Country = "US" }
        } as IReadOnlyList<ProcessorReadModel>;

        _readModelRepository.QueryAsync(
                Arg.Any<Func<IQueryable<ProcessorReadModel>, IQueryable<ProcessorReadModel>>>(),
                Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<ProcessorReadModel>>(processors));

        // Act
        var result = await _sut.GetAllProcessorsAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(Right: list => list.Count, Left: _ => -1).ShouldBe(2);
    }

    [Fact]
    public async Task GetAllProcessorsAsync_WhenQueryReturnsError_ShouldReturnError()
    {
        // Arrange
        var error = EncinaErrors.Create("query.error", "Query failed");
        _readModelRepository.QueryAsync(
                Arg.Any<Func<IQueryable<ProcessorReadModel>, IQueryable<ProcessorReadModel>>>(),
                Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, IReadOnlyList<ProcessorReadModel>>(error));

        // Act
        var result = await _sut.GetAllProcessorsAsync();

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    // ========================================================================
    // GetSubProcessorsAsync tests (error path)
    // ========================================================================

#pragma warning disable CA2201
    [Fact]
    public async Task GetSubProcessorsAsync_WhenExceptionThrown_ShouldReturnStoreError()
    {
        // Arrange
        var processorId = Guid.NewGuid();
        _readModelRepository.QueryAsync(
                Arg.Any<Func<IQueryable<ProcessorReadModel>, IQueryable<ProcessorReadModel>>>(),
                Arg.Any<CancellationToken>())
            .Throws(new Exception("Query failed"));

        // Act
        var result = await _sut.GetSubProcessorsAsync(processorId);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.Match(Right: _ => "", Left: e => e.GetCode().IfNone("")).ShouldBe("processor.store_error");
    }
#pragma warning restore CA2201

    [Fact]
    public async Task GetSubProcessorsAsync_WhenQueryReturnsError_ShouldPropagateError()
    {
        // Arrange
        var processorId = Guid.NewGuid();
        var error = EncinaErrors.Create("query.error", "Query failed");
        _readModelRepository.QueryAsync(
                Arg.Any<Func<IQueryable<ProcessorReadModel>, IQueryable<ProcessorReadModel>>>(),
                Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, IReadOnlyList<ProcessorReadModel>>(error));

        // Act
        var result = await _sut.GetSubProcessorsAsync(processorId);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    // ========================================================================
    // GetFullSubProcessorChainAsync tests (with children)
    // ========================================================================

    [Fact]
    public async Task GetFullSubProcessorChainAsync_WithChildren_ShouldReturnFullChain()
    {
        // Arrange
        var rootId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var grandchildId = Guid.NewGuid();

        var child = new ProcessorReadModel
        {
            Id = childId, Name = "Sub1", Country = "US",
            ParentProcessorId = rootId, Depth = 1
        };
        var grandchild = new ProcessorReadModel
        {
            Id = grandchildId, Name = "Sub2", Country = "FR",
            ParentProcessorId = childId, Depth = 2
        };

        var empty = new List<ProcessorReadModel>() as IReadOnlyList<ProcessorReadModel>;

        // First call (rootId) returns child, second (childId) returns grandchild, third (grandchildId) returns empty
        _readModelRepository.QueryAsync(
                Arg.Any<Func<IQueryable<ProcessorReadModel>, IQueryable<ProcessorReadModel>>>(),
                Arg.Any<CancellationToken>())
            .Returns(
                Right<EncinaError, IReadOnlyList<ProcessorReadModel>>(
                    new List<ProcessorReadModel> { child }),
                Right<EncinaError, IReadOnlyList<ProcessorReadModel>>(
                    new List<ProcessorReadModel> { grandchild }),
                Right<EncinaError, IReadOnlyList<ProcessorReadModel>>(empty));

        // Act
        var result = await _sut.GetFullSubProcessorChainAsync(rootId);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(Right: chain => chain.Count, Left: _ => -1).ShouldBe(2);
    }

    // ========================================================================
    // GetProcessorAsync tests (not found path)
    // ========================================================================

    [Fact]
    public async Task GetProcessorAsync_WhenNotFound_ShouldReturnNotFoundError()
    {
        // Arrange
        var processorId = Guid.NewGuid();
        _cache.GetAsync<ProcessorReadModel>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((ProcessorReadModel?)null);
        var error = EncinaErrors.Create("not_found", "Not found");
        _readModelRepository.GetByIdAsync(processorId, Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, ProcessorReadModel>(error));

        // Act
        var result = await _sut.GetProcessorAsync(processorId);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.Match(Right: _ => "", Left: e => e.GetCode().IfNone("")).ShouldBe("processor.not_found");
    }

#pragma warning disable CA2201
    [Fact]
    public async Task GetProcessorAsync_WhenExceptionThrown_ShouldReturnStoreError()
    {
        // Arrange
        var processorId = Guid.NewGuid();
        _cache.GetAsync<ProcessorReadModel>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((ProcessorReadModel?)null);
        _readModelRepository.GetByIdAsync(processorId, Arg.Any<CancellationToken>())
            .Throws(new Exception("Connection failed"));

        // Act
        var result = await _sut.GetProcessorAsync(processorId);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.Match(Right: _ => "", Left: e => e.GetCode().IfNone("")).ShouldBe("processor.store_error");
    }
#pragma warning restore CA2201
#pragma warning restore CA2012

    // ========================================================================
    // Helpers
    // ========================================================================

    private ProcessorAggregate CreateActiveProcessorAggregate(Guid processorId)
    {
        var occurredAtUtc = _timeProvider.GetUtcNow();
        return ProcessorAggregate.Register(
            processorId, "TestProcessor", "DE", "test@example.com",
            null, 0, SubProcessorAuthorizationType.Specific, occurredAtUtc);
    }
}
