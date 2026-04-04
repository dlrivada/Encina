using System.Reflection;

using Encina.Compliance.Retention;

using FluentAssertions;

namespace Encina.UnitTests.Compliance.Retention;

/// <summary>
/// Extended unit tests for <see cref="RetentionErrors"/> verifying metadata/details dictionaries
/// and message content for all factory methods.
/// </summary>
public sealed class RetentionErrorsMetadataTests
{
    #region PolicyNotFound Metadata

    [Fact]
    public void PolicyNotFound_IncludesPolicyIdInDetails()
    {
        var error = RetentionErrors.PolicyNotFound("pol-42");
        var details = error.GetDetails();

        details.Should().NotBeNull();
        details.Should().ContainKey("policyId");
        details["policyId"].Should().Be("pol-42");
    }

    [Fact]
    public void PolicyNotFound_IncludesRetentionProcessingStage()
    {
        var error = RetentionErrors.PolicyNotFound("pol-1");
        var details = error.GetDetails();

        details.Should().ContainKey("retention_processing");
    }

    #endregion

    #region PolicyAlreadyExists Metadata

    [Fact]
    public void PolicyAlreadyExists_IncludesDataCategoryInDetails()
    {
        var error = RetentionErrors.PolicyAlreadyExists("audit-logs");
        var details = error.GetDetails();

        details.Should().NotBeNull();
        details.Should().ContainKey("dataCategory");
        details["dataCategory"].Should().Be("audit-logs");
    }

    #endregion

    #region RecordNotFound Metadata

    [Fact]
    public void RecordNotFound_IncludesRecordIdInDetails()
    {
        var error = RetentionErrors.RecordNotFound("rec-77");
        var details = error.GetDetails();

        details.Should().ContainKey("recordId");
        details["recordId"].Should().Be("rec-77");
    }

    #endregion

    #region RecordAlreadyExists Metadata

    [Fact]
    public void RecordAlreadyExists_IncludesRecordIdInDetails()
    {
        var error = RetentionErrors.RecordAlreadyExists("rec-99");
        var details = error.GetDetails();

        details.Should().ContainKey("recordId");
        details["recordId"].Should().Be("rec-99");
    }

    #endregion

    #region HoldNotFound Metadata

    [Fact]
    public void HoldNotFound_IncludesHoldIdInDetails()
    {
        var error = RetentionErrors.HoldNotFound("hold-55");
        var details = error.GetDetails();

        details.Should().ContainKey("holdId");
        details["holdId"].Should().Be("hold-55");
    }

    #endregion

    #region HoldAlreadyActive Metadata

    [Fact]
    public void HoldAlreadyActive_IncludesEntityIdAndRequirementInDetails()
    {
        var error = RetentionErrors.HoldAlreadyActive("ent-22");
        var details = error.GetDetails();

        details.Should().ContainKey("entityId");
        details["entityId"].Should().Be("ent-22");
        details.Should().ContainKey("requirement");
        details["requirement"].Should().Be("article_17_3_e_legal_claims");
    }

    #endregion

    #region HoldAlreadyReleased Metadata

    [Fact]
    public void HoldAlreadyReleased_IncludesHoldIdInDetails()
    {
        var error = RetentionErrors.HoldAlreadyReleased("hold-33");
        var details = error.GetDetails();

        details.Should().ContainKey("holdId");
        details["holdId"].Should().Be("hold-33");
    }

    #endregion

    #region EnforcementFailed Metadata

    [Fact]
    public void EnforcementFailed_IncludesRequirementInDetails()
    {
        var error = RetentionErrors.EnforcementFailed("timeout");
        var details = error.GetDetails();

        details.Should().ContainKey("requirement");
        details["requirement"].Should().Be("article_5_1_e_storage_limitation");
    }

    [Fact]
    public void EnforcementFailed_WithNullException_DoesNotThrow()
    {
        var error = RetentionErrors.EnforcementFailed("generic failure", null);
        error.Message.Should().Contain("generic failure");
    }

    #endregion

    #region DeletionFailed Metadata

    [Fact]
    public void DeletionFailed_IncludesEntityIdAndRequirementInDetails()
    {
        var error = RetentionErrors.DeletionFailed("ent-1", "FK constraint");
        var details = error.GetDetails();

        details.Should().ContainKey("entityId");
        details["entityId"].Should().Be("ent-1");
        details.Should().ContainKey("requirement");
        details["requirement"].Should().Be("article_17_1_a_no_longer_necessary");
    }

