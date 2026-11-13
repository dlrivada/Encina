using Encina.Compliance.Consent;
using Encina.Compliance.Consent.Aggregates;
using Encina.Compliance.Consent.Events;
using Encina.Compliance.Consent.ReadModels;
using Encina.Marten.Projections;
using Shouldly;

namespace Encina.UnitTests.Compliance.Consent.Aggregates;

/// <summary>
/// Tests of the act-timestamp ordering rule of #3771 on <see cref="ConsentAggregate"/> and
/// <see cref="ConsentProjection"/>: the most recent act of consent wins by its own timestamp, a late
/// act is recorded without changing the state, and both sides derive the same state from the same
/// stream.
/// </summary>
public class ConsentActOrderingTests
{
    private static readonly Guid ConsentId = Guid.Parse("8f1c2b3a-4d5e-8f60-8a1b-2c3d4e5f6a7b");
    private static readonly DateTimeOffset T0 = new(2026, 3, 1, 9, 0, 0, TimeSpan.Zero);
    private static readonly IReadOnlyDictionary<string, object?> NoMetadata = new Dictionary<string, object?>();

    private readonly ProjectionContext _context = new();

    // ---- Aggregate commands ----

    [Fact]
    public void RenewByGrant_Active_RaisesEventAndReplacesTheProof()
    {
        var aggregate = Granted();

        aggregate.RenewByGrant("v2", "api", "10.0.0.2", "hash-2", NoMetadata, T0.AddYears(1), "admin", T0.AddDays(1));

        aggregate.UncommittedEvents[^1].ShouldBeOfType<ConsentRenewedByGrant>();
        aggregate.Status.ShouldBe(ConsentStatus.Active);
        aggregate.ConsentVersionId.ShouldBe("v2");
        aggregate.Source.ShouldBe("api");
        aggregate.IpAddress.ShouldBe("10.0.0.2");
        aggregate.ProofOfConsent.ShouldBe("hash-2");
        aggregate.ExpiresAtUtc.ShouldBe(T0.AddYears(1));
        aggregate.LastActAtUtc.ShouldBe(T0.AddDays(1));
        aggregate.GivenAtUtc.ShouldBe(T0);
    }

    [Theory]
    [InlineData(ConsentStatus.Withdrawn)]
    [InlineData(ConsentStatus.Expired)]
    [InlineData(ConsentStatus.RequiresReconsent)]
    public void RenewByGrant_NotActive_Throws(ConsentStatus status)
    {
        var aggregate = InStatus(status);

        Should.Throw<InvalidOperationException>(() =>
            aggregate.RenewByGrant("v2", "api", null, null, NoMetadata, null, "admin", T0.AddDays(30)));
    }

    [Theory]
    [InlineData(ConsentStatus.Withdrawn)]
    [InlineData(ConsentStatus.Expired)]
    public void Regrant_WithdrawnOrExpired_ReactivatesWithFreshProof(ConsentStatus status)
    {
        var aggregate = InStatus(status);

        aggregate.Regrant("v3", "paper", null, "hash-3", NoMetadata, null, "reception", T0.AddDays(30));

        var regrant = aggregate.UncommittedEvents[^1].ShouldBeOfType<ConsentRegranted>();
        regrant.PreviousStatus.ShouldBe(status);
        aggregate.Status.ShouldBe(ConsentStatus.Active);
        aggregate.ConsentVersionId.ShouldBe("v3");
        aggregate.ProofOfConsent.ShouldBe("hash-3");
        aggregate.WithdrawnAtUtc.ShouldBeNull();
        aggregate.WithdrawalReason.ShouldBeNull();
        aggregate.GivenAtUtc.ShouldBe(T0.AddDays(30));
        aggregate.LastActAtUtc.ShouldBe(T0.AddDays(30));
    }

