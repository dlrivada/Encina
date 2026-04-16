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
/// Unit tests for <see cref="AuditedReadOnlyRepository{TEntity, TId}"/> covering
/// all read operations, sampling, and failure resilience. These tests complement
/// the contract tests by verifying decorator-specific behavior (audit entry
/// content, sampling, exception safety).
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "Audit")]
public sealed class AuditedReadOnlyRepositoryTests
{
    // ── Constructor guards ──────────────────────────────────────────────

    [Fact]
    public void Constructor_NullInner_ShouldThrow()
    {
        var h = new Harness();
        var act = () => new AuditedReadOnlyRepository<ReadOnlyAuditedTestEntity, Guid>(
            null!,
            h.Store,
            h.RequestContext,
            h.AuditContext,
            h.Options,
            h.TimeProvider,
            NullLogger<AuditedReadOnlyRepository<ReadOnlyAuditedTestEntity, Guid>>.Instance);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("inner");
    }

    [Fact]
    public void Constructor_NullStore_ShouldThrow()
    {
        var h = new Harness();
        var act = () => new AuditedReadOnlyRepository<ReadOnlyAuditedTestEntity, Guid>(
            h.Inner,
            null!,
            h.RequestContext,
            h.AuditContext,
            h.Options,
            h.TimeProvider,
            NullLogger<AuditedReadOnlyRepository<ReadOnlyAuditedTestEntity, Guid>>.Instance);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("readAuditStore");
    }

    [Fact]
    public void Constructor_NullRequestContext_ShouldThrow()
    {
        var h = new Harness();
        var act = () => new AuditedReadOnlyRepository<ReadOnlyAuditedTestEntity, Guid>(
            h.Inner,
            h.Store,
            null!,
            h.AuditContext,
            h.Options,
            h.TimeProvider,
            NullLogger<AuditedReadOnlyRepository<ReadOnlyAuditedTestEntity, Guid>>.Instance);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("requestContext");
    }

    [Fact]
    public void Constructor_NullAuditContext_ShouldThrow()
    {
        var h = new Harness();
        var act = () => new AuditedReadOnlyRepository<ReadOnlyAuditedTestEntity, Guid>(
            h.Inner,
            h.Store,
            h.RequestContext,
            null!,
            h.Options,
            h.TimeProvider,
            NullLogger<AuditedReadOnlyRepository<ReadOnlyAuditedTestEntity, Guid>>.Instance);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("readAuditContext");
    }

