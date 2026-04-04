using Encina.Compliance.DataSubjectRights;
using Encina.Compliance.GDPR;

namespace Encina.UnitTests.Compliance.DataSubjectRights;

/// <summary>
/// Unit tests for model records verifying initialization, property access, and record semantics.
/// </summary>
public class ModelRecordTests
{
    #region PersonalDataLocation

    [Fact]
    public void PersonalDataLocation_PropertiesAreSet()
    {
        var location = new PersonalDataLocation
        {
            EntityType = typeof(string),
            EntityId = "id-1",
            FieldName = "Email",
            Category = PersonalDataCategory.Contact,
            IsErasable = true,
            IsPortable = false,
            HasLegalRetention = true,
            CurrentValue = "test@example.com"
        };

        location.EntityType.ShouldBe(typeof(string));
        location.EntityId.ShouldBe("id-1");
        location.FieldName.ShouldBe("Email");
        location.Category.ShouldBe(PersonalDataCategory.Contact);
        location.IsErasable.ShouldBeTrue();
        location.IsPortable.ShouldBeFalse();
        location.HasLegalRetention.ShouldBeTrue();
        location.CurrentValue.ShouldBe("test@example.com");
    }

    [Fact]
    public void PersonalDataLocation_NullCurrentValue_IsAllowed()
    {
        var location = new PersonalDataLocation
        {
            EntityType = typeof(string),
            EntityId = "id-1",
            FieldName = "Email",
            Category = PersonalDataCategory.Contact,
            IsErasable = true,
            IsPortable = true,
            HasLegalRetention = false,
            CurrentValue = null
        };

        location.CurrentValue.ShouldBeNull();
    }

    #endregion

    #region PersonalDataField

    [Fact]
    public void PersonalDataField_PropertiesAreSet()
    {
        var field = new PersonalDataField
        {
            PropertyName = "Email",
            Category = PersonalDataCategory.Contact,
            IsErasable = true,
            IsPortable = true,
            HasLegalRetention = false
        };

        field.PropertyName.ShouldBe("Email");
        field.Category.ShouldBe(PersonalDataCategory.Contact);
        field.IsErasable.ShouldBeTrue();
        field.IsPortable.ShouldBeTrue();
        field.HasLegalRetention.ShouldBeFalse();
    }

    #endregion

    #region ErasureResult

    [Fact]
    public void ErasureResult_PropertiesAreSet()
    {
        var result = new ErasureResult
        {
            FieldsErased = 5,
            FieldsRetained = 2,
            FieldsFailed = 1,
            RetentionReasons = [new RetentionDetail { FieldName = "TaxId", EntityType = typeof(object), Reason = "Tax law" }],
            Exemptions = [ErasureExemption.LegalObligation]
        };

        result.FieldsErased.ShouldBe(5);
        result.FieldsRetained.ShouldBe(2);
        result.FieldsFailed.ShouldBe(1);
        result.RetentionReasons.Count.ShouldBe(1);
        result.Exemptions.Count.ShouldBe(1);
    }

    #endregion

    #region RetentionDetail

    [Fact]
    public void RetentionDetail_PropertiesAreSet()
    {
        var detail = new RetentionDetail
        {
            FieldName = "TaxId",
            EntityType = typeof(object),
            Reason = "7-year tax retention"
        };

        detail.FieldName.ShouldBe("TaxId");
        detail.EntityType.ShouldBe(typeof(object));
        detail.Reason.ShouldBe("7-year tax retention");
    }

    #endregion

    #region ExportedData

    [Fact]
    public void ExportedData_PropertiesAreSet()
    {
        var exported = new ExportedData
        {
            Content = [1, 2, 3],
            ContentType = "application/json",
            FileName = "export.json",
            Format = ExportFormat.JSON,
            FieldCount = 10
        };

        exported.Content.Length.ShouldBe(3);
        exported.ContentType.ShouldBe("application/json");
        exported.FileName.ShouldBe("export.json");
        exported.Format.ShouldBe(ExportFormat.JSON);
        exported.FieldCount.ShouldBe(10);
    }

    #endregion

    #region ErasureScope

    [Fact]
    public void ErasureScope_PropertiesAreSet()
    {
        var scope = new ErasureScope
        {
            Categories = [PersonalDataCategory.Contact, PersonalDataCategory.Identity],
            SpecificFields = ["Email"],
            Reason = ErasureReason.ConsentWithdrawn,
            ExemptionsToApply = [ErasureExemption.PublicHealth]
        };

        scope.Categories!.Count.ShouldBe(2);
        scope.SpecificFields!.Count.ShouldBe(1);
        scope.Reason.ShouldBe(ErasureReason.ConsentWithdrawn);
        scope.ExemptionsToApply!.Count.ShouldBe(1);
    }