    [Theory]
    [InlineData(ConsentStatus.Active)]
    [InlineData(ConsentStatus.RequiresReconsent)]
    public void Regrant_ActiveOrRequiresReconsent_Throws(ConsentStatus status)
    {
        var aggregate = InStatus(status);

        Should.Throw<InvalidOperationException>(() =>
            aggregate.Regrant("v3", "paper", null, null, NoMetadata, null, "reception", T0.AddDays(30)));
    }

    // ---- Ordering rule on the aggregate ----

    [Fact]
    public void Regrant_ActOlderThanTheWithdrawal_StaysWithdrawn()
    {
        var aggregate = Granted();
        aggregate.Withdraw("subject", "no more", T0.AddDays(10));

        aggregate.Regrant("v1", "web-form", null, null, NoMetadata, null, "admin", T0.AddDays(5));

        aggregate.UncommittedEvents[^1].ShouldBeOfType<ConsentRegranted>();
        aggregate.Status.ShouldBe(ConsentStatus.Withdrawn);
        aggregate.WithdrawnAtUtc.ShouldBe(T0.AddDays(10));
        aggregate.LastActAtUtc.ShouldBe(T0.AddDays(10));
    }

    [Fact]
    public void Regrant_ActOlderThanTheExpiry_StaysExpired()
    {
        var aggregate = Granted(expiresAtUtc: T0.AddDays(20));
        aggregate.Expire(T0.AddDays(25));

        aggregate.Regrant("v1", "web-form", null, null, NoMetadata, null, "admin", T0.AddDays(15));

        aggregate.Status.ShouldBe(ConsentStatus.Expired);
        aggregate.LastActAtUtc.ShouldBe(T0.AddDays(20));
    }

    [Fact]
    public void Regrant_ActAfterTheExpiryInstantButBeforeItsDetection_Reactivates()
    {
        // The expiry advances the watermark to the expiry instant, not to its detection time.
        var aggregate = Granted(expiresAtUtc: T0.AddDays(20));
        aggregate.Expire(T0.AddDays(25));

        aggregate.Regrant("v1", "web-form", null, null, NoMetadata, null, "admin", T0.AddDays(22));

        aggregate.Status.ShouldBe(ConsentStatus.Active);
    }

    [Fact]
    public void RenewByGrant_SameInstantAsTheLatestAct_Applies()
    {
        var aggregate = Granted();

        aggregate.RenewByGrant("v2", "api", null, null, NoMetadata, null, "admin", T0);

        aggregate.ConsentVersionId.ShouldBe("v2");
    }

    [Fact]
    public void LateActs_AfterANewerAct_DoNotOverwriteIt()
    {
        var aggregate = Granted();
        aggregate.RenewByGrant("v3", "api", null, "newest", NoMetadata, null, "admin", T0.AddDays(3));

        aggregate.RenewByGrant("v2", "api", null, "older", NoMetadata, null, "admin", T0.AddDays(2));
        aggregate.Renew("v1", null, "admin", "renew", T0.AddDays(1));

        aggregate.ConsentVersionId.ShouldBe("v3");
        aggregate.ProofOfConsent.ShouldBe("newest");
        aggregate.LastActAtUtc.ShouldBe(T0.AddDays(3));
        aggregate.Version.ShouldBe(4);
    }

    [Fact]
    public void ProvideReconsent_ActOlderThanTheVersionChange_StaysRequiresReconsent()
    {
        var aggregate = Granted();
        aggregate.ChangeVersion("v2", "terms changed", requiresReconsent: true, "legal", T0.AddDays(10));

        aggregate.ProvideReconsent("v2", "web-form", null, null, NoMetadata, null, "admin", T0.AddDays(9));

        aggregate.Status.ShouldBe(ConsentStatus.RequiresReconsent);
    }

