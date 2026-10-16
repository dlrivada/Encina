using Encina.Caching;
using Encina.Compliance.DataSubjectRights;
using Encina.Compliance.DataSubjectRights.Abstractions;
using Encina.Compliance.DataSubjectRights.Aggregates;
using Encina.Compliance.DataSubjectRights.Projections;
using Encina.Compliance.DataSubjectRights.Services;
using Encina.Compliance.GDPR;
using Encina.Marten;
using Encina.Marten.Projections;
using Shouldly;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

#pragma warning disable CA2012 // Use ValueTasks correctly (NSubstitute Returns with ValueTask)

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.DataSubjectRights.Services;

/// <summary>
/// Unit tests for <see cref="DefaultDSRService"/>.
/// </summary>
public class DefaultDSRServiceTests
{
    private readonly IAggregateRepository<DSRRequestAggregate> _repository;
    private readonly IReadModelRepository<DSRRequestReadModel> _readModelRepository;
    private readonly IPersonalDataLocator _locator;
    private readonly IDataErasureExecutor _erasureExecutor;
    private readonly IDataPortabilityExporter _portabilityExporter;
    private readonly IProcessingActivityRegistry _processingActivityRegistry;
    private readonly ICacheProvider _cache;
    private readonly FakeTimeProvider _timeProvider;
    private readonly ILogger<DefaultDSRService> _logger;
    private readonly IEncina _encina;
    private readonly IOptions<DataSubjectRightsOptions> _optionsAccessor;
    private readonly DefaultDSRService _sut;

    private static readonly DateTimeOffset Now = new(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);

    public DefaultDSRServiceTests()
    {
        _repository = Substitute.For<IAggregateRepository<DSRRequestAggregate>>();
        _readModelRepository = Substitute.For<IReadModelRepository<DSRRequestReadModel>>();
        _locator = Substitute.For<IPersonalDataLocator>();
        _erasureExecutor = Substitute.For<IDataErasureExecutor>();
        _portabilityExporter = Substitute.For<IDataPortabilityExporter>();
        _processingActivityRegistry = Substitute.For<IProcessingActivityRegistry>();
        _cache = Substitute.For<ICacheProvider>();
        _timeProvider = new FakeTimeProvider(Now);
        _logger = NullLogger<DefaultDSRService>.Instance;
        _encina = Substitute.For<IEncina>();

        _optionsAccessor = Options.Create(new DataSubjectRightsOptions());

        _sut = CreateService(_optionsAccessor);
    }

    private DefaultDSRService CreateService(IOptions<DataSubjectRightsOptions> options) =>
        new(
            _repository,
            _readModelRepository,
            _locator,
            _erasureExecutor,
            _portabilityExporter,
            _processingActivityRegistry,
            _cache,
            options,
            _timeProvider,
            _logger,
            _encina);

    #region Constructor Validation

    [Fact]
    public void Constructor_NullRepository_ShouldThrow()
    {
        Should.Throw<ArgumentNullException>(() => new DefaultDSRService(
            null!, _readModelRepository, _locator, _erasureExecutor,
            _portabilityExporter, _processingActivityRegistry, _cache,
            _optionsAccessor, _timeProvider, _logger))
            .ParamName.ShouldBe("repository");
    }

    [Fact]
    public void Constructor_NullReadModelRepository_ShouldThrow()
    {
        Should.Throw<ArgumentNullException>(() => new DefaultDSRService(
            _repository, null!, _locator, _erasureExecutor,
            _portabilityExporter, _processingActivityRegistry, _cache,
            _optionsAccessor, _timeProvider, _logger))
            .ParamName.ShouldBe("readModelRepository");
    }

    [Fact]
    public void Constructor_NullLocator_ShouldThrow()
    {
        Should.Throw<ArgumentNullException>(() => new DefaultDSRService(
            _repository, _readModelRepository, null!, _erasureExecutor,
            _portabilityExporter, _processingActivityRegistry, _cache,
            _optionsAccessor, _timeProvider, _logger))
            .ParamName.ShouldBe("locator");
    }

    [Fact]
    public void Constructor_NullEncina_ShouldNotThrow()
    {
        Should.NotThrow(() => new DefaultDSRService(
            _repository, _readModelRepository, _locator, _erasureExecutor,
            _portabilityExporter, _processingActivityRegistry, _cache,
            _optionsAccessor, _timeProvider, _logger, encina: null));
    }

    #endregion

    #region SubmitRequestAsync