    [Fact]
    public void ErasureScope_NullCollections_AreAllowed()
    {
        var scope = new ErasureScope
        {
            Reason = ErasureReason.ObjectionToProcessing,
            Categories = null,
            SpecificFields = null,
            ExemptionsToApply = null
        };

        scope.Categories.ShouldBeNull();
        scope.SpecificFields.ShouldBeNull();
        scope.ExemptionsToApply.ShouldBeNull();
    }

    #endregion

    #region AccessResponse

    [Fact]
    public void AccessResponse_PropertiesAreSet()
    {
        var now = DateTimeOffset.UtcNow;
        var response = new AccessResponse
        {
            SubjectId = "subject-1",
            Data = [],
            ProcessingActivities = [],
            GeneratedAtUtc = now
        };

        response.SubjectId.ShouldBe("subject-1");
        response.Data.Count.ShouldBe(0);
        response.ProcessingActivities.Count.ShouldBe(0);
        response.GeneratedAtUtc.ShouldBe(now);
    }

    #endregion

    #region PortabilityResponse

    [Fact]
    public void PortabilityResponse_PropertiesAreSet()
    {
        var now = DateTimeOffset.UtcNow;
        var exported = new ExportedData
        {
            Content = [],
            ContentType = "application/json",
            FileName = "export.json",
            Format = ExportFormat.JSON,
            FieldCount = 0
        };

        var response = new PortabilityResponse
        {
            SubjectId = "subject-1",
            ExportedData = exported,
            GeneratedAtUtc = now
        };

        response.SubjectId.ShouldBe("subject-1");
        response.ExportedData.ShouldBeSameAs(exported);
        response.GeneratedAtUtc.ShouldBe(now);
    }

    #endregion

    #region DSRAuditEntry

    [Fact]
    public void DSRAuditEntry_PropertiesAreSet()
    {
        var now = DateTimeOffset.UtcNow;
        var entry = new DSRAuditEntry
        {
            Id = "audit-1",
            DSRRequestId = "req-123",
            Action = "ErasureExecuted",
            Detail = "5 fields erased",
            PerformedByUserId = "admin-1",
            OccurredAtUtc = now
        };

        entry.Id.ShouldBe("audit-1");
        entry.DSRRequestId.ShouldBe("req-123");
        entry.Action.ShouldBe("ErasureExecuted");
        entry.Detail.ShouldBe("5 fields erased");
        entry.PerformedByUserId.ShouldBe("admin-1");
        entry.OccurredAtUtc.ShouldBe(now);
    }

    [Fact]
    public void DSRAuditEntry_OptionalFields_AreNullable()
    {
        var entry = new DSRAuditEntry
        {
            Id = "audit-1",
            DSRRequestId = "req-123",
            Action = "RequestReceived",
            Detail = null,
            PerformedByUserId = null,
            OccurredAtUtc = DateTimeOffset.UtcNow
        };

        entry.Detail.ShouldBeNull();
        entry.PerformedByUserId.ShouldBeNull();
    }

    #endregion

    #region Request Records

    [Fact]
    public void AccessRequest_PropertiesAreSet()
    {
        var request = new AccessRequest("subject-1", true);

        request.SubjectId.ShouldBe("subject-1");
        request.IncludeProcessingActivities.ShouldBeTrue();
    }

    [Fact]
    public void ErasureRequest_PropertiesAreSet()
    {
        var request = new ErasureRequest("subject-1", ErasureReason.ConsentWithdrawn, null);

        request.SubjectId.ShouldBe("subject-1");
        request.Reason.ShouldBe(ErasureReason.ConsentWithdrawn);
        request.Scope.ShouldBeNull();
    }

    [Fact]
    public void PortabilityRequest_PropertiesAreSet()
    {
        var request = new PortabilityRequest("subject-1", ExportFormat.JSON, null);

        request.SubjectId.ShouldBe("subject-1");
        request.Format.ShouldBe(ExportFormat.JSON);
        request.Categories.ShouldBeNull();
    }

    [Fact]
    public void RectificationRequest_PropertiesAreSet()
    {
        var request = new RectificationRequest("subject-1", "Email", "new@example.com", typeof(object), "entity-1");

        request.SubjectId.ShouldBe("subject-1");
        request.FieldName.ShouldBe("Email");
        request.NewValue.ShouldBe("new@example.com");
        request.EntityType.ShouldBe(typeof(object));
        request.EntityId.ShouldBe("entity-1");
    }

    [Fact]
    public void RestrictionRequest_PropertiesAreSet()
    {
        var request = new RestrictionRequest("subject-1", "Accuracy contested", null);

        request.SubjectId.ShouldBe("subject-1");
        request.Reason.ShouldBe("Accuracy contested");
        request.Scope.ShouldBeNull();
    }

    [Fact]
    public void ObjectionRequest_PropertiesAreSet()
    {
        var request = new ObjectionRequest("subject-1", "Marketing", "Personal reasons");

        request.SubjectId.ShouldBe("subject-1");
        request.ProcessingPurpose.ShouldBe("Marketing");
        request.Reason.ShouldBe("Personal reasons");
    }

    #endregion
}