    [Fact]
    public void Withdraw_AlwaysAppliesEvenWhenOlderThanTheLatestAct()
    {
        // A withdrawal is never ignored (Art. 7(3)); the watermark keeps the later act's time.
        var aggregate = Granted();
        aggregate.RenewByGrant("v2", "api", null, null, NoMetadata, null, "admin", T0.AddDays(10));

        aggregate.Withdraw("subject", null, T0.AddDays(5));

        aggregate.Status.ShouldBe(ConsentStatus.Withdrawn);
        aggregate.LastActAtUtc.ShouldBe(T0.AddDays(10));
    }

    // ---- Projection mirrors the aggregate ----

    [Fact]
    public void Projection_RenewedByGrant_ReplacesProofAndAdvancesWatermark()
    {
        var projection = new ConsentProjection();
        var model = projection.Create(GrantedEvent(), _context);

        model = projection.Apply(RenewedByGrantEvent(T0.AddDays(1), "v2"), model, _context);

        model.ConsentVersionId.ShouldBe("v2");
        model.ProofOfConsent.ShouldBe("proof-v2");
        model.LastActAtUtc.ShouldBe(T0.AddDays(1));
        model.LastModifiedAtUtc.ShouldBe(T0.AddDays(1));
        model.Version.ShouldBe(2);
    }

    [Fact]
    public void Projection_LateRenewedByGrant_OnlyAdvancesVersion()
    {
        var projection = new ConsentProjection();
        var model = projection.Create(GrantedEvent(), _context);
        model = projection.Apply(RenewedByGrantEvent(T0.AddDays(5), "v2"), model, _context);

        model = projection.Apply(RenewedByGrantEvent(T0.AddDays(1), "v1-late"), model, _context);

        model.ConsentVersionId.ShouldBe("v2");
        model.LastActAtUtc.ShouldBe(T0.AddDays(5));
        model.LastModifiedAtUtc.ShouldBe(T0.AddDays(5));
        model.Version.ShouldBe(3);
    }

    [Fact]
    public void Projection_Regranted_AfterWithdrawal_Reactivates()
    {
        var projection = new ConsentProjection();
        var model = projection.Create(GrantedEvent(), _context);
        model = projection.Apply(new ConsentWithdrawn(ConsentId, "subject-1", "marketing", "subject", null, T0.AddDays(2)), model, _context);

        model = projection.Apply(RegrantedEvent(T0.AddDays(3)), model, _context);

        model.Status.ShouldBe(ConsentStatus.Active);
        model.WithdrawnAtUtc.ShouldBeNull();
        model.GivenAtUtc.ShouldBe(T0.AddDays(3));
        model.Version.ShouldBe(3);
    }

    [Fact]
    public void Projection_LateRegranted_StaysWithdrawn()
    {
        var projection = new ConsentProjection();
        var model = projection.Create(GrantedEvent(), _context);
        model = projection.Apply(new ConsentWithdrawn(ConsentId, "subject-1", "marketing", "subject", null, T0.AddDays(2)), model, _context);

        model = projection.Apply(RegrantedEvent(T0.AddDays(1)), model, _context);

        model.Status.ShouldBe(ConsentStatus.Withdrawn);
        model.WithdrawnAtUtc.ShouldBe(T0.AddDays(2));
        model.GivenAtUtc.ShouldBe(T0);
    }

    [Fact]
    public void Projection_ExpiredAndVersionChanged_AdvanceTheWatermark()
    {
        var projection = new ConsentProjection();
        var model = projection.Create(GrantedEvent(expiresAtUtc: T0.AddDays(20)), _context);

        model = projection.Apply(
            new ConsentVersionChanged(ConsentId, "subject-1", "marketing", "v1", "v1.1", "typo", false, "legal", T0.AddDays(4)),
            model, _context);
        model.LastActAtUtc.ShouldBe(T0.AddDays(4));

        model = projection.Apply(new ConsentExpired(ConsentId, "subject-1", "marketing", T0.AddDays(20), T0.AddDays(21)), model, _context);
        model.LastActAtUtc.ShouldBe(T0.AddDays(20));
    }

