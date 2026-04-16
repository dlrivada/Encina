#pragma warning disable CA2012 // Use ValueTasks correctly — NSubstitute requires throw-returning lambda for resilience tests

using System.Linq.Expressions;
using Encina.DomainModeling;
using Encina.Security.Audit;
using Shouldly;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace Encina.UnitTests.Security.Audit;

/// <summary>
/// Unit tests for <see cref="AuditedRepository{TEntity, TId}"/> covering all
/// read, metadata, write, and sampling scenarios. These tests complement the
/// contract tests by exercising decorator-specific behavior (audit logging,
/// sampling, error handling).
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "Audit")]
public sealed class AuditedRepositoryTests
{
    // ── Constructor guards ──────────────────────────────────────────────

    [Fact]
    public void Constructor_NullInner_ShouldThrow()
    {
        var harness = new Harness();
        var act = () => new AuditedRepository<AuditedTestEntity, Guid>(
            null!,
            harness.Store,
            harness.RequestContext,
            harness.AuditContext,
            harness.Options,
            harness.TimeProvider,
            NullLogger<AuditedRepository<AuditedTestEntity, Guid>>.Instance);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("inner");
    }

    [Fact]
    public void Constructor_NullStore_ShouldThrow()
    {
        var harness = new Harness();
        var act = () => new AuditedRepository<AuditedTestEntity, Guid>(
            harness.Inner,
            null!,
            harness.RequestContext,
            harness.AuditContext,
            harness.Options,
            harness.TimeProvider,
            NullLogger<AuditedRepository<AuditedTestEntity, Guid>>.Instance);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("readAuditStore");
    }

    [Fact]
    public void Constructor_NullRequestContext_ShouldThrow()
    {
        var harness = new Harness();
        var act = () => new AuditedRepository<AuditedTestEntity, Guid>(
            harness.Inner,
            harness.Store,
            null!,
            harness.AuditContext,
            harness.Options,
            harness.TimeProvider,
            NullLogger<AuditedRepository<AuditedTestEntity, Guid>>.Instance);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("requestContext");
    }

    [Fact]
    public void Constructor_NullAuditContext_ShouldThrow()
    {
        var harness = new Harness();
        var act = () => new AuditedRepository<AuditedTestEntity, Guid>(
            harness.Inner,
            harness.Store,
            harness.RequestContext,
            null!,
            harness.Options,
            harness.TimeProvider,
            NullLogger<AuditedRepository<AuditedTestEntity, Guid>>.Instance);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("readAuditContext");
    }

