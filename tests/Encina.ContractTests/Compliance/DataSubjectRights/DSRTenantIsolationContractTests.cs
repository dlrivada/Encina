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
using Encina.Testing.Shouldly;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

using static LanguageExt.Prelude;

namespace Encina.ContractTests.Compliance.DataSubjectRights;

/// <summary>
/// Contract of the tenant isolation of <see cref="IDSRService"/> in a multi-tenant application
/// (#1963): without a tenant every request-store operation fails closed with
/// <see cref="DSRErrors.TenantRequiredCode"/>; a request of another tenant is
/// <see cref="DSRErrors.RequestNotFoundCode"/> for every id-based operation and is never saved; the
/// owning tenant still drives the whole lifecycle.
/// </summary>
public sealed class DSRTenantIsolationContractTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);

    private readonly Dictionary<Guid, DSRRequestAggregate> _aggregates = [];
    private int _saves;

    private DefaultDSRService CreateService(string? tenantId)
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
                _saves++;
                return Task.FromResult(Right<EncinaError, Unit>(unit));
            });
        repository.CreateAsync(Arg.Any<DSRRequestAggregate>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var created = call.Arg<DSRRequestAggregate>();
                _aggregates[created.Id] = created;
                return Task.FromResult(Right<EncinaError, Unit>(unit));
            });

        var readModels = Substitute.For<IReadModelRepository<DSRRequestReadModel>>();
        readModels.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Left<EncinaError, DSRRequestReadModel>(EncinaErrors.Create("test.not_found", "not found"))));
        readModels.QueryAsync(
                Arg.Any<Func<IQueryable<DSRRequestReadModel>, IQueryable<DSRRequestReadModel>>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, IReadOnlyList<DSRRequestReadModel>>([])));

        var accessor = Substitute.For<IRequestContextAccessor>();
        accessor.RequestContext.Returns(tenantId is null ? null : RequestContext.CreateForTest(tenantId: tenantId));

        return new DefaultDSRService(
            repository,
            readModels,
            Substitute.For<IPersonalDataLocator>(),
            Substitute.For<IDataErasureExecutor>(),
            Substitute.For<IDataPortabilityExporter>(),
            Substitute.For<IProcessingActivityRegistry>(),
            Substitute.For<ICacheProvider>(),
            Options.Create(new DataSubjectRightsOptions()),
            new FixedTimeProvider(Now),
            accessor,
            NullLogger<DefaultDSRService>.Instance,
            encina: null,
            tenantProvider: Substitute.For<ITenantProvider>());
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private Guid Seed(string tenantId, DataSubjectRight right = DataSubjectRight.Access)
    {
        var aggregate = DSRRequestAggregate.Submit(Guid.NewGuid(), "subject-1", right, Now, tenantId: tenantId);
        _aggregates[aggregate.Id] = aggregate;
        return aggregate.Id;
    }

    public static TheoryData<string> LifecycleCommands => ["Verify", "Start", "Complete", "Deny", "Extend", "Expire"];

    private static ValueTask<Either<EncinaError, Unit>> Run(DefaultDSRService sut, string command, Guid id) =>
        command switch
        {
            "Verify" => sut.VerifyIdentityAsync(id, "admin"),
            "Start" => sut.StartProcessingAsync(id),
            "Complete" => sut.CompleteRequestAsync(id),
            "Deny" => sut.DenyRequestAsync(id, DSRRejectionReason.Other, "reason"),
            "Extend" => sut.ExtendDeadlineAsync(id, "complex"),
            "Expire" => sut.ExpireRequestAsync(id),
            _ => throw new ArgumentOutOfRangeException(nameof(command))
        };

    [Fact]
    public async Task Contract_NoTenant_QueriesAndSubmitFailClosed()
    {
        var sut = CreateService(tenantId: null);

        (await sut.SubmitRequestAsync("subject-1", DataSubjectRight.Access)).ShouldBeErrorWithCode(DSRErrors.TenantRequiredCode);
        (await sut.GetRequestAsync(Guid.NewGuid())).ShouldBeErrorWithCode(DSRErrors.TenantRequiredCode);
        (await sut.GetRequestsBySubjectAsync("subject-1")).ShouldBeErrorWithCode(DSRErrors.TenantRequiredCode);
        (await sut.GetPendingRequestsAsync()).ShouldBeErrorWithCode(DSRErrors.TenantRequiredCode);
        (await sut.GetOverdueRequestsAsync()).ShouldBeErrorWithCode(DSRErrors.TenantRequiredCode);
        (await sut.HasActiveRestrictionAsync("subject-1")).ShouldBeErrorWithCode(DSRErrors.TenantRequiredCode);
        (await sut.HandleRestrictionAsync(new RestrictionRequest("subject-1", "reason", null)))
            .ShouldBeErrorWithCode(DSRErrors.TenantRequiredCode);
    }

    [Theory]
    [MemberData(nameof(LifecycleCommands))]
    public async Task Contract_NoTenant_LifecycleCommandFailsClosed(string command)
    {
        var sut = CreateService(tenantId: null);

        (await Run(sut, command, Seed("tenant-a"))).ShouldBeErrorWithCode(DSRErrors.TenantRequiredCode);
        _saves.ShouldBe(0);
    }

    [Theory]
    [MemberData(nameof(LifecycleCommands))]
    public async Task Contract_OtherTenantsRequest_IsNotFoundAndNeverSaved(string command)
    {
        var sut = CreateService("tenant-b");

        (await Run(sut, command, Seed("tenant-a"))).ShouldBeErrorWithCode(DSRErrors.RequestNotFoundCode);
        _saves.ShouldBe(0);
    }

    [Fact]
    public async Task Contract_OwnerTenant_DrivesTheLifecycle()
    {
        var sut = CreateService("tenant-a");
        var id = Seed("tenant-a");

        (await sut.VerifyIdentityAsync(id, "admin")).ShouldBeSuccess();
        (await sut.StartProcessingAsync(id)).ShouldBeSuccess();
        (await sut.CompleteRequestAsync(id)).ShouldBeSuccess();
        (await sut.ExtendDeadlineAsync(Seed("tenant-a"), "complex")).ShouldBeSuccess();
        _saves.ShouldBe(4);
    }

    [Fact]
    public async Task Contract_OwnerTenant_CanDenyAndExpire()
    {
        var sut = CreateService("tenant-a");

        (await sut.DenyRequestAsync(Seed("tenant-a"), DSRRejectionReason.Other, "reason")).ShouldBeSuccess();
        (await sut.ExpireRequestAsync(Seed("tenant-a"))).ShouldBeSuccess();
        _saves.ShouldBe(2);
    }

    [Fact]
    public async Task Contract_Submit_TakesAmbientTenantAndRefusesContradiction()
    {
        var sut = CreateService("tenant-a");

        var id = (await sut.SubmitRequestAsync("subject-1", DataSubjectRight.Access)).ShouldBeSuccess();
        _aggregates[id].TenantId.ShouldBe("tenant-a");

        (await sut.SubmitRequestAsync("subject-1", DataSubjectRight.Access, tenantId: "tenant-b"))
            .ShouldBeErrorWithCode(DSRErrors.TenantRequiredCode);
    }
}