    [Fact]
    public void Projection_LateRenewedAndReconsent_DoNotChangeState()
    {
        var projection = new ConsentProjection();
        var model = projection.Create(GrantedEvent(), _context);
        model = projection.Apply(
            new ConsentVersionChanged(ConsentId, "subject-1", "marketing", "v1", "v2", "terms", true, "legal", T0.AddDays(10)),
            model, _context);

        model = projection.Apply(new ConsentRenewed(ConsentId, "subject-1", "marketing", "v0", null, "admin", "api", T0.AddDays(1)), model, _context);
        model = projection.Apply(
            new ConsentReconsentProvided(ConsentId, "subject-1", "marketing", "v2", "web", null, null, NoMetadata, null, "admin", T0.AddDays(2)),
            model, _context);

        model.Status.ShouldBe(ConsentStatus.RequiresReconsent);
        model.ConsentVersionId.ShouldBe("v2");
        model.Version.ShouldBe(4);
    }

    /// <summary>
    /// Property-style invariant over 500 seeded random streams: the aggregate and the projection
    /// derive the same state from the same events, and a late act never changes the status.
    /// </summary>
    [Fact]
    public void RandomStreams_AggregateAndProjectionAgree_AndLateActsNeverChangeStatus()
    {
        for (var seed = 0; seed < 500; seed++)
        {
            var random = new Random(seed);
            var events = RandomStream(random);

            var aggregate = new ConsentAggregate();
            var projection = new ConsentProjection();
            ConsentReadModel? model = null;
            var watermark = DateTimeOffset.MinValue;

            foreach (var domainEvent in events)
            {
                var statusBefore = aggregate.Status;
                var isLateAct = IsGrantAct(domainEvent, out var actAt) && actAt < watermark;

                aggregate.LoadFromHistory([domainEvent]);
                model = ApplyToProjection(projection, model, domainEvent);
                watermark = aggregate.LastActAtUtc;

                if (isLateAct)
                {
                    aggregate.Status.ShouldBe(statusBefore, $"seed {seed}: a late act changed the status");
                }
            }

            model.ShouldNotBeNull();
            model.Status.ShouldBe(aggregate.Status, $"seed {seed}");
            model.ConsentVersionId.ShouldBe(aggregate.ConsentVersionId, $"seed {seed}");
            model.ExpiresAtUtc.ShouldBe(aggregate.ExpiresAtUtc, $"seed {seed}");
            model.ProofOfConsent.ShouldBe(aggregate.ProofOfConsent, $"seed {seed}");
            model.WithdrawnAtUtc.ShouldBe(aggregate.WithdrawnAtUtc, $"seed {seed}");
            model.GivenAtUtc.ShouldBe(aggregate.GivenAtUtc, $"seed {seed}");
            model.LastActAtUtc.ShouldBe(aggregate.LastActAtUtc, $"seed {seed}");
            model.Version.ShouldBe(aggregate.Version, $"seed {seed}");
        }
    }

    // ---- Helpers ----

    private static ConsentAggregate Granted(DateTimeOffset? expiresAtUtc = null) =>
        ConsentAggregate.Grant(
            ConsentId, "subject-1", "marketing", "v1", "web-form", null, null, NoMetadata, expiresAtUtc, "admin", T0);

    private static ConsentAggregate InStatus(ConsentStatus status)
    {
        var aggregate = Granted(expiresAtUtc: T0.AddDays(20));
        switch (status)
        {
            case ConsentStatus.Withdrawn:
                aggregate.Withdraw("subject", null, T0.AddDays(1));
                break;
            case ConsentStatus.Expired:
                aggregate.Expire(T0.AddDays(21));
                break;
            case ConsentStatus.RequiresReconsent:
                aggregate.ChangeVersion("v2", "terms", requiresReconsent: true, "legal", T0.AddDays(1));
                break;
        }

        aggregate.Status.ShouldBe(status);
        return aggregate;
    }

