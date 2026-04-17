using Encina.Compliance.Consent;
using Encina.Compliance.Consent.Events;
using Encina.Compliance.Consent.ReadModels;
using Encina.Marten.Projections;
using Shouldly;

namespace Encina.UnitTests.Compliance.Consent.ReadModels;

/// <summary>
/// Unit tests for <see cref="ConsentProjection"/>.
/// </summary>
public class ConsentProjectionTests
{
    private readonly ConsentProjection _sut = new();
    private readonly ProjectionContext _context = new();

    private static ConsentGranted CreateConsentGrantedEvent(
        Guid? consentId = null,
        string dataSubjectId = "user-123",
        string purpose = "marketing",
        string consentVersionId = "v1.0",
        string source = "web-form",
        string? ipAddress = "192.168.1.1",
        string? proofOfConsent = "form-hash-abc",
        IReadOnlyDictionary<string, object?>? metadata = null,
        DateTimeOffset? expiresAtUtc = null,
        string grantedBy = "system",
        DateTimeOffset? occurredAtUtc = null,
        string? tenantId = "tenant-1",
        string? moduleId = "module-1")
    {
        return new ConsentGranted(
            ConsentId: consentId ?? Guid.NewGuid(),
            DataSubjectId: dataSubjectId,
            Purpose: purpose,
            ConsentVersionId: consentVersionId,
            Source: source,
            IpAddress: ipAddress,
            ProofOfConsent: proofOfConsent,
            Metadata: metadata ?? new Dictionary<string, object?> { ["browser"] = "Chrome" },
            ExpiresAtUtc: expiresAtUtc ?? DateTimeOffset.UtcNow.AddDays(365),
            GrantedBy: grantedBy,
            OccurredAtUtc: occurredAtUtc ?? DateTimeOffset.UtcNow,
            TenantId: tenantId,
            ModuleId: moduleId);
    }

    private ConsentReadModel CreateActiveReadModel(Guid? consentId = null, int version = 1)
    {
        var granted = CreateConsentGrantedEvent(consentId: consentId);
        var readModel = _sut.Create(granted, _context);
        readModel.Version = version;
        return readModel;
    }

    #region ProjectionName

    [Fact]
    public void ProjectionName_ShouldReturnConsentProjection()
    {
        // Act
        var name = _sut.ProjectionName;

        // Assert
        name.ShouldBe("ConsentProjection");
    }

    #endregion

    #region Create (ConsentGranted)

    [Fact]
    public void Create_ConsentGranted_ShouldMapAllFieldsCorrectly()
    {
        // Arrange
        var consentId = Guid.NewGuid();
        var occurredAt = new DateTimeOffset(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);
        var expiresAt = occurredAt.AddDays(365);
        var metadata = new Dictionary<string, object?> { ["browser"] = "Firefox", ["variant"] = "A" };

        var granted = CreateConsentGrantedEvent(
            consentId: consentId,
            dataSubjectId: "subject-456",
            purpose: "analytics",
            consentVersionId: "v2.1",
            source: "mobile-app",
            ipAddress: "10.0.0.1",
            proofOfConsent: "hash-xyz",
            metadata: metadata,
            expiresAtUtc: expiresAt,
            grantedBy: "admin",
            occurredAtUtc: occurredAt,
            tenantId: "tenant-A",
            moduleId: "module-B");

        // Act
        var result = _sut.Create(granted, _context);

        // Assert
        result.Id.ShouldBe(consentId);
        result.DataSubjectId.ShouldBe("subject-456");
        result.Purpose.ShouldBe("analytics");
        result.ConsentVersionId.ShouldBe("v2.1");
        result.Source.ShouldBe("mobile-app");
        result.IpAddress.ShouldBe("10.0.0.1");
        result.ProofOfConsent.ShouldBe("hash-xyz");
        result.Metadata.ShouldBe(metadata);
        result.ExpiresAtUtc.ShouldBe(expiresAt);
        result.GivenAtUtc.ShouldBe(occurredAt);
        result.TenantId.ShouldBe("tenant-A");
        result.ModuleId.ShouldBe("module-B");
        result.LastModifiedAtUtc.ShouldBe(occurredAt);
    }

    [Fact]
    public void Create_ConsentGranted_ShouldSetStatusToActive()
    {
        // Arrange
        var granted = CreateConsentGrantedEvent();

        // Act
        var result = _sut.Create(granted, _context);

        // Assert
        result.Status.ShouldBe(ConsentStatus.Active);
    }