    [Fact]
    public void Constructor_NullOptions_ShouldThrow()
    {
        var h = new Harness();
        var act = () => new AuditedReadOnlyRepository<ReadOnlyAuditedTestEntity, Guid>(
            h.Inner,
            h.Store,
            h.RequestContext,
            h.AuditContext,
            null!,
            h.TimeProvider,
            NullLogger<AuditedReadOnlyRepository<ReadOnlyAuditedTestEntity, Guid>>.Instance);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_NullTimeProvider_ShouldThrow()
    {
        var h = new Harness();
        var act = () => new AuditedReadOnlyRepository<ReadOnlyAuditedTestEntity, Guid>(
            h.Inner,
            h.Store,
            h.RequestContext,
            h.AuditContext,
            h.Options,
            null!,
            NullLogger<AuditedReadOnlyRepository<ReadOnlyAuditedTestEntity, Guid>>.Instance);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("timeProvider");
    }

    [Fact]
    public void Constructor_NullLogger_ShouldThrow()
    {
        var h = new Harness();
        var act = () => new AuditedReadOnlyRepository<ReadOnlyAuditedTestEntity, Guid>(
            h.Inner,
            h.Store,
            h.RequestContext,
            h.AuditContext,
            h.Options,
            h.TimeProvider,
            null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    // ── GetByIdAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_Found_AuditsWithEntityCountOne()
    {
        var h = new Harness(samplingRate: 1.0);
        var id = Guid.NewGuid();
        h.Inner.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(Option<ReadOnlyAuditedTestEntity>.Some(new ReadOnlyAuditedTestEntity { Id = id }));

        var sut = h.Create();
        var result = await sut.GetByIdAsync(id);

        result.IsSome.ShouldBeTrue();
        h.LoggedEntries.Count.ShouldBe(1);
        var entry = h.LoggedEntries[0];
        entry.EntityCount.ShouldBe(1);
        entry.EntityId.ShouldBe(id.ToString());
        entry.EntityType.ShouldBe(nameof(ReadOnlyAuditedTestEntity));
        entry.Metadata.ShouldContainKey("method").WhoseValue.ShouldBe("GetById");
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_AuditsWithEntityCountZero()
    {
        var h = new Harness(samplingRate: 1.0);
        var id = Guid.NewGuid();
        h.Inner.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(Option<ReadOnlyAuditedTestEntity>.None);

        var sut = h.Create();
        var result = await sut.GetByIdAsync(id);

        result.IsNone.ShouldBeTrue();
        h.LoggedEntries.Count.ShouldBe(1);
        h.LoggedEntries[0].EntityCount.ShouldBe(0);
    }

    [Fact]
    public async Task GetByIdAsync_SamplingZero_DoesNotAudit()
    {
        var h = new Harness(samplingRate: 0.0);
        var id = Guid.NewGuid();
        h.Inner.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(Option<ReadOnlyAuditedTestEntity>.None);

        var sut = h.Create();
        await sut.GetByIdAsync(id);

        h.LoggedEntries.ShouldBeEmpty();
    }

    // ── GetAllAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_WithItems_AuditsCount()
    {
        var h = new Harness(samplingRate: 1.0);
        var items = new List<ReadOnlyAuditedTestEntity>
        {
            new() { Id = Guid.NewGuid() },
            new() { Id = Guid.NewGuid() }
        };
        h.Inner.GetAllAsync(Arg.Any<CancellationToken>()).Returns(items.AsReadOnly());

        var sut = h.Create();
        var result = await sut.GetAllAsync();

        result.Count.ShouldBe(2);
        h.LoggedEntries.Count.ShouldBe(1);
        h.LoggedEntries[0].EntityCount.ShouldBe(2);
        h.LoggedEntries[0].Metadata!["method"].ShouldBe("GetAll");
    }

    [Fact]
    public async Task GetAllAsync_Empty_AuditsZero()
    {
        var h = new Harness(samplingRate: 1.0);
        h.Inner.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<ReadOnlyAuditedTestEntity>().AsReadOnly());

        var sut = h.Create();
        await sut.GetAllAsync();

        h.LoggedEntries.Count.ShouldBe(1);
        h.LoggedEntries[0].EntityCount.ShouldBe(0);
    }

    // ── FindAsync (Specification) ───────────────────────────────────────

    [Fact]
    public async Task FindAsync_Specification_AuditsResultCount()
    {
        var h = new Harness(samplingRate: 1.0);
        var spec = new ReadOnlyTestSpec();
        var items = new List<ReadOnlyAuditedTestEntity> { new() { Id = Guid.NewGuid() } };
        h.Inner.FindAsync(spec, Arg.Any<CancellationToken>()).Returns(items.AsReadOnly());

        var sut = h.Create();
        var result = await sut.FindAsync(spec);

        result.Count.ShouldBe(1);
        h.LoggedEntries.Count.ShouldBe(1);
        h.LoggedEntries[0].Metadata!["method"].ShouldBe("Find");
    }

    [Fact]
    public async Task FindAsync_Specification_Empty_StillAudits()
    {
        var h = new Harness(samplingRate: 1.0);
        var spec = new ReadOnlyTestSpec();
        h.Inner.FindAsync(spec, Arg.Any<CancellationToken>())
            .Returns(new List<ReadOnlyAuditedTestEntity>().AsReadOnly());

        var sut = h.Create();
        await sut.FindAsync(spec);

        h.LoggedEntries.Count.ShouldBe(1);
        h.LoggedEntries[0].EntityCount.ShouldBe(0);
    }

    // ── FindOneAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task FindOneAsync_Found_AuditsOne()
    {
        var h = new Harness(samplingRate: 1.0);
        var spec = new ReadOnlyTestSpec();
        h.Inner.FindOneAsync(spec, Arg.Any<CancellationToken>())
            .Returns(Option<ReadOnlyAuditedTestEntity>.Some(new ReadOnlyAuditedTestEntity { Id = Guid.NewGuid() }));

        var sut = h.Create();
        var result = await sut.FindOneAsync(spec);

        result.IsSome.ShouldBeTrue();
        h.LoggedEntries.Count.ShouldBe(1);
        h.LoggedEntries[0].EntityCount.ShouldBe(1);
        h.LoggedEntries[0].Metadata!["method"].ShouldBe("FindOne");
    }

    [Fact]
    public async Task FindOneAsync_NotFound_AuditsZero()
    {
        var h = new Harness(samplingRate: 1.0);
        var spec = new ReadOnlyTestSpec();
        h.Inner.FindOneAsync(spec, Arg.Any<CancellationToken>())
            .Returns(Option<ReadOnlyAuditedTestEntity>.None);

        var sut = h.Create();
        var result = await sut.FindOneAsync(spec);

        result.IsNone.ShouldBeTrue();
        h.LoggedEntries.Count.ShouldBe(1);
        h.LoggedEntries[0].EntityCount.ShouldBe(0);
    }

    // ── FindAsync (predicate) ───────────────────────────────────────────

    [Fact]
    public async Task FindAsync_Predicate_WithItems_AuditsCount()
    {
        var h = new Harness(samplingRate: 1.0);
        Expression<Func<ReadOnlyAuditedTestEntity, bool>> predicate = e => e.Id != Guid.Empty;
        var items = new List<ReadOnlyAuditedTestEntity>
        {
            new() { Id = Guid.NewGuid() },
            new() { Id = Guid.NewGuid() },
            new() { Id = Guid.NewGuid() }
        };
        h.Inner.FindAsync(predicate, Arg.Any<CancellationToken>()).Returns(items.AsReadOnly());

        var sut = h.Create();
        var result = await sut.FindAsync(predicate);

        result.Count.ShouldBe(3);
        h.LoggedEntries.Count.ShouldBe(1);
        h.LoggedEntries[0].Metadata!["method"].ShouldBe("FindByPredicate");
        h.LoggedEntries[0].EntityCount.ShouldBe(3);
    }

    [Fact]
    public async Task FindAsync_Predicate_Empty_StillAudits()
    {
        var h = new Harness(samplingRate: 1.0);
        Expression<Func<ReadOnlyAuditedTestEntity, bool>> predicate = e => e.Id != Guid.Empty;
        h.Inner.FindAsync(predicate, Arg.Any<CancellationToken>())
            .Returns(new List<ReadOnlyAuditedTestEntity>().AsReadOnly());

        var sut = h.Create();
        await sut.FindAsync(predicate);

        h.LoggedEntries.Count.ShouldBe(1);
        h.LoggedEntries[0].EntityCount.ShouldBe(0);
    }

    // ── GetPagedAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetPagedAsync_Plain_AuditsWithGetPaged()
    {
        var h = new Harness(samplingRate: 1.0);
        var paged = new global::Encina.DomainModeling.PagedResult<ReadOnlyAuditedTestEntity>(
            new List<ReadOnlyAuditedTestEntity> { new() { Id = Guid.NewGuid() } }.AsReadOnly(),
            PageNumber: 1,
            PageSize: 20,
            TotalCount: 1);
        h.Inner.GetPagedAsync(1, 20, Arg.Any<CancellationToken>()).Returns(paged);

        var sut = h.Create();
        var result = await sut.GetPagedAsync(1, 20);

        result.Items.Count.ShouldBe(1);
        h.LoggedEntries.Count.ShouldBe(1);
        h.LoggedEntries[0].Metadata!["method"].ShouldBe("GetPaged");
        h.LoggedEntries[0].EntityCount.ShouldBe(1);
    }

    [Fact]
    public async Task GetPagedAsync_WithSpecification_AuditsWithGetPagedWithSpec()
    {
        var h = new Harness(samplingRate: 1.0);
        var spec = new ReadOnlyTestSpec();
        var paged = new global::Encina.DomainModeling.PagedResult<ReadOnlyAuditedTestEntity>(
            new List<ReadOnlyAuditedTestEntity>
            {
                new() { Id = Guid.NewGuid() },
                new() { Id = Guid.NewGuid() }
            }.AsReadOnly(),
            PageNumber: 3,
            PageSize: 15,
            TotalCount: 45);
        h.Inner.GetPagedAsync(spec, 3, 15, Arg.Any<CancellationToken>()).Returns(paged);

        var sut = h.Create();
        var result = await sut.GetPagedAsync(spec, 3, 15);

        result.TotalCount.ShouldBe(45);
        h.LoggedEntries.Count.ShouldBe(1);
        h.LoggedEntries[0].Metadata!["method"].ShouldBe("GetPagedWithSpec");
        h.LoggedEntries[0].EntityCount.ShouldBe(2);
    }

    // ── Metadata operations (not audited) ───────────────────────────────

    [Fact]
    public async Task AnyAsync_Specification_DelegatesWithoutAuditing()
    {
        var h = new Harness(samplingRate: 1.0);
        var spec = new ReadOnlyTestSpec();
        h.Inner.AnyAsync(spec, Arg.Any<CancellationToken>()).Returns(true);

        var sut = h.Create();
        var result = await sut.AnyAsync(spec);

        result.ShouldBeTrue();
        h.LoggedEntries.ShouldBeEmpty();
    }

    [Fact]
    public async Task AnyAsync_Predicate_DelegatesWithoutAuditing()
    {
        var h = new Harness(samplingRate: 1.0);
        Expression<Func<ReadOnlyAuditedTestEntity, bool>> predicate = e => true;
        h.Inner.AnyAsync(predicate, Arg.Any<CancellationToken>()).Returns(false);

        var sut = h.Create();
        var result = await sut.AnyAsync(predicate);

        result.ShouldBeFalse();
        h.LoggedEntries.ShouldBeEmpty();
    }

    [Fact]
    public async Task CountAsync_Specification_DelegatesWithoutAuditing()
    {
        var h = new Harness(samplingRate: 1.0);
        var spec = new ReadOnlyTestSpec();
        h.Inner.CountAsync(spec, Arg.Any<CancellationToken>()).Returns(5);

        var sut = h.Create();
        var result = await sut.CountAsync(spec);

        result.ShouldBe(5);
        h.LoggedEntries.ShouldBeEmpty();
    }

    [Fact]
    public async Task CountAsync_NoArgs_DelegatesWithoutAuditing()
    {
        var h = new Harness(samplingRate: 1.0);
        h.Inner.CountAsync(Arg.Any<CancellationToken>()).Returns(123);

        var sut = h.Create();
        var result = await sut.CountAsync();

        result.ShouldBe(123);
        h.LoggedEntries.ShouldBeEmpty();
    }

    // ── Sampling / exclusion logic ──────────────────────────────────────

    [Fact]
    public async Task ExcludeSystemAccess_NoUserId_DoesNotAudit()
    {
        var h = new Harness(samplingRate: 1.0);
        h.Options.ExcludeSystemAccess = true;
        h.RequestContext.UserId.Returns((string?)null);

        h.Inner.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<ReadOnlyAuditedTestEntity>().AsReadOnly());

        var sut = h.Create();
        await sut.GetAllAsync();

        h.LoggedEntries.ShouldBeEmpty();
    }

    [Fact]
    public async Task ExcludeSystemAccess_WithUserId_Audits()
    {
        var h = new Harness(samplingRate: 1.0);
        h.Options.ExcludeSystemAccess = true;
        h.RequestContext.UserId.Returns("user-99");

        h.Inner.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<ReadOnlyAuditedTestEntity>().AsReadOnly());

        var sut = h.Create();
        await sut.GetAllAsync();

        h.LoggedEntries.Count.ShouldBe(1);
        h.LoggedEntries[0].UserId.ShouldBe("user-99");
    }

    [Fact]
    public async Task UnregisteredEntity_DoesNotAudit()
    {
        var h = new Harness(samplingRate: null);
        h.Inner.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<ReadOnlyAuditedTestEntity>().AsReadOnly());

        var sut = h.Create();
        await sut.GetAllAsync();

        h.LoggedEntries.ShouldBeEmpty();
    }