    #endregion

    #region StoreError Metadata

    [Fact]
    public void StoreError_IncludesOperationInDetails()
    {
        var error = RetentionErrors.StoreError("GetById", "Not found");
        var details = error.GetDetails();

        details.Should().ContainKey("operation");
        details["operation"].Should().Be("GetById");
    }

    [Fact]
    public void StoreError_MessageIncludesBothOperationAndReason()
    {
        var error = RetentionErrors.StoreError("UpdateStatus", "Concurrency conflict");
        error.Message.Should().Contain("UpdateStatus");
        error.Message.Should().Contain("Concurrency conflict");
    }

    #endregion

    #region InvalidParameter Metadata

    [Fact]
    public void InvalidParameter_IncludesParameterNameInDetails()
    {
        var error = RetentionErrors.InvalidParameter("duration", "Must be > 0");
        var details = error.GetDetails();

        details.Should().ContainKey("parameterName");
        details["parameterName"].Should().Be("duration");
    }

    [Fact]
    public void InvalidParameter_MessageIncludesBothNameAndReason()
    {
        var error = RetentionErrors.InvalidParameter("dataCategory", "Cannot be empty");
        error.Message.Should().Contain("dataCategory");
        error.Message.Should().Contain("Cannot be empty");
    }

    #endregion

    #region NoPolicyForCategory Metadata

    [Fact]
    public void NoPolicyForCategory_IncludesDataCategoryAndRequirementInDetails()
    {
        var error = RetentionErrors.NoPolicyForCategory("user-sessions");
        var details = error.GetDetails();

        details.Should().ContainKey("dataCategory");
        details["dataCategory"].Should().Be("user-sessions");
        details.Should().ContainKey("requirement");
        details["requirement"].Should().Be("article_5_1_e_storage_limitation");
    }

    [Fact]
    public void NoPolicyForCategory_MessageReferencesArticle5()
    {
        var error = RetentionErrors.NoPolicyForCategory("marketing");
        error.Message.Should().Contain("Article 5(1)(e)");
    }

    #endregion

    #region PipelineRecordCreationFailed Metadata

    [Fact]
    public void PipelineRecordCreationFailed_IncludesDataCategoryAndRequirementInDetails()
    {
        var error = RetentionErrors.PipelineRecordCreationFailed("invoices", "Store unavailable");
        var details = error.GetDetails();

        details.Should().ContainKey("dataCategory");
        details["dataCategory"].Should().Be("invoices");
        details.Should().ContainKey("requirement");
        details["requirement"].Should().Be("article_5_1_e_storage_limitation");
    }

    [Fact]
    public void PipelineRecordCreationFailed_WithException_HasValidCode()
    {
        var ex = new TimeoutException("DB timeout");
        var error = RetentionErrors.PipelineRecordCreationFailed("orders", "timeout", ex);

        error.GetCode().Match(
            Some: code => code.Should().Be(RetentionErrors.PipelineRecordCreationFailedCode),
            None: () => Assert.Fail("Expected code"));
    }

    #endregion

    #region InvalidStateTransition Metadata

    [Fact]
    public void InvalidStateTransition_IncludesAggregateIdAndOperationInDetails()
    {
        var id = Guid.NewGuid();
        var error = RetentionErrors.InvalidStateTransition(id, "Expire");
        var details = error.GetDetails();

        details.Should().ContainKey("aggregateId");
        details["aggregateId"].Should().Be(id.ToString());
        details.Should().ContainKey("operation");
        details["operation"].Should().Be("Expire");
    }

    #endregion

    #region ServiceError Metadata

    [Fact]
    public void ServiceError_IncludesOperationInDetails()
    {
        var error = RetentionErrors.ServiceError("GetRetentionPeriod");
        var details = error.GetDetails();

        details.Should().ContainKey("operation");
        details["operation"].Should().Be("GetRetentionPeriod");
    }

    [Fact]
    public void ServiceError_WithException_HasValidCode()
    {
        var ex = new InvalidOperationException("Unexpected");
        var error = RetentionErrors.ServiceError("TrackEntity", ex);

        error.GetCode().Match(
            Some: code => code.Should().Be(RetentionErrors.ServiceErrorCode),
            None: () => Assert.Fail("Expected code"));
    }

