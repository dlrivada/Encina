using Encina.Caching;
using Encina.Compliance.DataSubjectRights;
using Encina.Compliance.DataSubjectRights.Abstractions;
using Encina.Compliance.DataSubjectRights.Aggregates;
using Encina.Compliance.DataSubjectRights.Projections;
using Encina.Compliance.DataSubjectRights.Services;
using Encina.Compliance.GDPR;
using Encina.Marten;
using Encina.Marten.Projections;
using Encina.Tenancy;

using FsCheck;
using FsCheck.Xunit;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

using static LanguageExt.Prelude;

namespace Encina.PropertyTests.Compliance.DataSubjectRights;

/// <summary>
/// Property-based proof of the tenant isolation invariant of <see cref="DefaultDSRService"/>
/// (#1963): for any two distinct tenants, nothing recorded by tenant A is visible to, cacheable
/// for, or mutable by tenant B, whatever the tenant and subject identifiers are.
/// </summary>
public class DSRTenantIsolationPropertyTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);

    /// <summary>Two services over one shared store and one shared cache, one per tenant.</summary>
    private sealed class World
    {
        private readonly List<DSRRequestReadModel> _rows = [];
        private readonly Dictionary<Guid, DSRRequestAggregate> _aggregates = [];
        private readonly Dictionary<string, object?> _cache = new(StringComparer.Ordinal);

        public World(string tenantA, string tenantB)
        {
            A = CreateService(tenantA);
            B = CreateService(tenantB);
        }

        public DefaultDSRService A { get; }

        public DefaultDSRService B { get; }

        public int SavedAggregates { get; private set; }

        public IReadOnlyCollection<string> CacheKeys => _cache.Keys;

        /// <summary>Seeds a restriction request of tenant A (aggregate and read model).</summary>
        public Guid SeedRestriction(string tenantId, string subjectId)
        {
            var aggregate = DSRRequestAggregate.Submit(
                Guid.NewGuid(), subjectId, DataSubjectRight.Restriction, Now.AddDays(-40), tenantId: tenantId);
            _aggregates[aggregate.Id] = aggregate;
            _rows.Add(new DSRRequestReadModel
            {
                Id = aggregate.Id,
                SubjectId = subjectId,
                RightType = DataSubjectRight.Restriction,
                Status = DSRRequestStatus.Received,
                TenantId = tenantId,
                ReceivedAtUtc = Now.AddDays(-40),
                DeadlineAtUtc = Now.AddDays(-10)
            });
            return aggregate.Id;
        }

        private DefaultDSRService CreateService(string tenantId)
        {
            var repository = Substitute.For<IAggregateRepository<DSRRequestAggregate>>();
            repository.LoadAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                .Returns(call => Task.FromResult(
                    _aggregates.TryGetValue(call.Arg<Guid>(), out var a)
                        ? Right<EncinaError, DSRRequestAggregate>(a)
                        : Left<EncinaError, DSRRequestAggregate>(EncinaErrors.Create("test.not_found", "not found"))));
            repository.SaveAsync(Arg.Any<DSRRequestAggregate>(), Arg.Any<CancellationToken>())
                .Returns(_ =>
                {
                    SavedAggregates++;
                    return Task.FromResult(Right<EncinaError, Unit>(unit));
                });

            var readModels = Substitute.For<IReadModelRepository<DSRRequestReadModel>>();
            readModels.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                .Returns(call => Task.FromResult(
                    _rows.FirstOrDefault(r => r.Id == call.Arg<Guid>()) is { } row
                        ? Right<EncinaError, DSRRequestReadModel>(row)
                        : Left<EncinaError, DSRRequestReadModel>(EncinaErrors.Create("test.not_found", "not found"))));
            readModels.QueryAsync(
                    Arg.Any<Func<IQueryable<DSRRequestReadModel>, IQueryable<DSRRequestReadModel>>>(),
                    Arg.Any<CancellationToken>())
                .Returns(call =>
                {
                    var query = call.Arg<Func<IQueryable<DSRRequestReadModel>, IQueryable<DSRRequestReadModel>>>();
                    IReadOnlyList<DSRRequestReadModel> result = query(_rows.AsQueryable()).ToList();
                    return Task.FromResult(Right<EncinaError, IReadOnlyList<DSRRequestReadModel>>(result));
                });

            var cache = Substitute.For<ICacheProvider>();
            cache.GetAsync<DSRRequestReadModel>(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(call => Task.FromResult(_cache.TryGetValue(call.Arg<string>(), out var v) ? v as DSRRequestReadModel : null));
            cache.GetAsync<bool?>(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(call => Task.FromResult(_cache.TryGetValue(call.Arg<string>(), out var v) ? v as bool? : null));
            cache.SetAsync(Arg.Any<string>(), Arg.Any<DSRRequestReadModel>(), Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>())
                .Returns(call =>
                {
                    _cache[call.Arg<string>()] = call.Arg<DSRRequestReadModel>();
                    return Task.CompletedTask;
                });
            cache.SetAsync(Arg.Any<string>(), Arg.Any<bool?>(), Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>())
                .Returns(call =>
                {
                    _cache[call.Arg<string>()] = call.Arg<bool?>();
                    return Task.CompletedTask;
                });

            var accessor = Substitute.For<IRequestContextAccessor>();
            accessor.RequestContext.Returns(RequestContext.CreateForTest(tenantId: tenantId));

            return new DefaultDSRService(
                repository,
                readModels,
                Substitute.For<IPersonalDataLocator>(),
                Substitute.For<IDataErasureExecutor>(),
                Substitute.For<IDataPortabilityExporter>(),
                Substitute.For<IProcessingActivityRegistry>(),
                cache,
                Options.Create(new DataSubjectRightsOptions()),
                new FixedTimeProvider(Now),
                accessor,
                NullLogger<DefaultDSRService>.Instance,
                encina: null,
                tenantProvider: Substitute.For<ITenantProvider>());
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private static (string TenantA, string TenantB, string SubjectId) Ids(NonEmptyString tenant, NonEmptyString subject) =>
        ($"a-{tenant.Get}", $"b-{tenant.Get}", $"subject-{subject.Get}");

    [Property(MaxTest = 50)]
    public bool TenantB_NeverSeesTenantAsRequestsOrRestrictions(NonEmptyString tenant, NonEmptyString subject)
    {
        var (tenantA, tenantB, subjectId) = Ids(tenant, subject);
        var world = new World(tenantA, tenantB);
        var id = world.SeedRestriction(tenantA, subjectId);

        var own = world.A.HasActiveRestrictionAsync(subjectId).AsTask().Result;
        var other = world.B.HasActiveRestrictionAsync(subjectId).AsTask().Result;
        var bySubject = world.B.GetRequestsBySubjectAsync(subjectId).AsTask().Result;
        var pending = world.B.GetPendingRequestsAsync().AsTask().Result;
        var overdue = world.B.GetOverdueRequestsAsync().AsTask().Result;
        var byId = world.B.GetRequestAsync(id).AsTask().Result;

        return own.Match(active => active, _ => false)
            && other.Match(active => !active, _ => false)
            && bySubject.Match(rows => rows.Count == 0, _ => false)
            && pending.Match(rows => rows.All(r => r.Id != id), _ => false)
            && overdue.Match(rows => rows.All(r => r.Id != id), _ => false)
            && byId.Match(_ => false, e => e.GetCode().IfNone(string.Empty) == DSRErrors.RequestNotFoundCode);
    }

    [Property(MaxTest = 50)]
    public bool TenantB_CacheHitsNeverCrossTenants(NonEmptyString tenant, NonEmptyString subject)
    {
        var (tenantA, tenantB, subjectId) = Ids(tenant, subject);
        var world = new World(tenantA, tenantB);
        var id = world.SeedRestriction(tenantA, subjectId);

        // Tenant A warms every cache entry, then tenant B asks for the same keys' subjects.
        _ = world.A.GetRequestAsync(id).AsTask().Result;
        _ = world.A.HasActiveRestrictionAsync(subjectId).AsTask().Result;
        var restrictedForB = world.B.HasActiveRestrictionAsync(subjectId).AsTask().Result;
        var byIdForB = world.B.GetRequestAsync(id).AsTask().Result;

        return world.CacheKeys.All(k => k.StartsWith($"dsr:tenant:{tenantA}:", StringComparison.Ordinal)
                || k.StartsWith($"dsr:tenant:{tenantB}:", StringComparison.Ordinal))
            && !world.CacheKeys.Contains($"dsr:tenant:{tenantB}:request:{id}")
            && restrictedForB.Match(active => !active, _ => false)
            && byIdForB.IsLeft;
    }

    [Property(MaxTest = 50)]
    public bool TenantB_CannotMutateTenantAsRequest(NonEmptyString tenant, NonEmptyString subject)
    {
        var (tenantA, tenantB, subjectId) = Ids(tenant, subject);
        var world = new World(tenantA, tenantB);
        var id = world.SeedRestriction(tenantA, subjectId);

        var results = new[]
        {
            world.B.VerifyIdentityAsync(id, "x").AsTask().Result,
            world.B.StartProcessingAsync(id).AsTask().Result,
            world.B.CompleteRequestAsync(id).AsTask().Result,
            world.B.DenyRequestAsync(id, DSRRejectionReason.Other, "x").AsTask().Result,
            world.B.ExtendDeadlineAsync(id, "x").AsTask().Result,
            world.B.ExpireRequestAsync(id).AsTask().Result
        };

        return world.SavedAggregates == 0
            && results.All(r => r.Match(_ => false, e => e.GetCode().IfNone(string.Empty) == DSRErrors.RequestNotFoundCode));
    }
}