    // ── Audit failure resilience ────────────────────────────────────────

    [Fact]
    public async Task AuditStoreThrows_ReadStillSucceeds()
    {
        var h = new Harness(samplingRate: 1.0);
        h.Store
            .LogReadAsync(Arg.Any<ReadAuditEntry>(), Arg.Any<CancellationToken>())
            .Returns<ValueTask<Either<EncinaError, Unit>>>(_ => throw new InvalidOperationException("boom"));

        h.Inner.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<ReadOnlyAuditedTestEntity> { new() { Id = Guid.NewGuid() } }.AsReadOnly());

        var sut = h.Create();

        var act = async () => await sut.GetAllAsync();
        var result = await Should.NotThrowAsync(act);
        result.Which.Count.ShouldBe(1);
    }

    // ── Purpose handling ────────────────────────────────────────────────

    [Fact]
    public async Task Purpose_WhenDeclared_IsCopiedIntoAuditEntry()
    {
        var h = new Harness(samplingRate: 1.0);
        h.AuditContext.Purpose.Returns("Patient care review");

        h.Inner.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<ReadOnlyAuditedTestEntity>().AsReadOnly());

        var sut = h.Create();
        await sut.GetAllAsync();

        h.LoggedEntries.Count.ShouldBe(1);
        h.LoggedEntries[0].Purpose.ShouldBe("Patient care review");
    }

    [Fact]
    public async Task RequirePurpose_WithoutPurpose_StillAudits()
    {
        var h = new Harness(samplingRate: 1.0);
        h.Options.RequirePurpose = true;
        h.AuditContext.Purpose.Returns((string?)null);

        h.Inner.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<ReadOnlyAuditedTestEntity>().AsReadOnly());

        var sut = h.Create();
        await sut.GetAllAsync();

        h.LoggedEntries.Count.ShouldBe(1);
        h.LoggedEntries[0].Purpose.ShouldBeNull();
    }

    [Fact]
    public async Task AuditEntry_IncludesRequestContextFields()
    {
        var h = new Harness(samplingRate: 1.0);
        h.RequestContext.UserId.Returns("alice");
        h.RequestContext.TenantId.Returns("acme");
        h.RequestContext.CorrelationId.Returns("trace-abc");

        h.Inner.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<ReadOnlyAuditedTestEntity>().AsReadOnly());

        var sut = h.Create();
        await sut.GetAllAsync();

        h.LoggedEntries.Count.ShouldBe(1);
        var entry = h.LoggedEntries[0];
        entry.UserId.ShouldBe("alice");
        entry.TenantId.ShouldBe("acme");
        entry.CorrelationId.ShouldBe("trace-abc");
        entry.AccessMethod.ShouldBe(ReadAccessMethod.Repository);
        entry.AccessedAtUtc.ShouldBe(h.TimeProvider.GetUtcNow());
    }

    // ── Harness & helpers ───────────────────────────────────────────────

    private sealed class Harness
    {
        public IReadOnlyRepository<ReadOnlyAuditedTestEntity, Guid> Inner { get; } =
            Substitute.For<IReadOnlyRepository<ReadOnlyAuditedTestEntity, Guid>>();

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
                Options.AuditReadsFor<ReadOnlyAuditedTestEntity>(samplingRate.Value);
            }

            Store
                .LogReadAsync(Arg.Any<ReadAuditEntry>(), Arg.Any<CancellationToken>())
                .Returns(callInfo =>
                {
                    LoggedEntries.Add(callInfo.Arg<ReadAuditEntry>());
                    return ValueTask.FromResult(Either<EncinaError, Unit>.Right(Unit.Default));
                });
        }

        public AuditedReadOnlyRepository<ReadOnlyAuditedTestEntity, Guid> Create() =>
            new(
                Inner,
                Store,
                RequestContext,
                AuditContext,
                Options,
                TimeProvider,
                NullLogger<AuditedReadOnlyRepository<ReadOnlyAuditedTestEntity, Guid>>.Instance);
    }

    private sealed class ReadOnlyTestSpec : Specification<ReadOnlyAuditedTestEntity>
    {
        public override Expression<Func<ReadOnlyAuditedTestEntity, bool>> ToExpression() => e => true;
    }

    public sealed class ReadOnlyAuditedTestEntity : IEntity<Guid>, IReadAuditable
    {
        public Guid Id { get; set; }
    }
}