    [Fact]
    public void Create_ConsentGranted_ShouldSetVersionToOne()
    {
        // Arrange
        var granted = CreateConsentGrantedEvent();

        // Act
        var result = _sut.Create(granted, _context);

        // Assert
        result.Version.ShouldBe(1);
    }

    #endregion

    #region Apply (ConsentWithdrawn)

    [Fact]
    public void Apply_ConsentWithdrawn_ShouldSetStatusToWithdrawn()
    {
        // Arrange
        var readModel = CreateActiveReadModel();
        var withdrawnAt = new DateTimeOffset(2026, 6, 1, 14, 30, 0, TimeSpan.Zero);
        var withdrawn = new ConsentWithdrawn(
            ConsentId: readModel.Id,
            DataSubjectId: readModel.DataSubjectId,
            Purpose: readModel.Purpose,
            WithdrawnBy: "user-123",
            Reason: "No longer interested",
            OccurredAtUtc: withdrawnAt);

        // Act
        var result = _sut.Apply(withdrawn, readModel, _context);

        // Assert
        result.Status.ShouldBe(ConsentStatus.Withdrawn);
        result.WithdrawnAtUtc.ShouldBe(withdrawnAt);
        result.LastModifiedAtUtc.ShouldBe(withdrawnAt);
    }

    [Fact]
    public void Apply_ConsentWithdrawn_ShouldIncrementVersion()
    {
        // Arrange
        var readModel = CreateActiveReadModel(version: 3);
        var withdrawn = new ConsentWithdrawn(
            ConsentId: readModel.Id,
            DataSubjectId: readModel.DataSubjectId,
            Purpose: readModel.Purpose,
            WithdrawnBy: "user-123",
            Reason: null,
            OccurredAtUtc: DateTimeOffset.UtcNow);

        // Act
        var result = _sut.Apply(withdrawn, readModel, _context);

        // Assert
        result.Version.ShouldBe(4);
    }

    #endregion

    #region Apply (ConsentExpired)

    [Fact]
    public void Apply_ConsentExpired_ShouldSetStatusToExpired()
    {
        // Arrange
        var readModel = CreateActiveReadModel();
        var expiredAt = new DateTimeOffset(2026, 12, 31, 23, 59, 59, TimeSpan.Zero);
        var expired = new ConsentExpired(
            ConsentId: readModel.Id,
            DataSubjectId: readModel.DataSubjectId,
            Purpose: readModel.Purpose,
            ExpiredAtUtc: expiredAt,
            OccurredAtUtc: expiredAt);

        // Act
        var result = _sut.Apply(expired, readModel, _context);

        // Assert
        result.Status.ShouldBe(ConsentStatus.Expired);
        result.LastModifiedAtUtc.ShouldBe(expiredAt);
    }

    [Fact]
    public void Apply_ConsentExpired_ShouldIncrementVersion()
    {
        // Arrange
        var readModel = CreateActiveReadModel(version: 2);
        var expired = new ConsentExpired(
            ConsentId: readModel.Id,
            DataSubjectId: readModel.DataSubjectId,
            Purpose: readModel.Purpose,
            ExpiredAtUtc: DateTimeOffset.UtcNow,
            OccurredAtUtc: DateTimeOffset.UtcNow);

        // Act
        var result = _sut.Apply(expired, readModel, _context);

        // Assert
        result.Version.ShouldBe(3);
    }

    #endregion

    #region Apply (ConsentRenewed)

    [Fact]
    public void Apply_ConsentRenewed_ShouldUpdateVersionIdAndExpiry()
    {
        // Arrange
        var readModel = CreateActiveReadModel();
        var newExpiry = new DateTimeOffset(2027, 6, 15, 0, 0, 0, TimeSpan.Zero);
        var renewedAt = new DateTimeOffset(2026, 6, 15, 10, 0, 0, TimeSpan.Zero);
        var renewed = new ConsentRenewed(
            ConsentId: readModel.Id,
            DataSubjectId: readModel.DataSubjectId,
            Purpose: readModel.Purpose,
            ConsentVersionId: "v2.0",
            NewExpiresAtUtc: newExpiry,
            RenewedBy: "user-123",
            Source: "api",
            OccurredAtUtc: renewedAt);

        // Act
        var result = _sut.Apply(renewed, readModel, _context);

        // Assert
        result.ConsentVersionId.ShouldBe("v2.0");
        result.ExpiresAtUtc.ShouldBe(newExpiry);
        result.Source.ShouldBe("api");
        result.LastModifiedAtUtc.ShouldBe(renewedAt);
    }

