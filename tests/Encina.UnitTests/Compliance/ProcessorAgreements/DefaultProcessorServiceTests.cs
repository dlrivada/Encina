using Encina.Caching;
using Encina.Compliance.ProcessorAgreements;
using Encina.Compliance.ProcessorAgreements.Abstractions;
using Encina.Compliance.ProcessorAgreements.Aggregates;
using Encina.Compliance.ProcessorAgreements.Model;
using Encina.Compliance.ProcessorAgreements.ReadModels;
using Encina.Compliance.ProcessorAgreements.Services;
using Encina.Marten;
using Encina.Marten.Projections;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.ProcessorAgreements;

/// <summary>
/// Unit tests for <see cref="DefaultProcessorService"/>, including its tenant isolation (#3679)
/// and the registration completeness of <see cref="ServiceCollectionExtensions.AddEncinaProcessorAgreements"/>.
/// </summary>
public sealed class DefaultProcessorServiceTests
{
    private const string TenantA = "tenant-a";
    private const string TenantB = "tenant-b";
    private const string TenantRequiredCode = "processor.tenant_required";
    private const string NotFoundCode = "processor.not_found";

    private readonly IAggregateRepository<ProcessorAggregate> _repository;
    private readonly IReadModelRepository<ProcessorReadModel> _readModelRepository;
    private readonly ICacheProvider _cache;
    private readonly FakeTimeProvider _timeProvider;
    private readonly ILogger<DefaultProcessorService> _logger;
    private readonly IOptions<ProcessorAgreementOptions> _options;
    private readonly IRequestContextAccessor _requestContextAccessor;
    private readonly DefaultProcessorService _sut;