    private static ConsentGranted GrantedEvent(DateTimeOffset? expiresAtUtc = null) =>
        new(ConsentId, "subject-1", "marketing", "v1", "web-form", null, null, NoMetadata, expiresAtUtc, "admin", T0, null, null);

    private static ConsentRenewedByGrant RenewedByGrantEvent(DateTimeOffset at, string version) =>
        new(ConsentId, "subject-1", "marketing", version, "api", null, $"proof-{version}", NoMetadata, null, "admin", at);

    private static ConsentRegranted RegrantedEvent(DateTimeOffset at) =>
        new(ConsentId, "subject-1", "marketing", ConsentStatus.Withdrawn, "v1", "web-form", null, "proof-regrant", NoMetadata, null, "admin", at);

    private static bool IsGrantAct(object domainEvent, out DateTimeOffset actAt)
    {
        (var isAct, actAt) = domainEvent switch
        {
            ConsentRenewedByGrant e => (true, e.OccurredAtUtc),
            ConsentRegranted e => (true, e.OccurredAtUtc),
            ConsentReconsentProvided e => (true, e.OccurredAtUtc),
            ConsentRenewed e => (true, e.OccurredAtUtc),
            _ => (false, default)
        };
        return isAct;
    }

    private ConsentReadModel ApplyToProjection(ConsentProjection projection, ConsentReadModel? model, object domainEvent) =>
        domainEvent switch
        {
            ConsentGranted e => projection.Create(e, _context),
            ConsentWithdrawn e => projection.Apply(e, model!, _context),
            ConsentExpired e => projection.Apply(e, model!, _context),
            ConsentRenewed e => projection.Apply(e, model!, _context),
            ConsentVersionChanged e => projection.Apply(e, model!, _context),
            ConsentReconsentProvided e => projection.Apply(e, model!, _context),
            ConsentRenewedByGrant e => projection.Apply(e, model!, _context),
            ConsentRegranted e => projection.Apply(e, model!, _context),
            _ => throw new InvalidOperationException(domainEvent.GetType().Name)
        };

    /// <summary>
    /// A granted stream followed by 1-12 events of every type, with timestamps that move forward
    /// or jump back, so late acts are frequent.
    /// </summary>
    private static List<object> RandomStream(Random random)
    {
        var events = new List<object> { GrantedEvent(expiresAtUtc: T0.AddDays(random.Next(1, 60))) };
        var count = random.Next(1, 13);

        for (var i = 0; i < count; i++)
        {
            var at = T0.AddHours(random.Next(-48, 24 * 60));
            var version = $"v{random.Next(1, 4)}";
            var proof = $"proof-{random.Next(100)}";
            DateTimeOffset? expires = random.Next(2) == 0 ? null : at.AddDays(random.Next(1, 30));

            events.Add(random.Next(7) switch
            {
                0 => new ConsentWithdrawn(ConsentId, "subject-1", "marketing", "subject", null, at),
                1 => new ConsentExpired(ConsentId, "subject-1", "marketing", at, at.AddHours(1)),
                2 => new ConsentRenewed(ConsentId, "subject-1", "marketing", version, expires, "admin", random.Next(2) == 0 ? null : "api", at),
                3 => new ConsentVersionChanged(ConsentId, "subject-1", "marketing", "v1", version, "terms", random.Next(2) == 0, "legal", at),
                4 => new ConsentReconsentProvided(ConsentId, "subject-1", "marketing", version, "web", null, proof, NoMetadata, expires, "admin", at),
                5 => new ConsentRenewedByGrant(ConsentId, "subject-1", "marketing", version, "api", null, proof, NoMetadata, expires, "admin", at),
                _ => new ConsentRegranted(ConsentId, "subject-1", "marketing", ConsentStatus.Withdrawn, version, "paper", null, proof, NoMetadata, expires, "reception", at)
            });
        }

        return events;
    }
}