    [Fact]
    public void Apply_ConsentRenewed_WithNullSource_ShouldNotUpdateSource()
    {
        // Arrange
        var readModel = CreateActiveReadModel();
        var originalSource = readModel.Source;
        var renewed = new ConsentRenewed(
            ConsentId: readModel.Id,
            DataSubjectId: readModel.DataSubjectId,
            Purpose: readModel.Purpose,
            ConsentVersionId: "v2.0",
            NewExpiresAtUtc: DateTimeOffset.UtcNow.AddDays(365),
            RenewedBy: "user-123",
            Source: null,
            OccurredAtUtc: DateTimeOffset.UtcNow);

        // Act
        var result = _sut.Apply(renewed, readModel, _context);

        // Assert
        result.Source.ShouldBe(originalSource);
    }

    [Fact]
    public void Apply_ConsentRenewed_ShouldIncrementVersion()
    {
        // Arrange
        var readModel = CreateActiveReadModel(version: 5);
        var renewed = new ConsentRenewed(
            ConsentId: readModel.Id,
            DataSubjectId: readModel.DataSubjectId,
            Purpose: readModel.Purpose,
            ConsentVersionId: "v3.0",
            NewExpiresAtUtc: null,
            RenewedBy: "admin",
            Source: null,
            OccurredAtUtc: DateTimeOffset.UtcNow);

        // Act
        var result = _sut.Apply(renewed, readModel, _context);

        // Assert
        result.Version.ShouldBe(6);
    }

    #endregion

    #region Apply (ConsentVersionChanged)

    [Fact]
    public void Apply_ConsentVersionChanged_WithRequiresReconsent_ShouldSetStatusToRequiresReconsent()
    {
        // Arrange
        var readModel = CreateActiveReadModel();
        var changedAt = new DateTimeOffset(2026, 7, 1, 9, 0, 0, TimeSpan.Zero);
        var versionChanged = new ConsentVersionChanged(
            ConsentId: readModel.Id,
            DataSubjectId: readModel.DataSubjectId,
            Purpose: readModel.Purpose,
            PreviousVersionId: "v1.0",
            NewVersionId: "v2.0",
            Description: "Updated data processing scope",
            RequiresReconsent: true,
            ChangedBy: "legal-team",
            OccurredAtUtc: changedAt);

        // Act
        var result = _sut.Apply(versionChanged, readModel, _context);

        // Assert
        result.Status.ShouldBe(ConsentStatus.RequiresReconsent);
        result.ConsentVersionId.ShouldBe("v2.0");
        result.LastModifiedAtUtc.ShouldBe(changedAt);
    }

    [Fact]
    public void Apply_ConsentVersionChanged_WithoutRequiresReconsent_ShouldKeepCurrentStatus()
    {
        // Arrange
        var readModel = CreateActiveReadModel();
        var versionChanged = new ConsentVersionChanged(
            ConsentId: readModel.Id,
            DataSubjectId: readModel.DataSubjectId,
            Purpose: readModel.Purpose,
            PreviousVersionId: "v1.0",
            NewVersionId: "v1.1",
            Description: "Minor clarification",
            RequiresReconsent: false,
            ChangedBy: "legal-team",
            OccurredAtUtc: DateTimeOffset.UtcNow);

        // Act
        var result = _sut.Apply(versionChanged, readModel, _context);

        // Assert
        result.Status.ShouldBe(ConsentStatus.Active);
        result.ConsentVersionId.ShouldBe("v1.1");
    }

    #endregion

    #region Apply (ConsentReconsentProvided)

    [Fact]
    public void Apply_ConsentReconsentProvided_ShouldReactivateAndUpdateAllProofFields()
    {
        // Arrange
        var readModel = CreateActiveReadModel(version: 3);
        readModel.Status = ConsentStatus.RequiresReconsent;
        readModel.WithdrawnAtUtc = DateTimeOffset.UtcNow.AddDays(-1);

        var reconsentAt = new DateTimeOffset(2026, 8, 1, 12, 0, 0, TimeSpan.Zero);
        var newExpiry = reconsentAt.AddDays(365);
        var newMetadata = new Dictionary<string, object?> { ["consent-form"] = "v2-form" };

        var reconsent = new ConsentReconsentProvided(
            ConsentId: readModel.Id,
            DataSubjectId: readModel.DataSubjectId,
            Purpose: readModel.Purpose,
            NewConsentVersionId: "v2.0",
            Source: "api",
            IpAddress: "10.0.0.99",
            ProofOfConsent: "new-hash-def",
            Metadata: newMetadata,
            ExpiresAtUtc: newExpiry,
            GrantedBy: "user-123",
            OccurredAtUtc: reconsentAt);

        // Act
        var result = _sut.Apply(reconsent, readModel, _context);

        // Assert
        result.Status.ShouldBe(ConsentStatus.Active);
        result.ConsentVersionId.ShouldBe("v2.0");
        result.Source.ShouldBe("api");
        result.IpAddress.ShouldBe("10.0.0.99");
        result.ProofOfConsent.ShouldBe("new-hash-def");
        result.Metadata.ShouldBe(newMetadata);
        result.ExpiresAtUtc.ShouldBe(newExpiry);
        result.WithdrawnAtUtc.ShouldBeNull();
        result.LastModifiedAtUtc.ShouldBe(reconsentAt);
    }