    [Fact]
    public async Task SubmitRequestAsync_ValidRequest_ShouldReturnGuid()
    {
        // Arrange
        _repository.CreateAsync(Arg.Any<DSRRequestAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, Unit>(unit)));

        // Act
        var result = await _sut.SubmitRequestAsync("subject-1", DataSubjectRight.Access);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(id => id.ShouldNotBe(Guid.Empty), _ => { });
    }

    [Fact]
    public async Task SubmitRequestAsync_RepositoryFails_ShouldReturnError()
    {
        // Arrange
        var error = EncinaError.New("Repository error");
        _repository.CreateAsync(Arg.Any<DSRRequestAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Left<EncinaError, Unit>(error)));

        // Act
        var result = await _sut.SubmitRequestAsync("subject-1", DataSubjectRight.Erasure);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task SubmitRequestAsync_ExceptionThrown_ShouldReturnServiceError()
    {
        // Arrange
        _repository.CreateAsync(Arg.Any<DSRRequestAggregate>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("DB error"));

        // Act
        var result = await _sut.SubmitRequestAsync("subject-1", DataSubjectRight.Access);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region VerifyIdentityAsync

    [Fact]
    public async Task VerifyIdentityAsync_ValidRequest_ShouldReturnUnit()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        var aggregate = DSRRequestAggregate.Submit(
            requestId, "subject-1", DataSubjectRight.Access, Now);
        aggregate.ClearUncommittedEvents();

        _repository.LoadAsync(requestId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, DSRRequestAggregate>(aggregate)));
        _repository.SaveAsync(Arg.Any<DSRRequestAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, Unit>(unit)));

        // Act
        var result = await _sut.VerifyIdentityAsync(requestId, "admin-1");

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task VerifyIdentityAsync_RequestNotFound_ShouldReturnError()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        _repository.LoadAsync(requestId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Left<EncinaError, DSRRequestAggregate>(EncinaError.New("Not found"))));

        // Act
        var result = await _sut.VerifyIdentityAsync(requestId, "admin-1");

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region CompleteRequestAsync

    [Fact]
    public async Task CompleteRequestAsync_ValidRequest_ShouldReturnUnit()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        var aggregate = CreateInProgressAggregate(requestId);

        _repository.LoadAsync(requestId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, DSRRequestAggregate>(aggregate)));
        _repository.SaveAsync(Arg.Any<DSRRequestAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, Unit>(unit)));

        // Act
        var result = await _sut.CompleteRequestAsync(requestId);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task CompleteRequestAsync_InvalidStateTransition_ShouldReturnError()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        var aggregate = DSRRequestAggregate.Submit(
            requestId, "subject-1", DataSubjectRight.Access, Now);
        aggregate.ClearUncommittedEvents();

        _repository.LoadAsync(requestId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, DSRRequestAggregate>(aggregate)));

        // Act — Complete from Received status should fail
        var result = await _sut.CompleteRequestAsync(requestId);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region DenyRequestAsync

    [Fact]
    public async Task DenyRequestAsync_ValidRequest_ShouldReturnUnit()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        var aggregate = DSRRequestAggregate.Submit(
            requestId, "subject-1", DataSubjectRight.Access, Now);
        aggregate.ClearUncommittedEvents();

        _repository.LoadAsync(requestId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, DSRRequestAggregate>(aggregate)));
        _repository.SaveAsync(Arg.Any<DSRRequestAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, Unit>(unit)));

        // Act
        var result = await _sut.DenyRequestAsync(
            requestId, DSRRejectionReason.ManifestlyUnfounded, "Manifestly unfounded");

        // Assert
        result.IsRight.ShouldBeTrue();
        aggregate.RejectionReason.ShouldBe(DSRRejectionReason.ManifestlyUnfounded);
        aggregate.RejectionExplanation.ShouldBe("Manifestly unfounded");
    }

    public static TheoryData<DSRRejectionReason> CurrentLawReasons =>
        new(Enum.GetValues<DSRRejectionReason>().Where(r => !r.IsDraftLawGround));

    private void ArrangeDeniableRequest(Guid requestId, DSRRequestAggregate? aggregate = null)
    {
        aggregate ??= DSRRequestAggregate.Submit(requestId, "subject-1", DataSubjectRight.Access, Now);
        aggregate.ClearUncommittedEvents();

        _repository.LoadAsync(requestId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, DSRRequestAggregate>(aggregate)));
        _repository.SaveAsync(Arg.Any<DSRRequestAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, Unit>(unit)));
    }

    [Theory]
    [MemberData(nameof(CurrentLawReasons))]
    public async Task DenyRequestAsync_DefaultOptions_AcceptsEveryCurrentLawReason(DSRRejectionReason reason)
    {
        var requestId = Guid.NewGuid();
        ArrangeDeniableRequest(requestId);

        var result = await _sut.DenyRequestAsync(requestId, reason, "Explanation");

        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task DenyRequestAsync_DefaultOptions_RejectsAbusiveIntentAsNotEnabled()
    {
        var requestId = Guid.NewGuid();
        ArrangeDeniableRequest(requestId);

        var result = await _sut.DenyRequestAsync(requestId, DSRRejectionReason.AbusiveIntent, "Explanation");

        var error = result.LeftAsEnumerable().Single();
        error.GetCode().IfNone(string.Empty).ShouldBe(DSRErrors.RejectionReasonNotEnabledCode);
        await _repository.DidNotReceive().LoadAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DenyRequestAsync_OmnibusOptionOn_AcceptsAbusiveIntent()
    {
        var requestId = Guid.NewGuid();
        ArrangeDeniableRequest(requestId);
        var sut = CreateService(Options.Create(new DataSubjectRightsOptions { EnableOmnibusArticle12RefusalGrounds = true }));

        var result = await sut.DenyRequestAsync(requestId, DSRRejectionReason.AbusiveIntent, "Explanation");

        result.IsRight.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task DenyRequestAsync_BlankExplanation_ReturnsInvalidRequestWithoutLoading(string explanation)
    {
        var result = await _sut.DenyRequestAsync(Guid.NewGuid(), DSRRejectionReason.Other, explanation);

        result.LeftAsEnumerable().Single().GetCode().IfNone(string.Empty).ShouldBe(DSRErrors.InvalidRequestCode);
        await _repository.DidNotReceive().LoadAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DenyRequestAsync_UndefinedReason_ReturnsInvalidRequestWithoutLoading()
    {
        var result = await _sut.DenyRequestAsync(Guid.NewGuid(), (DSRRejectionReason)0, "Explanation");

        result.LeftAsEnumerable().Single().GetCode().IfNone(string.Empty).ShouldBe(DSRErrors.InvalidRequestCode);
        await _repository.DidNotReceive().LoadAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DenyRequestAsync_UnknownRequest_ReturnsRequestNotFound()
    {
        var requestId = Guid.NewGuid();
        _repository.LoadAsync(requestId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Left<EncinaError, DSRRequestAggregate>(EncinaErrors.Create("test.missing", "missing"))));

        var result = await _sut.DenyRequestAsync(requestId, DSRRejectionReason.Other, "Explanation");

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task DenyRequestAsync_TerminalRequest_ReturnsInvalidRequest()
    {
        var requestId = Guid.NewGuid();
        var aggregate = DSRRequestAggregate.Submit(requestId, "subject-1", DataSubjectRight.Access, Now);
        aggregate.Deny(DSRRejectionReason.Other, "first", Now);
        ArrangeDeniableRequest(requestId, aggregate);

        var result = await _sut.DenyRequestAsync(requestId, DSRRejectionReason.Other, "second");

        result.LeftAsEnumerable().Single().GetCode().IfNone(string.Empty).ShouldBe(DSRErrors.InvalidRequestCode);
    }

    [Fact]
    public async Task DenyRequestAsync_SaveFails_ReturnsStoreError()
    {
        var requestId = Guid.NewGuid();
        ArrangeDeniableRequest(requestId);
        _repository.SaveAsync(Arg.Any<DSRRequestAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Left<EncinaError, Unit>(EncinaErrors.Create("test.save", "save failed"))));

        var result = await _sut.DenyRequestAsync(requestId, DSRRejectionReason.Other, "Explanation");

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task DenyRequestAsync_RepositoryThrows_ReturnsServiceError()
    {
        var requestId = Guid.NewGuid();
        _repository.LoadAsync(requestId, Arg.Any<CancellationToken>())
            .Throws(new InvalidCastException("boom"));

        var result = await _sut.DenyRequestAsync(requestId, DSRRejectionReason.Other, "Explanation");

        result.LeftAsEnumerable().Single().GetCode().IfNone(string.Empty).ShouldBe(DSRErrors.ServiceErrorCode);
    }

    #endregion

    #region HandleAccessAsync

    [Fact]
    public async Task HandleAccessAsync_ValidRequest_ShouldReturnAccessResponse()
    {
        // Arrange
        var locations = new List<PersonalDataLocation>
        {
            new()
            {
                EntityType = typeof(object),
                EntityId = "user-1",
                FieldName = "Email",
                Category = PersonalDataCategory.Contact,
                IsErasable = true,
                IsPortable = true,
                HasLegalRetention = false
            }
        };

        _locator.LocateAllDataAsync("subject-1", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, IReadOnlyList<PersonalDataLocation>>(
                (IReadOnlyList<PersonalDataLocation>)locations)));

        var request = new AccessRequest("subject-1", IncludeProcessingActivities: false);

        // Act
        var result = await _sut.HandleAccessAsync(request);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            response =>
            {
                response.SubjectId.ShouldBe("subject-1");
                response.Data.Count.ShouldBe(1);
            },
            _ => { });
    }

    [Fact]
    public async Task HandleAccessAsync_LocatorFails_ShouldReturnError()
    {
        // Arrange
        var error = EncinaError.New("Locator failed");
        _locator.LocateAllDataAsync("subject-1", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Left<EncinaError, IReadOnlyList<PersonalDataLocation>>(error)));

        var request = new AccessRequest("subject-1", IncludeProcessingActivities: false);

        // Act
        var result = await _sut.HandleAccessAsync(request);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task HandleAccessAsync_NullRequest_ShouldThrow()
    {
        // Act
        Func<Task> act = async () => await _sut.HandleAccessAsync(null!);

        // Assert
        await Should.ThrowAsync<ArgumentNullException>(act);
    }

    #endregion

    #region HandleErasureAsync

    [Fact]
    public async Task HandleErasureAsync_ValidRequest_ShouldReturnErasureResult()
    {
        // Arrange
        var erasureResult = new ErasureResult
        {
            FieldsErased = 5,
            FieldsRetained = 1,
            FieldsFailed = 0,
            RetentionReasons = [],
            Exemptions = []
        };
        _erasureExecutor.EraseAsync("subject-1", Arg.Any<ErasureScope>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, ErasureResult>(erasureResult)));
        _encina.Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Unit>(unit)));

        var request = new ErasureRequest("subject-1", ErasureReason.ConsentWithdrawn, null);

        // Act
        var result = await _sut.HandleErasureAsync(request);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            r => r.FieldsErased.ShouldBe(5),
            _ => { });
    }

    [Fact]
    public async Task HandleErasureAsync_ExecutorFails_ShouldReturnError()
    {
        // Arrange
        var error = EncinaError.New("Erasure failed");
        _erasureExecutor.EraseAsync("subject-1", Arg.Any<ErasureScope>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Left<EncinaError, ErasureResult>(error)));

        var request = new ErasureRequest("subject-1", ErasureReason.ConsentWithdrawn, null);

        // Act
        var result = await _sut.HandleErasureAsync(request);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region HandlePortabilityAsync

    [Fact]
    public async Task HandlePortabilityAsync_ValidRequest_ShouldReturnPortabilityResponse()
    {
        // Arrange
        var response = new PortabilityResponse
        {
            SubjectId = "subject-1",
            ExportedData = new ExportedData
            {
                Content = [0x01, 0x02],
                ContentType = "application/json",
                FileName = "export.json",
                Format = ExportFormat.JSON,
                FieldCount = 10
            },
            GeneratedAtUtc = Now
        };

        _portabilityExporter.ExportAsync("subject-1", ExportFormat.JSON, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, PortabilityResponse>(response)));

        var request = new PortabilityRequest("subject-1", ExportFormat.JSON, null);

        // Act
        var result = await _sut.HandlePortabilityAsync(request);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            r => r.SubjectId.ShouldBe("subject-1"),
            _ => { });
    }

    #endregion

    #region HandleObjectionAsync

    [Fact]
    public async Task HandleObjectionAsync_ValidRequest_ShouldReturnUnit()
    {
        // Arrange
        var request = new ObjectionRequest("subject-1", "direct-marketing", "I object to marketing");

        // Act
        var result = await _sut.HandleObjectionAsync(request);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task HandleObjectionAsync_NullRequest_ShouldThrow()
    {
        // Act
        Func<Task> act = async () => await _sut.HandleObjectionAsync(null!);

        // Assert
        await Should.ThrowAsync<ArgumentNullException>(act);
    }

    #endregion

    #region GetRequestAsync

    [Fact]
    public async Task GetRequestAsync_Cached_ShouldReturnFromCache()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        var readModel = CreateReadModel(requestId);

        _cache.GetAsync<DSRRequestReadModel>($"dsr:request:{requestId}", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<DSRRequestReadModel?>(readModel));

        // Act
        var result = await _sut.GetRequestAsync(requestId);

        // Assert
        result.IsRight.ShouldBeTrue();
        await _readModelRepository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetRequestAsync_NotCached_ShouldQueryRepository()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        var readModel = CreateReadModel(requestId);

        _cache.GetAsync<DSRRequestReadModel>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<DSRRequestReadModel?>(null));
        _readModelRepository.GetByIdAsync(requestId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, DSRRequestReadModel>(readModel)));

        // Act
        var result = await _sut.GetRequestAsync(requestId);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task GetRequestAsync_NotFound_ShouldReturnError()
    {
        // Arrange
        var requestId = Guid.NewGuid();

        _cache.GetAsync<DSRRequestReadModel>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<DSRRequestReadModel?>(null));
        _readModelRepository.GetByIdAsync(requestId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Left<EncinaError, DSRRequestReadModel>(EncinaError.New("Not found"))));

        // Act
        var result = await _sut.GetRequestAsync(requestId);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region HasActiveRestrictionAsync

    [Fact]
    public async Task HasActiveRestrictionAsync_CachedTrue_ShouldReturnTrue()
    {
        // Arrange
        _cache.GetAsync<bool?>("dsr:restriction:subject-1", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<bool?>(true));

        // Act
        var result = await _sut.HasActiveRestrictionAsync("subject-1");

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(value => value.ShouldBeTrue(), _ => { });
    }

    [Fact]
    public async Task HasActiveRestrictionAsync_NotCached_WithRestrictions_ShouldReturnTrue()
    {
        // Arrange
        _cache.GetAsync<bool?>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<bool?>(null));

        var readModels = new List<DSRRequestReadModel>
        {
            CreateReadModel(Guid.NewGuid(), DSRRequestStatus.Received, DataSubjectRight.Restriction)
        };

        _readModelRepository.QueryAsync(
                Arg.Any<Func<IQueryable<DSRRequestReadModel>, IQueryable<DSRRequestReadModel>>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(
                Right<EncinaError, IReadOnlyList<DSRRequestReadModel>>(
                    (IReadOnlyList<DSRRequestReadModel>)readModels)));

        // Act
        var result = await _sut.HasActiveRestrictionAsync("subject-1");

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(value => value.ShouldBeTrue(), _ => { });
    }

    [Fact]
    public async Task HasActiveRestrictionAsync_NotCached_WithoutRestrictions_ShouldReturnFalse()
    {
        // Arrange
        _cache.GetAsync<bool?>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<bool?>(null));

        _readModelRepository.QueryAsync(
                Arg.Any<Func<IQueryable<DSRRequestReadModel>, IQueryable<DSRRequestReadModel>>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(
                Right<EncinaError, IReadOnlyList<DSRRequestReadModel>>(
                    (IReadOnlyList<DSRRequestReadModel>)System.Array.Empty<DSRRequestReadModel>())));

        // Act
        var result = await _sut.HasActiveRestrictionAsync("subject-1");

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(value => value.ShouldBeFalse(), _ => { });
    }

    #endregion

    #region GetRequestHistoryAsync

    [Fact]
    public async Task GetRequestHistoryAsync_ShouldReturnUnavailableError()
    {
        // Act
        var result = await _sut.GetRequestHistoryAsync(Guid.NewGuid());

        // Assert — event history not yet implemented
        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region Helpers

    private static DSRRequestAggregate CreateInProgressAggregate(Guid requestId)
    {
        var aggregate = DSRRequestAggregate.Submit(
            requestId, "subject-1", DataSubjectRight.Access, Now);
        aggregate.Verify("admin-1", Now.AddHours(1));
        aggregate.StartProcessing("operator-1", Now.AddHours(2));
        aggregate.ClearUncommittedEvents();
        return aggregate;
    }

    private static DSRRequestReadModel CreateReadModel(
        Guid id,
        DSRRequestStatus status = DSRRequestStatus.Received,
        DataSubjectRight rightType = DataSubjectRight.Access)
    {
        return new DSRRequestReadModel
        {
            Id = id,
            SubjectId = "subject-1",
            RightType = rightType,
            Status = status,
            ReceivedAtUtc = Now,
            DeadlineAtUtc = Now.AddDays(30),
            LastModifiedAtUtc = Now,
            Version = 1
        };
    }

    #endregion
}
