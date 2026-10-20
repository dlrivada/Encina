using Encina.Compliance.DataSubjectRights;
using Encina.DomainModeling;
using Encina.Marten.GDPR;

namespace Encina.UnitTests.Marten.GDPR.Nested;

public sealed class PatientAggregate : AggregateBase
{
    public string? PatientId { get; set; }

    [PersonalData]
    [CryptoShredded(SubjectIdProperty = nameof(PatientId))]
    public string? Email { get; set; }

    public void Start(Guid id) => RaiseEvent(new PatientStarted(id));

    protected override void Apply(object domainEvent)
    {
        if (domainEvent is PatientStarted started)
        {
            Id = started.Id;
        }
    }
}

public sealed record PatientStarted(Guid Id);

public sealed class PatientEntity : Entity<Guid>
{
    public PatientEntity()
        : base(Guid.Empty)
    {
    }

    public PatientEntity(Guid id)
        : base(id)
    {
    }

    public string? PatientId { get; set; }

    [PersonalData]
    [CryptoShredded(SubjectIdProperty = nameof(PatientId))]
    public string? Email { get; set; }
}

public sealed class SoftDeletablePatient : SoftDeletableEntity<Guid>
{
    public SoftDeletablePatient()
        : base(Guid.Empty)
    {
    }

    public string? PatientId { get; set; }

    [PersonalData]
    [CryptoShredded(SubjectIdProperty = nameof(PatientId))]
    public string? Email { get; set; }
}

/// <summary>
/// Encina.DomainModeling base types as crypto owners, and the per-call frames: pairing after failures,
/// isolation between threads and async flows, filter restoration and key ownership (#1698).
/// </summary>
[Trait("Category", "Unit")]
public sealed class CryptoShreddingCallScopeAndDomainOwnerTests : IDisposable
{
    private const string Subject = "subject-d";
    private const string Secret = "domain-secret@example.com";

    private readonly CryptoHarness _harness = new();

    public void Dispose() => _harness.Dispose();

    [Fact]
    public void AggregateBaseOwner_RoundTripsWithItsIdAndWithoutUncommittedEvents()
    {
        var id = Guid.NewGuid();
        var aggregate = new PatientAggregate { PatientId = Subject, Email = Secret };
        aggregate.Start(id);

        var json = _harness.Serializer.ToJson(aggregate);
        var read = _harness.FromJson<PatientAggregate>(json);

        json.ShouldNotContain(Secret);
        json.ShouldNotContain(nameof(AggregateBase.UncommittedEvents));
        read.Id.ShouldBe(id);
        read.Email.ShouldBe(Secret);
    }

    [Fact]
    public void EntityOwner_RoundTripsWithItsIdAndWithoutDomainEvents()
    {
        var id = Guid.NewGuid();
        var entity = new PatientEntity(id) { PatientId = Subject, Email = Secret };

        var json = _harness.Serializer.ToJson(entity);
        var read = _harness.FromJson<PatientEntity>(json);

        json.ShouldNotContain(Secret);
        json.ShouldNotContain("DomainEvents");
        read.Id.ShouldBe(id);
        read.Email.ShouldBe(Secret);
    }

    [Fact]
    public void SoftDeletableEntityOwner_IsAcceptedAndKeepsItsDeletionState()
    {
        var entity = new SoftDeletablePatient { PatientId = Subject, Email = Secret };
        entity.Delete("admin");

        var read = _harness.FromJson<SoftDeletablePatient>(_harness.Serializer.ToJson(entity));

        read.Email.ShouldBe(Secret);
        read.IsDeleted.ShouldBeTrue();
        read.DeletedBy.ShouldBe("admin");
    }

    [Fact]
    public void Frames_ArePoppedAfterAFailingWriteOrRead()
    {
        Should.Throw<CryptoShreddingEncryptionException>(() => _harness.Serializer.ToJson(new TopLevelOwner { Email = Secret }));
        Should.Throw<CryptoShreddingDecryptionException>(() => _harness.FromJson<TopLevelOwner>("{\"PatientId\":\"p\",\"Email\":\"plain\"}"));

        CryptoShreddingCallScope.CurrentWrite.ShouldBeNull();
        CryptoShreddingCallScope.CurrentRead.ShouldBeNull();
    }

    [Fact]
    public void SubjectFilter_IsRestoredWhenDisposed()
    {
        using (CryptoShreddingCallScope.FilterToSubject("outer"))
        {
            using (CryptoShreddingCallScope.FilterToSubject("inner"))
            {
                CryptoShreddingCallScope.CurrentSubjectFilter.ShouldBe("inner");
            }

            CryptoShreddingCallScope.CurrentSubjectFilter.ShouldBe("outer");
        }

        CryptoShreddingCallScope.CurrentSubjectFilter.ShouldBeNull();
    }

    [Fact]
    public async Task ParallelReadsAndWrites_NeverBleedKeysOrFramesAcrossCalls()
    {
        var tasks = Enumerable.Range(0, 32).Select(i => Task.Run(async () =>
        {
            var subject = $"s-{i % 4}";
            var value = new CollectionsEvent { List = [new() { SubjectId = subject, Email = $"mail-{i}" }] };
            var json = _harness.Serializer.ToJson(value);
            await Task.Yield();
            var read = await _harness.FromJsonAsync<CollectionsEvent>(json);
            return read.List[0]!.Email == $"mail-{i}";
        }));

        (await Task.WhenAll(tasks)).ShouldAllBe(ok => ok);
        CryptoShreddingCallScope.CurrentWrite.ShouldBeNull();
        CryptoShreddingCallScope.CurrentRead.ShouldBeNull();
    }

    [Fact]
    public async Task ProviderKeyBytes_AreNeverZeroedOrKeptByTheFrame()
    {
        _harness.Serializer.ToJson(new TopLevelOwner { PatientId = Subject, Email = "first" });
        var before = (await _harness.Keys.GetSubjectKeyAsync(Subject, 1)).Match(k => k, _ => []);

        var json = _harness.Serializer.ToJson(new TopLevelOwner { PatientId = Subject, Email = "second" });
        _harness.FromJson<TopLevelOwner>(json).Email.ShouldBe("second");
        var after = (await _harness.Keys.GetSubjectKeyAsync(Subject, 1)).Match(k => k, _ => []);

        after.ShouldBe(before);
        after.Any(b => b != 0).ShouldBeTrue();
    }
}