    [Fact]
    public void Constructor_NullOptions_ShouldThrow()
    {
        var harness = new Harness();
        var act = () => new AuditedRepository<AuditedTestEntity, Guid>(
            harness.Inner,
            harness.Store,
            harness.RequestContext,
            harness.AuditContext,
            null!,
            harness.TimeProvider,
            NullLogger<AuditedRepository<AuditedTestEntity, Guid>>.Instance);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_NullTimeProvider_ShouldThrow()
    {
        var harness = new Harness();
        var act = () => new AuditedRepository<AuditedTestEntity, Guid>(
            harness.Inner,
            harness.Store,
            harness.RequestContext,
            harness.AuditContext,
            harness.Options,
            null!,
            NullLogger<AuditedRepository<AuditedTestEntity, Guid>>.Instance);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("timeProvider");
    }

    [Fact]
    public void Constructor_NullLogger_ShouldThrow()
    {
        var harness = new Harness();
        var act = () => new AuditedRepository<AuditedTestEntity, Guid>(
            harness.Inner,
            harness.Store,
            harness.RequestContext,
            harness.AuditContext,
            harness.Options,
            harness.TimeProvider,
            null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    // ── GetByIdAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_Found_AuditsWithEntityCountOne()
    {
        var harness = new Harness(samplingRate: 1.0);
        var id = Guid.NewGuid();
        var entity = new AuditedTestEntity { Id = id };
        harness.Inner.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(Option<AuditedTestEntity>.Some(entity));

        var sut = harness.CreateRepository();

        var result = await sut.GetByIdAsync(id);

        result.IsSome.ShouldBeTrue();
        harness.LoggedEntries.Count.ShouldBe(1);
        var entry = harness.LoggedEntries[0];
        entry.EntityCount.ShouldBe(1);
        entry.EntityId.ShouldBe(id.ToString());
        entry.EntityType.ShouldBe(nameof(AuditedTestEntity));
        entry.AccessMethod.ShouldBe(ReadAccessMethod.Repository);
        entry.Metadata.ShouldContainKey("method");
        entry.Metadata["method"].ShouldBe("GetById");
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_AuditsWithEntityCountZero()
    {
        var harness = new Harness(samplingRate: 1.0);
        var id = Guid.NewGuid();
        harness.Inner.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(Option<AuditedTestEntity>.None);

        var sut = harness.CreateRepository();

        var result = await sut.GetByIdAsync(id);

        result.IsNone.ShouldBeTrue();
        harness.LoggedEntries.Count.ShouldBe(1);
        harness.LoggedEntries[0].EntityCount.ShouldBe(0);
    }

    [Fact]
    public async Task GetByIdAsync_SamplingZero_DoesNotAudit()
    {
        var harness = new Harness(samplingRate: 0.0);
        var id = Guid.NewGuid();
        harness.Inner.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(Option<AuditedTestEntity>.None);

        var sut = harness.CreateRepository();
        await sut.GetByIdAsync(id);

        harness.LoggedEntries.ShouldBeEmpty();
    }

    // ── GetAllAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_WithItems_AuditsItemCount()
    {
        var harness = new Harness(samplingRate: 1.0);
        var entities = new List<AuditedTestEntity>
        {
            new() { Id = Guid.NewGuid() },
            new() { Id = Guid.NewGuid() },
            new() { Id = Guid.NewGuid() }
        };
        harness.Inner.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(entities.AsReadOnly());

        var sut = harness.CreateRepository();
        var result = await sut.GetAllAsync();

        result.Count.ShouldBe(3);
        harness.LoggedEntries.Count.ShouldBe(1);
        harness.LoggedEntries[0].EntityCount.ShouldBe(3);
        harness.LoggedEntries[0].Metadata!["method"].ShouldBe("GetAll");
        harness.LoggedEntries[0].EntityId.ShouldBeNull();
    }

    [Fact]
    public async Task GetAllAsync_Empty_AuditsZeroCount()
    {
        var harness = new Harness(samplingRate: 1.0);
        harness.Inner.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<AuditedTestEntity>().AsReadOnly());

        var sut = harness.CreateRepository();
        var result = await sut.GetAllAsync();

        result.ShouldBeEmpty();
        harness.LoggedEntries.Count.ShouldBe(1);
        harness.LoggedEntries[0].EntityCount.ShouldBe(0);
    }

    // ── FindAsync (Specification) ───────────────────────────────────────

    [Fact]
    public async Task FindAsync_WithSpecification_AuditsResultCount()
    {
        var harness = new Harness(samplingRate: 1.0);
        var spec = new AuditTestSpec();
        var entities = new List<AuditedTestEntity> { new() { Id = Guid.NewGuid() } };
        harness.Inner.FindAsync(spec, Arg.Any<CancellationToken>())
            .Returns(entities.AsReadOnly());

        var sut = harness.CreateRepository();
        var result = await sut.FindAsync(spec);

        result.Count.ShouldBe(1);
        harness.LoggedEntries.Count.ShouldBe(1);
        harness.LoggedEntries[0].Metadata!["method"].ShouldBe("Find");
        harness.LoggedEntries[0].EntityCount.ShouldBe(1);
    }

    [Fact]
    public async Task FindAsync_WithSpecification_Empty_StillAudits()
    {
        var harness = new Harness(samplingRate: 1.0);
        var spec = new AuditTestSpec();
        harness.Inner.FindAsync(spec, Arg.Any<CancellationToken>())
            .Returns(new List<AuditedTestEntity>().AsReadOnly());

        var sut = harness.CreateRepository();
        var result = await sut.FindAsync(spec);

        result.ShouldBeEmpty();
        harness.LoggedEntries.Count.ShouldBe(1);
        harness.LoggedEntries[0].EntityCount.ShouldBe(0);
    }

    // ── FindOneAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task FindOneAsync_Found_AuditsOne()
    {
        var harness = new Harness(samplingRate: 1.0);
        var spec = new AuditTestSpec();
        var entity = new AuditedTestEntity { Id = Guid.NewGuid() };
        harness.Inner.FindOneAsync(spec, Arg.Any<CancellationToken>())
            .Returns(Option<AuditedTestEntity>.Some(entity));

        var sut = harness.CreateRepository();
        var result = await sut.FindOneAsync(spec);

        result.IsSome.ShouldBeTrue();
        harness.LoggedEntries.Count.ShouldBe(1);
        harness.LoggedEntries[0].EntityCount.ShouldBe(1);
        harness.LoggedEntries[0].Metadata!["method"].ShouldBe("FindOne");
    }

    [Fact]
    public async Task FindOneAsync_NotFound_AuditsZero()
    {
        var harness = new Harness(samplingRate: 1.0);
        var spec = new AuditTestSpec();
        harness.Inner.FindOneAsync(spec, Arg.Any<CancellationToken>())
            .Returns(Option<AuditedTestEntity>.None);

        var sut = harness.CreateRepository();
        var result = await sut.FindOneAsync(spec);

        result.IsNone.ShouldBeTrue();
        harness.LoggedEntries.Count.ShouldBe(1);
        harness.LoggedEntries[0].EntityCount.ShouldBe(0);
    }

    // ── FindAsync (predicate) ───────────────────────────────────────────

    [Fact]
    public async Task FindAsync_WithPredicate_AuditsWithFindByPredicate()
    {
        var harness = new Harness(samplingRate: 1.0);
        Expression<Func<AuditedTestEntity, bool>> predicate = e => e.Id != Guid.Empty;
        var entities = new List<AuditedTestEntity>
        {
            new() { Id = Guid.NewGuid() },
            new() { Id = Guid.NewGuid() }
        };
        harness.Inner.FindAsync(predicate, Arg.Any<CancellationToken>())
            .Returns(entities.AsReadOnly());

        var sut = harness.CreateRepository();
        var result = await sut.FindAsync(predicate);

        result.Count.ShouldBe(2);
        harness.LoggedEntries.Count.ShouldBe(1);
        harness.LoggedEntries[0].Metadata!["method"].ShouldBe("FindByPredicate");
        harness.LoggedEntries[0].EntityCount.ShouldBe(2);
    }

    [Fact]
    public async Task FindAsync_WithPredicate_Empty_StillAudits()
    {
        var harness = new Harness(samplingRate: 1.0);
        Expression<Func<AuditedTestEntity, bool>> predicate = e => e.Id != Guid.Empty;
        harness.Inner.FindAsync(predicate, Arg.Any<CancellationToken>())
            .Returns(new List<AuditedTestEntity>().AsReadOnly());

        var sut = harness.CreateRepository();
        await sut.FindAsync(predicate);

        harness.LoggedEntries.Count.ShouldBe(1);
        harness.LoggedEntries[0].EntityCount.ShouldBe(0);
    }

    // ── GetPagedAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetPagedAsync_PlainOverload_AuditsWithGetPaged()
    {
        var harness = new Harness(samplingRate: 1.0);
        var paged = new global::Encina.DomainModeling.PagedResult<AuditedTestEntity>(
            new List<AuditedTestEntity> { new() { Id = Guid.NewGuid() } }.AsReadOnly(),
            PageNumber: 1,
            PageSize: 10,
            TotalCount: 1);
        harness.Inner.GetPagedAsync(1, 10, Arg.Any<CancellationToken>()).Returns(paged);

        var sut = harness.CreateRepository();
        var result = await sut.GetPagedAsync(1, 10);

        result.Items.Count.ShouldBe(1);
        harness.LoggedEntries.Count.ShouldBe(1);
        harness.LoggedEntries[0].Metadata!["method"].ShouldBe("GetPaged");
        harness.LoggedEntries[0].EntityCount.ShouldBe(1);
    }

    [Fact]
    public async Task GetPagedAsync_WithSpecification_AuditsWithGetPagedWithSpec()
    {
        var harness = new Harness(samplingRate: 1.0);
        var spec = new AuditTestSpec();
        var paged = new global::Encina.DomainModeling.PagedResult<AuditedTestEntity>(
            new List<AuditedTestEntity>
            {
                new() { Id = Guid.NewGuid() },
                new() { Id = Guid.NewGuid() }
            }.AsReadOnly(),
            PageNumber: 2,
            PageSize: 25,
            TotalCount: 27);
        harness.Inner.GetPagedAsync(spec, 2, 25, Arg.Any<CancellationToken>()).Returns(paged);

        var sut = harness.CreateRepository();
        var result = await sut.GetPagedAsync(spec, 2, 25);

        result.TotalCount.ShouldBe(27);
        harness.LoggedEntries.Count.ShouldBe(1);
        harness.LoggedEntries[0].Metadata!["method"].ShouldBe("GetPagedWithSpec");
        harness.LoggedEntries[0].EntityCount.ShouldBe(2);
    }

    // ── Metadata operations (NOT audited) ───────────────────────────────

    [Fact]
    public async Task AnyAsync_Specification_DelegatesWithoutAuditing()
    {
        var harness = new Harness(samplingRate: 1.0);
        var spec = new AuditTestSpec();
        harness.Inner.AnyAsync(spec, Arg.Any<CancellationToken>()).Returns(true);

        var sut = harness.CreateRepository();
        var result = await sut.AnyAsync(spec);

        result.ShouldBeTrue();
        harness.LoggedEntries.ShouldBeEmpty();
        await harness.Inner.Received(1).AnyAsync(spec, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AnyAsync_Predicate_DelegatesWithoutAuditing()
    {
        var harness = new Harness(samplingRate: 1.0);
        Expression<Func<AuditedTestEntity, bool>> predicate = e => true;
        harness.Inner.AnyAsync(predicate, Arg.Any<CancellationToken>()).Returns(true);

        var sut = harness.CreateRepository();
        var result = await sut.AnyAsync(predicate);

        result.ShouldBeTrue();
        harness.LoggedEntries.ShouldBeEmpty();
    }

    [Fact]
    public async Task CountAsync_Specification_DelegatesWithoutAuditing()
    {
        var harness = new Harness(samplingRate: 1.0);
        var spec = new AuditTestSpec();
        harness.Inner.CountAsync(spec, Arg.Any<CancellationToken>()).Returns(11);

        var sut = harness.CreateRepository();
        var result = await sut.CountAsync(spec);

        result.ShouldBe(11);
        harness.LoggedEntries.ShouldBeEmpty();
    }

    [Fact]
    public async Task CountAsync_NoArgs_DelegatesWithoutAuditing()
    {
        var harness = new Harness(samplingRate: 1.0);
        harness.Inner.CountAsync(Arg.Any<CancellationToken>()).Returns(99);

        var sut = harness.CreateRepository();
        var result = await sut.CountAsync();

        result.ShouldBe(99);
        harness.LoggedEntries.ShouldBeEmpty();
    }

    // ── Write operations (delegated, no audit) ──────────────────────────

    [Fact]
    public async Task AddAsync_DelegatesWithoutAuditing()
    {
        var harness = new Harness(samplingRate: 1.0);
        var entity = new AuditedTestEntity { Id = Guid.NewGuid() };

        var sut = harness.CreateRepository();
        await sut.AddAsync(entity);

        await harness.Inner.Received(1).AddAsync(entity, Arg.Any<CancellationToken>());
        harness.LoggedEntries.ShouldBeEmpty();
    }

    [Fact]
    public async Task AddRangeAsync_DelegatesWithoutAuditing()
    {
        var harness = new Harness(samplingRate: 1.0);
        var entities = new List<AuditedTestEntity>
        {
            new() { Id = Guid.NewGuid() },
            new() { Id = Guid.NewGuid() }
        };

        var sut = harness.CreateRepository();
        await sut.AddRangeAsync(entities);

        await harness.Inner.Received(1).AddRangeAsync(entities, Arg.Any<CancellationToken>());
        harness.LoggedEntries.ShouldBeEmpty();
    }

    [Fact]
    public void Update_DelegatesWithoutAuditing()
    {
        var harness = new Harness(samplingRate: 1.0);
        var entity = new AuditedTestEntity { Id = Guid.NewGuid() };

        var sut = harness.CreateRepository();
        sut.Update(entity);

        harness.Inner.Received(1).Update(entity);
        harness.LoggedEntries.ShouldBeEmpty();
    }

    [Fact]
    public void UpdateRange_DelegatesWithoutAuditing()
    {
        var harness = new Harness(samplingRate: 1.0);
        var entities = new List<AuditedTestEntity> { new() { Id = Guid.NewGuid() } };

        var sut = harness.CreateRepository();
        sut.UpdateRange(entities);

        harness.Inner.Received(1).UpdateRange(entities);
        harness.LoggedEntries.ShouldBeEmpty();
    }

    [Fact]
    public void Remove_DelegatesWithoutAuditing()
    {
        var harness = new Harness(samplingRate: 1.0);
        var entity = new AuditedTestEntity { Id = Guid.NewGuid() };

        var sut = harness.CreateRepository();
        sut.Remove(entity);

        harness.Inner.Received(1).Remove(entity);
        harness.LoggedEntries.ShouldBeEmpty();
    }

    [Fact]
    public void RemoveRange_DelegatesWithoutAuditing()
    {
        var harness = new Harness(samplingRate: 1.0);
        var entities = new List<AuditedTestEntity> { new() { Id = Guid.NewGuid() } };

        var sut = harness.CreateRepository();
        sut.RemoveRange(entities);

        harness.Inner.Received(1).RemoveRange(entities);
        harness.LoggedEntries.ShouldBeEmpty();
    }

    [Fact]
    public async Task RemoveByIdAsync_DelegatesWithoutAuditing()
    {
        var harness = new Harness(samplingRate: 1.0);
        var id = Guid.NewGuid();
        harness.Inner.RemoveByIdAsync(id, Arg.Any<CancellationToken>()).Returns(true);

        var sut = harness.CreateRepository();
        var result = await sut.RemoveByIdAsync(id);

        result.ShouldBeTrue();
        harness.LoggedEntries.ShouldBeEmpty();
    }

    // ── ShouldAudit sampling / exclusion logic ──────────────────────────

    [Fact]
    public async Task ExcludeSystemAccess_NoUserId_DoesNotAudit()
    {
        var harness = new Harness(samplingRate: 1.0);
        harness.Options.ExcludeSystemAccess = true;
        harness.RequestContext.UserId.Returns((string?)null);

        harness.Inner.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<AuditedTestEntity>().AsReadOnly());

        var sut = harness.CreateRepository();
        await sut.GetAllAsync();

        harness.LoggedEntries.ShouldBeEmpty();
    }

    [Fact]
    public async Task ExcludeSystemAccess_WithUserId_StillAudits()
    {
        var harness = new Harness(samplingRate: 1.0);
        harness.Options.ExcludeSystemAccess = true;
        harness.RequestContext.UserId.Returns("user-42");

        harness.Inner.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<AuditedTestEntity>().AsReadOnly());

        var sut = harness.CreateRepository();
        await sut.GetAllAsync();

        harness.LoggedEntries.Count.ShouldBe(1);
        harness.LoggedEntries[0].UserId.ShouldBe("user-42");
    }

    [Fact]
    public async Task UnregisteredEntityType_NoSamplingRate_DoesNotAudit()
    {
        var harness = new Harness(samplingRate: null); // no registration
        harness.Inner.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<AuditedTestEntity>().AsReadOnly());

        var sut = harness.CreateRepository();
        await sut.GetAllAsync();

        harness.LoggedEntries.ShouldBeEmpty();
    }

    // ── Audit failure resilience ────────────────────────────────────────

    [Fact]
    public async Task AuditStoreThrows_ReadOperationStillReturnsResult()
    {
        var harness = new Harness(samplingRate: 1.0);
        harness.Store
            .LogReadAsync(Arg.Any<ReadAuditEntry>(), Arg.Any<CancellationToken>())
            .Returns<ValueTask<Either<EncinaError, Unit>>>(_ => throw new InvalidOperationException("audit backend down"));

        var entity = new AuditedTestEntity { Id = Guid.NewGuid() };
        harness.Inner.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<AuditedTestEntity> { entity }.AsReadOnly());

        var sut = harness.CreateRepository();

        var items = await sut.GetAllAsync();
        items.Count.ShouldBe(1);
    }

    // ── RequirePurpose branch ───────────────────────────────────────────

    [Fact]
    public async Task RequirePurpose_NoPurpose_LogsWarningButStillAudits()
    {
        var harness = new Harness(samplingRate: 1.0);
        harness.Options.RequirePurpose = true;
        harness.AuditContext.Purpose.Returns((string?)null);

        harness.Inner.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<AuditedTestEntity>().AsReadOnly());

        var sut = harness.CreateRepository();
        await sut.GetAllAsync();

        harness.LoggedEntries.Count.ShouldBe(1);
        harness.LoggedEntries[0].Purpose.ShouldBeNull();
    }