    #endregion

    #region EventHistoryUnavailable Metadata

    [Fact]
    public void EventHistoryUnavailable_IncludesAggregateIdInDetails()
    {
        var id = Guid.NewGuid();
        var error = RetentionErrors.EventHistoryUnavailable(id);
        var details = error.GetDetails();

        details.Should().ContainKey("aggregateId");
        details["aggregateId"].Should().Be(id.ToString());
    }

    [Fact]
    public void EventHistoryUnavailable_MessageReferencesPhase4()
    {
        var id = Guid.NewGuid();
        var error = RetentionErrors.EventHistoryUnavailable(id);
        error.Message.Should().Contain("Phase 4");
    }

    #endregion

    #region PipelineEntityIdNotFound Metadata

    [Fact]
    public void PipelineEntityIdNotFound_IncludesResponseTypeInDetails()
    {
        var error = RetentionErrors.PipelineEntityIdNotFound("CreateInvoiceResponse");
        var details = error.GetDetails();

        details.Should().ContainKey("responseType");
        details["responseType"].Should().Be("CreateInvoiceResponse");
    }

    [Fact]
    public void PipelineEntityIdNotFound_MessageSuggestsIdOrEntityId()
    {
        var error = RetentionErrors.PipelineEntityIdNotFound("MyResponse");
        error.Message.Should().Contain("Id");
        error.Message.Should().Contain("EntityId");
    }

    #endregion

    #region All Error Factory Methods - Non-Null Message

    [Fact]
    public void AllFactoryMethods_ReturnErrorsWithNonEmptyMessage()
    {
        var errors = new[]
        {
            RetentionErrors.PolicyNotFound("x"),
            RetentionErrors.PolicyAlreadyExists("x"),
            RetentionErrors.RecordNotFound("x"),
            RetentionErrors.RecordAlreadyExists("x"),
            RetentionErrors.HoldNotFound("x"),
            RetentionErrors.HoldAlreadyActive("x"),
            RetentionErrors.HoldAlreadyReleased("x"),
            RetentionErrors.EnforcementFailed("x"),
            RetentionErrors.DeletionFailed("x", "y"),
            RetentionErrors.StoreError("x", "y"),
            RetentionErrors.InvalidParameter("x", "y"),
            RetentionErrors.NoPolicyForCategory("x"),
            RetentionErrors.PipelineRecordCreationFailed("x", "y"),
            RetentionErrors.InvalidStateTransition(Guid.NewGuid(), "x"),
            RetentionErrors.ServiceError("x"),
            RetentionErrors.EventHistoryUnavailable(Guid.NewGuid()),
            RetentionErrors.PipelineEntityIdNotFound("x")
        };

        foreach (var error in errors)
        {
            error.Message.Should().NotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public void AllFactoryMethods_ReturnErrorsWithCode()
    {
        var errors = new[]
        {
            RetentionErrors.PolicyNotFound("x"),
            RetentionErrors.PolicyAlreadyExists("x"),
            RetentionErrors.RecordNotFound("x"),
            RetentionErrors.RecordAlreadyExists("x"),
            RetentionErrors.HoldNotFound("x"),
            RetentionErrors.HoldAlreadyActive("x"),
            RetentionErrors.HoldAlreadyReleased("x"),
            RetentionErrors.EnforcementFailed("x"),
            RetentionErrors.DeletionFailed("x", "y"),
            RetentionErrors.StoreError("x", "y"),
            RetentionErrors.InvalidParameter("x", "y"),
            RetentionErrors.NoPolicyForCategory("x"),
            RetentionErrors.PipelineRecordCreationFailed("x", "y"),
            RetentionErrors.InvalidStateTransition(Guid.NewGuid(), "x"),
            RetentionErrors.ServiceError("x"),
            RetentionErrors.EventHistoryUnavailable(Guid.NewGuid()),
            RetentionErrors.PipelineEntityIdNotFound("x")
        };

        foreach (var error in errors)
        {
            error.GetCode().IsSome.Should().BeTrue($"Error with message '{error.Message}' should have a code");
        }
    }

    #endregion
}