    [Fact]
    public void Apply_ConsentReconsentProvided_ShouldIncrementVersion()
    {
        // Arrange
        var readModel = CreateActiveReadModel(version: 4);
        readModel.Status = ConsentStatus.RequiresReconsent;

        var reconsent = new ConsentReconsentProvided(
            ConsentId: readModel.Id,
            DataSubjectId: readModel.DataSubjectId,
            Purpose: readModel.Purpose,
            NewConsentVersionId: "v3.0",
            Source: "web-form",
            IpAddress: null,
            ProofOfConsent: null,
            Metadata: new Dictionary<string, object?>(),
            ExpiresAtUtc: null,
            GrantedBy: "system",
            OccurredAtUtc: DateTimeOffset.UtcNow);

        // Act
        var result = _sut.Apply(reconsent, readModel, _context);

        // Assert
        result.Version.ShouldBe(5);
    }

    #endregion

    #region Full Lifecycle

    [Fact]
    public void FullLifecycle_Grant_VersionChange_Reconsent_Withdraw_ShouldTrackStateCorrectly()
    {
        // Arrange
        var consentId = Guid.NewGuid();
        var t0 = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        // Step 1: Grant consent
        var granted = CreateConsentGrantedEvent(
            consentId: consentId,
            dataSubjectId: "user-lifecycle",
            purpose: "marketing",
            consentVersionId: "v1.0",
            source: "web-form",
            occurredAtUtc: t0);

        var readModel = _sut.Create(granted, _context);
        readModel.Status.ShouldBe(ConsentStatus.Active);
        readModel.Version.ShouldBe(1);

        // Step 2: Version change requiring reconsent
        var t1 = t0.AddDays(30);
        var versionChanged = new ConsentVersionChanged(
            ConsentId: consentId,
            DataSubjectId: "user-lifecycle",
            Purpose: "marketing",
            PreviousVersionId: "v1.0",
            NewVersionId: "v2.0",
            Description: "Expanded data sharing scope",
            RequiresReconsent: true,
            ChangedBy: "legal-team",
            OccurredAtUtc: t1);

        readModel = _sut.Apply(versionChanged, readModel, _context);
        readModel.Status.ShouldBe(ConsentStatus.RequiresReconsent);
        readModel.ConsentVersionId.ShouldBe("v2.0");
        readModel.Version.ShouldBe(2);

        // Step 3: Data subject provides reconsent
        var t2 = t1.AddDays(5);
        var reconsent = new ConsentReconsentProvided(
            ConsentId: consentId,
            DataSubjectId: "user-lifecycle",
            Purpose: "marketing",
            NewConsentVersionId: "v2.0",
            Source: "mobile-app",
            IpAddress: "172.16.0.1",
            ProofOfConsent: "reconsent-proof-123",
            Metadata: new Dictionary<string, object?> { ["channel"] = "push-notification" },
            ExpiresAtUtc: t2.AddDays(365),
            GrantedBy: "user-lifecycle",
            OccurredAtUtc: t2);

        readModel = _sut.Apply(reconsent, readModel, _context);
        readModel.Status.ShouldBe(ConsentStatus.Active);
        readModel.Source.ShouldBe("mobile-app");
        readModel.WithdrawnAtUtc.ShouldBeNull();
        readModel.Version.ShouldBe(3);

        // Step 4: Data subject withdraws consent
        var t3 = t2.AddDays(60);
        var withdrawn = new ConsentWithdrawn(
            ConsentId: consentId,
            DataSubjectId: "user-lifecycle",
            Purpose: "marketing",
            WithdrawnBy: "user-lifecycle",
            Reason: "Privacy concerns",
            OccurredAtUtc: t3);

        readModel = _sut.Apply(withdrawn, readModel, _context);
        readModel.Status.ShouldBe(ConsentStatus.Withdrawn);
        readModel.WithdrawnAtUtc.ShouldBe(t3);
        readModel.LastModifiedAtUtc.ShouldBe(t3);
        readModel.Version.ShouldBe(4);
    }

    #endregion
}