    public DefaultProcessorServiceTests()
    {
        _repository = Substitute.For<IAggregateRepository<ProcessorAggregate>>();
        _readModelRepository = Substitute.For<IReadModelRepository<ProcessorReadModel>>();
        _cache = Substitute.For<ICacheProvider>();
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 3, 25, 12, 0, 0, TimeSpan.Zero));
        _logger = NullLogger<DefaultProcessorService>.Instance;
        _options = Options.Create(new ProcessorAgreementOptions());
        _requestContextAccessor = Substitute.For<IRequestContextAccessor>();

        _sut = new DefaultProcessorService(
            _repository,
            _readModelRepository,
            _cache,
            _options,
            _timeProvider,
            _requestContextAccessor,
            _logger);
    }

    // ========================================================================
    // Constructor guard tests
    // ========================================================================

    [Fact]
    public void Constructor_NullRepository_ThrowsArgumentNullException()
    {
        var act = () => new DefaultProcessorService(
            null!, _readModelRepository, _cache, _options, _timeProvider, _requestContextAccessor, _logger);
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("repository");
    }

    [Fact]
    public void Constructor_NullReadModelRepository_ThrowsArgumentNullException()
    {
        var act = () => new DefaultProcessorService(
            _repository, null!, _cache, _options, _timeProvider, _requestContextAccessor, _logger);
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("readModelRepository");
    }

    [Fact]
    public void Constructor_NullCache_ThrowsArgumentNullException()
    {
        var act = () => new DefaultProcessorService(
            _repository, _readModelRepository, null!, _options, _timeProvider, _requestContextAccessor, _logger);
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("cache");
    }

    [Fact]
    public void Constructor_NullTimeProvider_ThrowsArgumentNullException()
    {
        var act = () => new DefaultProcessorService(
            _repository, _readModelRepository, _cache, _options, null!, _requestContextAccessor, _logger);
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("timeProvider");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new DefaultProcessorService(
            _repository, _readModelRepository, _cache, _options, _timeProvider, _requestContextAccessor, null!);
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

    [Fact]
    public async Task GetFullSubProcessorChainAsync_WhenALevelReturnsAStoreError_ReturnsTheErrorNotAPartialChain()
    {
        // Arrange
        var rootId = Guid.NewGuid();
        var child = new ProcessorReadModel { Id = Guid.NewGuid(), Name = "Sub1", Country = "US", ParentProcessorId = rootId };
        var storeError = EncinaErrors.Create("store.down", "Store unavailable");
        _readModelRepository.QueryAsync(
                Arg.Any<Func<IQueryable<ProcessorReadModel>, IQueryable<ProcessorReadModel>>>(),
                Arg.Any<CancellationToken>())
            .Returns(
                Right<EncinaError, IReadOnlyList<ProcessorReadModel>>(new List<ProcessorReadModel> { child }),
                Left<EncinaError, IReadOnlyList<ProcessorReadModel>>(storeError));

        // Act
        var result = await _sut.GetFullSubProcessorChainAsync(rootId);

        // Assert
        result.IsLeft.ShouldBeTrue();
        CodeOf(result).ShouldBe("store.down");
    }

    // ========================================================================
    // Tenant isolation: reads (#3679)
    // ========================================================================

    [Fact]
    public async Task GetAllProcessorsAsync_AsTenantA_ReturnsOnlyTenantAProcessors()
    {
        // Arrange
        var a = Processor(TenantA);
        UseStore(a, Processor(TenantB), Processor(null), Processor(TenantA, isRemoved: true));
        var sut = CreateSut(TenantA, multiTenant: true);

        // Act
        var result = await sut.GetAllProcessorsAsync();

        // Assert
        result.Match(Right: list => list.Select(p => p.Id).ToList(), Left: _ => []).ShouldBe([a.Id]);
    }

    [Fact]
    public async Task GetAllProcessorsAsync_TenancyOff_FiltersOnTheNullTenantWithNoConfiguration()
    {
        // Arrange
        var unscoped = Processor(null);
        UseStore(Processor(TenantA), Processor(TenantB), unscoped);
        var sut = CreateSut(ambientTenant: null);

        // Act
        var result = await sut.GetAllProcessorsAsync();

        // Assert
        result.Match(Right: list => list.Select(p => p.Id).ToList(), Left: _ => []).ShouldBe([unscoped.Id]);
    }

    [Fact]
    public async Task GetAllProcessorsAsync_EmptyAmbientTenant_IsTheNullTenantScope()
    {
        // Arrange
        var unscoped = Processor(null);
        UseStore(Processor(TenantA), unscoped);
        var sut = CreateSut(ambientTenant: string.Empty);

        // Act
        var result = await sut.GetAllProcessorsAsync();

        // Assert
        result.Match(Right: list => list.Select(p => p.Id).ToList(), Left: _ => []).ShouldBe([unscoped.Id]);
    }

    [Fact]
    public async Task GetSubProcessorsAsync_AsTenantA_NeverReturnsAChildOfAnotherTenant()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        var own = Processor(TenantA, parentId);
        UseStore(own, Processor(TenantB, parentId), Processor(TenantA));
        var sut = CreateSut(TenantA, multiTenant: true);

        // Act
        var result = await sut.GetSubProcessorsAsync(parentId);

        // Assert
        result.Match(Right: list => list.Select(p => p.Id).ToList(), Left: _ => []).ShouldBe([own.Id]);
    }

    [Fact]
    public async Task GetFullSubProcessorChainAsync_AsTenantA_FollowsOnlyTenantABranches()
    {
        // Arrange
        var rootId = Guid.NewGuid();
        var child = Processor(TenantA, rootId);
        var grandchild = Processor(TenantA, child.Id);
        var foreignChild = Processor(TenantB, rootId);
        UseStore(child, grandchild, foreignChild, Processor(TenantB, foreignChild.Id));
        var sut = CreateSut(TenantA, multiTenant: true);

        // Act
        var result = await sut.GetFullSubProcessorChainAsync(rootId);

        // Assert
        result.Match(Right: list => list.Select(p => p.Id).ToList(), Left: _ => [])
            .ShouldBe([child.Id, grandchild.Id]);
    }

    [Fact]
    public async Task GetProcessorAsync_ProcessorOfAnotherTenant_IsReportedAsNotFoundAndNotCached()
    {
        // Arrange
        var foreign = Processor(TenantB);
        _readModelRepository.GetByIdAsync(foreign.Id, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, ProcessorReadModel>(foreign));
        var sut = CreateSut(TenantA, multiTenant: true);

        // Act
        var result = await sut.GetProcessorAsync(foreign.Id);

        // Assert
        CodeOf(result).ShouldBe(NotFoundCode);
        await _cache.DidNotReceive().SetAsync(
            Arg.Any<string>(), Arg.Any<ProcessorReadModel>(), Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetProcessorAsync_CachedEntryOfAnotherTenant_IsAMissAndReReadThroughTheTenantCheck()
    {
        // Arrange
        var foreign = Processor(TenantB);
        _cache.GetAsync<ProcessorReadModel>(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(foreign);
        _readModelRepository.GetByIdAsync(foreign.Id, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, ProcessorReadModel>(foreign));
        var sut = CreateSut(TenantA, multiTenant: true);

        // Act
        var result = await sut.GetProcessorAsync(foreign.Id);

        // Assert
        CodeOf(result).ShouldBe(NotFoundCode);
        await _readModelRepository.Received(1).GetByIdAsync(foreign.Id, Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(TenantA, "pa:tenant:tenant-a:processor:")]
    [InlineData(null, "pa:tenant:-:processor:")]
    public async Task GetProcessorAsync_OwnProcessor_IsCachedUnderTheTenantScopedKey(string? tenant, string keyPrefix)
    {
        // Arrange
        var own = Processor(tenant);
        _readModelRepository.GetByIdAsync(own.Id, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, ProcessorReadModel>(own));
        var sut = CreateSut(tenant);

        // Act
        var result = await sut.GetProcessorAsync(own.Id);

        // Assert
        result.IsRight.ShouldBeTrue();
        await _cache.Received(1).SetAsync(
            keyPrefix + own.Id, own, Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>());
    }

    // ========================================================================
    // Tenant isolation: mutations (#3679)
    // ========================================================================

    [Fact]
    public async Task UpdateProcessorAsync_ProcessorOfAnotherTenant_IsNotFoundAndNothingIsSaved()
    {
        // Arrange
        var processorId = Guid.NewGuid();
        StubAggregate(processorId, TenantB);
        var sut = CreateSut(TenantA, multiTenant: true);

        // Act
        var result = await sut.UpdateProcessorAsync(processorId, "X", "DE", null, SubProcessorAuthorizationType.General);

        // Assert
        CodeOf(result).ShouldBe(NotFoundCode);
        await _repository.DidNotReceive().SaveAsync(Arg.Any<ProcessorAggregate>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveProcessorAsync_ProcessorOfAnotherTenant_IsNotFoundAndNothingIsSaved()
    {
        // Arrange
        var processorId = Guid.NewGuid();
        StubAggregate(processorId, TenantB);
        var sut = CreateSut(TenantA, multiTenant: true);

        // Act
        var result = await sut.RemoveProcessorAsync(processorId, "cleanup");

        // Assert
        CodeOf(result).ShouldBe(NotFoundCode);
        await _repository.DidNotReceive().SaveAsync(Arg.Any<ProcessorAggregate>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateProcessorAsync_OwnProcessor_SavesAndInvalidatesTheKeyOfTheAggregateTenant()
    {
        // Arrange
        var processorId = Guid.NewGuid();
        StubAggregate(processorId, TenantA);
        _repository.SaveAsync(Arg.Any<ProcessorAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));
        var sut = CreateSut(TenantA, multiTenant: true);

        // Act
        var result = await sut.UpdateProcessorAsync(processorId, "X", "DE", null, SubProcessorAuthorizationType.General);

        // Assert
        result.IsRight.ShouldBeTrue();
        await _cache.Received(1).RemoveAsync($"pa:tenant:tenant-a:processor:{processorId}", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveProcessorAsync_OwnProcessor_SavesAndInvalidatesTheKeyOfTheAggregateTenant()
    {
        // Arrange
        var processorId = Guid.NewGuid();
        StubAggregate(processorId, TenantA);
        _repository.SaveAsync(Arg.Any<ProcessorAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));
        var sut = CreateSut(TenantA, multiTenant: true);

        // Act
        var result = await sut.RemoveProcessorAsync(processorId, "cleanup");

        // Assert
        result.IsRight.ShouldBeTrue();
        await _cache.Received(1).RemoveAsync($"pa:tenant:tenant-a:processor:{processorId}", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterProcessorAsync_ParentOfAnotherTenant_IsRefusedAsNotFoundAndNothingIsCreated()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        StubAggregate(parentId, TenantB);
        var sut = CreateSut(TenantA, multiTenant: true);

        // Act
        var result = await sut.RegisterProcessorAsync("Sub", "DE", null, parentId, 1, SubProcessorAuthorizationType.Specific);

        // Assert
        CodeOf(result).ShouldBe(NotFoundCode);
        await _repository.DidNotReceive().CreateAsync(Arg.Any<ProcessorAggregate>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterProcessorAsync_MissingParent_IsRefusedAsNotFound()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        _repository.LoadAsync(parentId, Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, ProcessorAggregate>(EncinaErrors.Create("missing", "missing")));
        var sut = CreateSut(TenantA, multiTenant: true);

        // Act
        var result = await sut.RegisterProcessorAsync("Sub", "DE", null, parentId, 1, SubProcessorAuthorizationType.Specific);

        // Assert
        CodeOf(result).ShouldBe(NotFoundCode);
        await _repository.DidNotReceive().CreateAsync(Arg.Any<ProcessorAggregate>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterProcessorAsync_ParentOfTheSameTenant_CreatesTheSubProcessorUnderThatTenant()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        StubAggregate(parentId, TenantA);
        var created = CaptureCreated();
        var sut = CreateSut(TenantA, multiTenant: true);

        // Act
        var result = await sut.RegisterProcessorAsync("Sub", "DE", null, parentId, 1, SubProcessorAuthorizationType.Specific);

        // Assert
        result.IsRight.ShouldBeTrue();
        created().TenantId.ShouldBe(TenantA);
        created().ParentProcessorId.ShouldBe(parentId);
    }

    [Fact]
    public async Task RegisterProcessorAsync_ExplicitTenantContradictingTheAmbientOne_IsRefused()
    {
        // Arrange
        var sut = CreateSut(TenantA, multiTenant: true);

        // Act
        var result = await sut.RegisterProcessorAsync(
            "P", "DE", null, null, 0, SubProcessorAuthorizationType.General, tenantId: TenantB);

        // Assert
        CodeOf(result).ShouldBe(TenantRequiredCode);
        await _repository.DidNotReceive().CreateAsync(Arg.Any<ProcessorAggregate>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterProcessorAsync_ExplicitTenantEqualToTheAmbientOne_IsAccepted()
    {
        // Arrange
        var created = CaptureCreated();
        var sut = CreateSut(TenantA, multiTenant: true);

        // Act
        var result = await sut.RegisterProcessorAsync(
            "P", "DE", null, null, 0, SubProcessorAuthorizationType.General, tenantId: TenantA);

        // Assert
        result.IsRight.ShouldBeTrue();
        created().TenantId.ShouldBe(TenantA);
    }

    [Fact]
    public async Task RegisterProcessorAsync_NoExplicitTenant_DefaultsToTheAmbientTenant()
    {
        // Arrange
        var created = CaptureCreated();
        var sut = CreateSut(TenantA, multiTenant: true);

        // Act
        var result = await sut.RegisterProcessorAsync("P", "DE", null, null, 0, SubProcessorAuthorizationType.General);

        // Assert
        result.IsRight.ShouldBeTrue();
        created().TenantId.ShouldBe(TenantA);
    }

    [Fact]
    public async Task RegisterProcessorAsync_NoAmbientTenant_AcceptsTheExplicitTenant()
    {
        // Arrange
        var created = CaptureCreated();
        var sut = CreateSut(ambientTenant: null, multiTenant: true);

        // Act
        var result = await sut.RegisterProcessorAsync(
            "P", "DE", null, null, 0, SubProcessorAuthorizationType.General, tenantId: TenantB);

        // Assert
        result.IsRight.ShouldBeTrue();
        created().TenantId.ShouldBe(TenantB);
    }

    // ========================================================================
    // Tenant isolation: fail closed and opt-out (#3679)
    // ========================================================================

    [Fact]
    public async Task EveryOperation_NoTenantWhileTenancyIsRegistered_FailsClosedWithTenantRequired()
    {
        // Arrange
        var sut = CreateSut(ambientTenant: null, multiTenant: true);
        var id = Guid.NewGuid();

        // Act
        var codes = new[]
        {
            CodeOf(await sut.RegisterProcessorAsync("P", "DE", null, null, 0, SubProcessorAuthorizationType.General)),
            CodeOf(await sut.UpdateProcessorAsync(id, "P", "DE", null, SubProcessorAuthorizationType.General)),
            CodeOf(await sut.RemoveProcessorAsync(id, "r")),
            CodeOf(await sut.GetProcessorAsync(id)),
            CodeOf(await sut.GetAllProcessorsAsync()),
            CodeOf(await sut.GetSubProcessorsAsync(id)),
            CodeOf(await sut.GetFullSubProcessorChainAsync(id)),
        };

        // Assert
        codes.ShouldAllBe(code => code == TenantRequiredCode);
        _repository.ReceivedCalls().ShouldBeEmpty();
        _readModelRepository.ReceivedCalls().ShouldBeEmpty();
        _cache.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task GetAllProcessorsAsync_RequireTenantContextTrueWithoutTenancy_FailsClosed()
    {
        // Arrange
        var sut = CreateSut(ambientTenant: null, requireTenantContext: true);

        // Act
        var result = await sut.GetAllProcessorsAsync();

        // Assert
        CodeOf(result).ShouldBe(TenantRequiredCode);
    }

    [Fact]
    public async Task ExplicitOptOut_WhileTenancyIsRegistered_IsLoggedOnceAndRunsTheNullTenantScope()
    {
        // Arrange
        var unscoped = Processor(null);
        UseStore(Processor(TenantA), unscoped);
        var logger = new FakeLogger<DefaultProcessorService>();
        var sut = CreateSut(ambientTenant: null, requireTenantContext: false, multiTenant: true, logger: logger);

        // Act
        var first = await sut.GetAllProcessorsAsync();
        var second = await sut.GetAllProcessorsAsync();

        // Assert
        first.Match(Right: list => list.Select(p => p.Id).ToList(), Left: _ => []).ShouldBe([unscoped.Id]);
        second.IsRight.ShouldBeTrue();
        logger.Collector.GetSnapshot().Count(r => r.Id.Id == 9487).ShouldBe(1);
    }

    [Fact]
    public void ExplicitOptOut_WithoutTenancy_IsNotLogged()
    {
        // Arrange
        var logger = new FakeLogger<DefaultProcessorService>();

        // Act
        _ = CreateSut(ambientTenant: null, requireTenantContext: false, logger: logger);

        // Assert
        logger.Collector.GetSnapshot().ShouldNotContain(r => r.Id.Id == 9487);
    }

    [Fact]
    public async Task Refusal_IsLoggedWithTheOperationNameOnly()
    {
        // Arrange
        var logger = new FakeLogger<DefaultProcessorService>();
        var sut = CreateSut(ambientTenant: null, multiTenant: true, logger: logger);

        // Act
        await sut.GetAllProcessorsAsync();

        // Assert
        var record = logger.Collector.GetSnapshot().Single(r => r.Id.Id == 9486);
        record.Level.ShouldBe(LogLevel.Warning);
        record.Message.ShouldContain("Operation=GetAllProcessors");
    }

    [Fact]
    public void TenantRequired_IsAnInvalidRequestWithTheOperationAndNoIdentifier()
    {
        // Act
        var error = ProcessorAgreementErrors.TenantRequired("GetProcessor");

        // Assert
        error.GetCode().IfNone("").ShouldBe(TenantRequiredCode);
        error.GetCategory().ShouldBe(ErrorCategory.InvalidRequest);
        error.Message.ShouldContain("GetProcessor");
    }

    // ========================================================================
    // Registration completeness (AGENTS.md section 3)
    // ========================================================================

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AddEncinaProcessorAgreements_BuildsWithValidateOnBuildAndScopes_AndResolvesTheService(bool withTenancy)
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped(_ => Substitute.For<IAggregateRepository<ProcessorAggregate>>());
        services.AddScoped(_ => Substitute.For<IAggregateRepository<DPAAggregate>>());
        services.AddScoped(_ => Substitute.For<IReadModelRepository<ProcessorReadModel>>());
        services.AddScoped(_ => Substitute.For<IReadModelRepository<DPAReadModel>>());
        services.AddSingleton(Substitute.For<ICacheProvider>());
        services.AddScoped(_ => Substitute.For<IEncina>());
        if (withTenancy)
        {
            services.AddScoped(_ => Substitute.For<ITenantProvider>());
        }

        services.AddEncinaProcessorAgreements();

        // Act
        await using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        await using var scope = provider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IProcessorService>();
        var result = await service.GetAllProcessorsAsync();

        // Assert: with tenancy registered and no ambient tenant the module fails closed.
        service.ShouldBeOfType<DefaultProcessorService>();
        provider.GetRequiredService<IRequestContextAccessor>().ShouldBeOfType<RequestContextAccessor>();
        if (withTenancy)
        {
            CodeOf(result).ShouldBe(TenantRequiredCode);
        }
    }

    // ========================================================================
    // Helpers
    // ========================================================================

    private DefaultProcessorService CreateSut(
        string? ambientTenant,
        bool? requireTenantContext = null,
        bool multiTenant = false,
        ILogger<DefaultProcessorService>? logger = null)
    {
        var accessor = Substitute.For<IRequestContextAccessor>();
        var context = Substitute.For<IRequestContext>();
        context.TenantId.Returns(ambientTenant);
        accessor.RequestContext.Returns(context);

        return new DefaultProcessorService(
            _repository,
            _readModelRepository,
            _cache,
            Options.Create(new ProcessorAgreementOptions { RequireTenantContext = requireTenantContext }),
            _timeProvider,
            accessor,
            logger ?? _logger,
            multiTenant ? Substitute.For<ITenantProvider>() : null);
    }

    private static ProcessorReadModel Processor(string? tenantId, Guid? parentId = null, bool isRemoved = false) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = "Processor",
            Country = "DE",
            TenantId = tenantId,
            ParentProcessorId = parentId,
            IsRemoved = isRemoved
        };

    /// <summary>
    /// Makes the mocked read model repository apply the service's own query to an in-memory store,
    /// so the tenant predicate is exercised rather than bypassed.
    /// </summary>
    private void UseStore(params ProcessorReadModel[] store) =>
        _readModelRepository.QueryAsync(
                Arg.Any<Func<IQueryable<ProcessorReadModel>, IQueryable<ProcessorReadModel>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var query = call.Arg<Func<IQueryable<ProcessorReadModel>, IQueryable<ProcessorReadModel>>>();
                IReadOnlyList<ProcessorReadModel> rows = query(store.AsQueryable()).ToList();
                return Task.FromResult(Right<EncinaError, IReadOnlyList<ProcessorReadModel>>(rows));
            });

    private void StubAggregate(Guid processorId, string? tenantId) =>
        _repository.LoadAsync(processorId, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, ProcessorAggregate>(ProcessorAggregate.Register(
                processorId, "Processor", "DE", null, null, 0, SubProcessorAuthorizationType.General,
                _timeProvider.GetUtcNow(), tenantId)));

    private Func<ProcessorAggregate> CaptureCreated()
    {
        ProcessorAggregate? created = null;
        _repository.CreateAsync(Arg.Do<ProcessorAggregate>(a => created = a), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));
        return () => created.ShouldNotBeNull();
    }

    private static string CodeOf<T>(Either<EncinaError, T> result) =>
        result.Match(Right: _ => "<right>", Left: e => e.GetCode().IfNone(""));
}
