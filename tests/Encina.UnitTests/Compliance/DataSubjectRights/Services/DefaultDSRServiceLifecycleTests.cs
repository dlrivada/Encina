using Encina.Caching;
using Encina.Compliance.DataSubjectRights;
using Encina.Compliance.DataSubjectRights.Aggregates;
using Encina.Compliance.DataSubjectRights.Projections;
using Encina.Compliance.DataSubjectRights.Services;
using Encina.Compliance.GDPR;
using Encina.Marten;
using Encina.Marten.Projections;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using Shouldly;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.DataSubjectRights.Services;

/// <summary>
/// Failure and edge paths of the <see cref="DefaultDSRService"/> lifecycle commands and queries
/// (not found, invalid state, store failure, unexpected exception).
/// </summary>
#pragma warning disable CA2012 // Use ValueTasks correctly (NSubstitute Returns with ValueTask)
public sealed class DefaultDSRServiceLifecycleTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);

    private readonly IAggregateRepository<DSRRequestAggregate> _repository =
        Substitute.For<IAggregateRepository<DSRRequestAggregate>>();
    private readonly IReadModelRepository<DSRRequestReadModel> _readModels =
        Substitute.For<IReadModelRepository<DSRRequestReadModel>>();
    private readonly ICacheProvider _cache = Substitute.For<ICacheProvider>();
    private readonly FakeTimeProvider _time = new(Now);
    private readonly DefaultDSRService _sut;

    public DefaultDSRServiceLifecycleTests()
    {
        _sut = new DefaultDSRService(
            _repository,
            _readModels,
            Substitute.For<IPersonalDataLocator>(),
            Substitute.For<IDataErasureExecutor>(),
            Substitute.For<IDataPortabilityExporter>(),
            Substitute.For<IProcessingActivityRegistry>(),
            _cache,
            Options.Create(new DataSubjectRightsOptions()),
            _time,
            new RequestContextAccessor(),
            NullLogger<DefaultDSRService>.Instance);
    }

    public enum Command
    {
        VerifyIdentity,
        StartProcessing,
        Complete,
        Deny,
        Extend,
        Expire
    }

    public static TheoryData<Command> Commands => new(Enum.GetValues<Command>());

    private ValueTask<Either<EncinaError, Unit>> Run(Command command, Guid id) => command switch
    {
        Command.VerifyIdentity => _sut.VerifyIdentityAsync(id, "admin"),
        Command.StartProcessing => _sut.StartProcessingAsync(id, "admin"),
        Command.Complete => _sut.CompleteRequestAsync(id),
        Command.Deny => _sut.DenyRequestAsync(id, DSRRejectionReason.Other, "explanation"),
        Command.Extend => _sut.ExtendDeadlineAsync(id, "complex"),
        _ => _sut.ExpireRequestAsync(id)
    };

    private static DSRRequestAggregate Aggregate(Command command, Guid id)
    {
        var aggregate = DSRRequestAggregate.Submit(id, "subject-1", DataSubjectRight.Access, Now);
        if (command is Command.StartProcessing)
        {
            aggregate.Verify("admin", Now);
        }

        if (command is Command.Complete)
        {
            aggregate.Verify("admin", Now);
            aggregate.StartProcessing("admin", Now);
        }

        aggregate.ClearUncommittedEvents();
        return aggregate;
    }

    private void ArrangeLoad(Guid id, DSRRequestAggregate aggregate, bool saveSucceeds = true)
    {
        _repository.LoadAsync(id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, DSRRequestAggregate>(aggregate)));
        _repository.SaveAsync(Arg.Any<DSRRequestAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(saveSucceeds
                ? Right<EncinaError, Unit>(unit)
                : Left<EncinaError, Unit>(EncinaErrors.Create("test.save", "save failed"))));
    }

    [Theory]
    [MemberData(nameof(Commands))]
    public async Task Command_ValidRequest_Succeeds(Command command)
    {
        var id = Guid.NewGuid();
        var aggregate = Aggregate(command, id);
        ArrangeLoad(id, aggregate);

        var result = await Run(command, id);

        result.IsRight.ShouldBeTrue();
        aggregate.Status.ShouldBe(command switch
        {
            Command.VerifyIdentity => DSRRequestStatus.IdentityVerified,
            Command.StartProcessing => DSRRequestStatus.InProgress,
            Command.Complete => DSRRequestStatus.Completed,
            Command.Deny => DSRRequestStatus.Rejected,
            Command.Extend => DSRRequestStatus.Extended,
            _ => DSRRequestStatus.Expired
        });
        aggregate.UncommittedEvents.Count.ShouldBe(1);
        await _repository.Received(1).SaveAsync(aggregate, Arg.Any<CancellationToken>());
    }

    [Theory]
    [MemberData(nameof(Commands))]
    public async Task Command_UnknownRequest_ReturnsRequestNotFound(Command command)
    {
        var id = Guid.NewGuid();
        _repository.LoadAsync(id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Left<EncinaError, DSRRequestAggregate>(EncinaErrors.Create("test.missing", "missing"))));

        var result = await Run(command, id);

        result.LeftAsEnumerable().Single().GetCode().IfNone(string.Empty).ShouldBe(DSRErrors.RequestNotFoundCode);
    }

    [Theory]
    [MemberData(nameof(Commands))]
    public async Task Command_SaveFails_ReturnsTheStoreError(Command command)
    {
        var id = Guid.NewGuid();
        ArrangeLoad(id, Aggregate(command, id), saveSucceeds: false);

        var result = await Run(command, id);

        result.LeftAsEnumerable().Single().GetCode().IfNone(string.Empty).ShouldBe("test.save");
    }

    [Theory]
    [MemberData(nameof(Commands))]
    public async Task Command_InvalidStateTransition_ReturnsInvalidRequest(Command command)
    {
        var id = Guid.NewGuid();
        // A completed request is terminal for deny, extend and expire; the other commands need an earlier status.
        var aggregate = DSRRequestAggregate.Submit(id, "subject-1", DataSubjectRight.Access, Now);
        aggregate.Verify("admin", Now);
        aggregate.StartProcessing("admin", Now);
        aggregate.Complete(Now);
        aggregate.ClearUncommittedEvents();
        ArrangeLoad(id, aggregate);

        var result = await Run(command, id);

        result.LeftAsEnumerable().Single().GetCode().IfNone(string.Empty).ShouldBe(DSRErrors.InvalidRequestCode);
    }

    [Theory]
    [MemberData(nameof(Commands))]
    public async Task Command_RepositoryThrows_ReturnsServiceError(Command command)
    {
        var id = Guid.NewGuid();
        _repository.LoadAsync(id, Arg.Any<CancellationToken>()).Throws(new InvalidCastException("boom"));

        var result = await Run(command, id);

        result.LeftAsEnumerable().Single().GetCode().IfNone(string.Empty).ShouldBe(DSRErrors.ServiceErrorCode);
    }

    // ---- Queries ----

    private static DSRRequestReadModel ReadModel(DSRRequestStatus status, DateTimeOffset deadline) => new()
    {
        Id = Guid.NewGuid(),
        SubjectId = "subject-1",
        RightType = DataSubjectRight.Access,
        Status = status,
        ReceivedAtUtc = Now.AddDays(-40),
        DeadlineAtUtc = deadline
    };

    private void ArrangeQuery(IReadOnlyList<DSRRequestReadModel> result) =>
        _readModels.QueryAsync(
                Arg.Any<Func<IQueryable<DSRRequestReadModel>, IQueryable<DSRRequestReadModel>>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, IReadOnlyList<DSRRequestReadModel>>(result)));

    private void ArrangeQueryFailure() =>
        _readModels.QueryAsync(
                Arg.Any<Func<IQueryable<DSRRequestReadModel>, IQueryable<DSRRequestReadModel>>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Left<EncinaError, IReadOnlyList<DSRRequestReadModel>>(EncinaErrors.Create("test.query", "query failed"))));

    private void ArrangeQueryThrows() =>
        _readModels.QueryAsync(
                Arg.Any<Func<IQueryable<DSRRequestReadModel>, IQueryable<DSRRequestReadModel>>>(),
                Arg.Any<CancellationToken>())
            .Throws(new InvalidCastException("boom"));

    [Fact]
    public async Task GetRequestsBySubjectAsync_ReturnsTheQueryResult()
    {
        ArrangeQuery([ReadModel(DSRRequestStatus.Received, Now.AddDays(1))]);

        var result = await _sut.GetRequestsBySubjectAsync("subject-1");

        result.RightAsEnumerable().Single().Count.ShouldBe(1);
    }

    [Fact]
    public async Task GetPendingRequestsAsync_ReturnsTheQueryResult()
    {
        ArrangeQuery([ReadModel(DSRRequestStatus.InProgress, Now.AddDays(1))]);

        var result = await _sut.GetPendingRequestsAsync();

        result.RightAsEnumerable().Single().Count.ShouldBe(1);
    }

    [Fact]
    public async Task GetOverdueRequestsAsync_KeepsOnlyRequestsPastTheirDeadline()
    {
        var overdue = ReadModel(DSRRequestStatus.InProgress, Now.AddDays(-1));
        var onTime = ReadModel(DSRRequestStatus.InProgress, Now.AddDays(5));
        ArrangeQuery([overdue, onTime]);

        var result = await _sut.GetOverdueRequestsAsync();

        result.RightAsEnumerable().Single().ShouldBe([overdue]);
    }

    [Fact]
    public async Task GetOverdueRequestsAsync_QueryFails_ReturnsTheError()
    {
        ArrangeQueryFailure();

        var result = await _sut.GetOverdueRequestsAsync();

        result.LeftAsEnumerable().Single().GetCode().IfNone(string.Empty).ShouldBe("test.query");
    }

    [Fact]
    public async Task HasActiveRestrictionAsync_QueryFails_ReturnsTheError()
    {
        _cache.GetAsync<bool?>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<bool?>(null));
        ArrangeQueryFailure();

        var result = await _sut.HasActiveRestrictionAsync("subject-1");

        result.LeftAsEnumerable().Single().GetCode().IfNone(string.Empty).ShouldBe("test.query");
    }

    public enum Query
    {
        BySubject,
        Pending,
        Overdue,
        ActiveRestriction,
        ById
    }

    public static TheoryData<Query> Queries => new(Enum.GetValues<Query>());

    [Theory]
    [MemberData(nameof(Queries))]
    public async Task Query_StoreThrows_ReturnsServiceError(Query query)
    {
        ArrangeQueryThrows();
        _cache.GetAsync<bool?>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<bool?>(null));
        _readModels.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Throws(new InvalidCastException("boom"));

        var error = query switch
        {
            Query.BySubject => (await _sut.GetRequestsBySubjectAsync("subject-1")).LeftAsEnumerable().Single(),
            Query.Pending => (await _sut.GetPendingRequestsAsync()).LeftAsEnumerable().Single(),
            Query.Overdue => (await _sut.GetOverdueRequestsAsync()).LeftAsEnumerable().Single(),
            Query.ActiveRestriction => (await _sut.HasActiveRestrictionAsync("subject-1")).LeftAsEnumerable().Single(),
            _ => (await _sut.GetRequestAsync(Guid.NewGuid())).LeftAsEnumerable().Single()
        };

        error.GetCode().IfNone(string.Empty).ShouldBe(DSRErrors.ServiceErrorCode);
    }
}
#pragma warning restore CA2012