    [Fact]
    public async Task RequirePurpose_WithPurpose_IncludesPurposeInEntry()
    {
        var harness = new Harness(samplingRate: 1.0);
        harness.Options.RequirePurpose = true;
        harness.AuditContext.Purpose.Returns("Compliance review");

        harness.Inner.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<AuditedTestEntity>().AsReadOnly());

        var sut = harness.CreateRepository();
        await sut.GetAllAsync();

        harness.LoggedEntries.Count.ShouldBe(1);
        harness.LoggedEntries[0].Purpose.ShouldBe("Compliance review");
    }

    // ── Harness & helpers ───────────────────────────────────────────────

    private sealed class Harness
    {
        public IRepository<AuditedTestEntity, Guid> Inner { get; } =
            Substitute.For<IRepository<AuditedTestEntity, Guid>>();

        public IReadAuditStore Store { get; } = Substitute.For<IReadAuditStore>();

        public IRequestContext RequestContext { get; } = Substitute.For<IRequestContext>();

        public IReadAuditContext AuditContext { get; } = Substitute.For<IReadAuditContext>();

        public ReadAuditOptions Options { get; } = new();

        public FakeTimeProvider TimeProvider { get; } = new(DateTimeOffset.UtcNow);

        public List<ReadAuditEntry> LoggedEntries { get; } = [];

        public Harness(double? samplingRate = null)
        {
            RequestContext.UserId.Returns("test-user");
            RequestContext.TenantId.Returns("tenant-1");
            RequestContext.CorrelationId.Returns("corr-id");

            if (samplingRate is not null)
            {
                Options.AuditReadsFor<AuditedTestEntity>(samplingRate.Value);
            }

            Store
                .LogReadAsync(Arg.Any<ReadAuditEntry>(), Arg.Any<CancellationToken>())
                .Returns(callInfo =>
                {
                    LoggedEntries.Add(callInfo.Arg<ReadAuditEntry>());
                    return ValueTask.FromResult(Either<EncinaError, Unit>.Right(Unit.Default));
                });
        }

        public AuditedRepository<AuditedTestEntity, Guid> CreateRepository() =>
            new(
                Inner,
                Store,
                RequestContext,
                AuditContext,
                Options,
                TimeProvider,
                NullLogger<AuditedRepository<AuditedTestEntity, Guid>>.Instance);
    }

    private sealed class AuditTestSpec : Specification<AuditedTestEntity>
    {
        public override Expression<Func<AuditedTestEntity, bool>> ToExpression() => e => true;
    }

    public sealed class AuditedTestEntity : IEntity<Guid>, IReadAuditable
    {
        public Guid Id { get; set; }
    }
}
