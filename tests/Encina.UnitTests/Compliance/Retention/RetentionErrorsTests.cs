using Encina.Compliance.Retention;
using Shouldly;

namespace Encina.UnitTests.Compliance.Retention;

/// <summary>
/// Unit tests for <see cref="RetentionErrors"/> factory methods and error code constants.
/// </summary>
public class RetentionErrorsTests
{
    #region PolicyNotFound Tests

    [Fact]
    public void PolicyNotFound_ShouldReturnCorrectCode()
    {
        var error = RetentionErrors.PolicyNotFound("policy-001");
        error.GetCode().Match(
            Some: code => code.ShouldBe(RetentionErrors.PolicyNotFoundCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public void PolicyNotFound_ShouldIncludePolicyIdInMessage()
    {
        var error = RetentionErrors.PolicyNotFound("policy-001");
        error.Message.ShouldContain("policy-001");
    }

    #endregion

    #region PolicyAlreadyExists Tests

    [Fact]
    public void PolicyAlreadyExists_ShouldReturnCorrectCode()
    {
        var error = RetentionErrors.PolicyAlreadyExists("financial-records");
        error.GetCode().Match(
            Some: code => code.ShouldBe(RetentionErrors.PolicyAlreadyExistsCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public void PolicyAlreadyExists_ShouldIncludeDataCategoryInMessage()
    {
        var error = RetentionErrors.PolicyAlreadyExists("financial-records");
        error.Message.ShouldContain("financial-records");
    }

    #endregion

    #region RecordNotFound Tests

    [Fact]
    public void RecordNotFound_ShouldReturnCorrectCode()
    {
        var error = RetentionErrors.RecordNotFound("record-001");
        error.GetCode().Match(
            Some: code => code.ShouldBe(RetentionErrors.RecordNotFoundCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public void RecordNotFound_ShouldIncludeRecordIdInMessage()
    {
        var error = RetentionErrors.RecordNotFound("record-001");
        error.Message.ShouldContain("record-001");
    }

    #endregion

    #region RecordAlreadyExists Tests

    [Fact]
    public void RecordAlreadyExists_ShouldReturnCorrectCode()
    {
        var error = RetentionErrors.RecordAlreadyExists("record-002");
        error.GetCode().Match(
            Some: code => code.ShouldBe(RetentionErrors.RecordAlreadyExistsCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public void RecordAlreadyExists_ShouldIncludeRecordIdInMessage()
    {
        var error = RetentionErrors.RecordAlreadyExists("record-002");
        error.Message.ShouldContain("record-002");
    }

    #endregion

    #region HoldNotFound Tests

    [Fact]
    public void HoldNotFound_ShouldReturnCorrectCode()
    {
        var error = RetentionErrors.HoldNotFound("hold-001");
        error.GetCode().Match(
            Some: code => code.ShouldBe(RetentionErrors.HoldNotFoundCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public void HoldNotFound_ShouldIncludeHoldIdInMessage()
    {
        var error = RetentionErrors.HoldNotFound("hold-001");
        error.Message.ShouldContain("hold-001");
    }

    #endregion

    #region HoldAlreadyActive Tests

    [Fact]
    public void HoldAlreadyActive_ShouldReturnCorrectCode()
    {
        var error = RetentionErrors.HoldAlreadyActive("entity-123");
        error.GetCode().Match(
            Some: code => code.ShouldBe(RetentionErrors.HoldAlreadyActiveCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public void HoldAlreadyActive_ShouldIncludeEntityIdInMessage()
    {
        var error = RetentionErrors.HoldAlreadyActive("entity-123");
        error.Message.ShouldContain("entity-123");
    }

    #endregion

    #region HoldAlreadyReleased Tests

    [Fact]
    public void HoldAlreadyReleased_ShouldReturnCorrectCode()
    {
        var error = RetentionErrors.HoldAlreadyReleased("hold-002");
        error.GetCode().Match(
            Some: code => code.ShouldBe(RetentionErrors.HoldAlreadyReleasedCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public void HoldAlreadyReleased_ShouldIncludeHoldIdInMessage()
    {
        var error = RetentionErrors.HoldAlreadyReleased("hold-002");
        error.Message.ShouldContain("hold-002");
    }

    #endregion

    #region EnforcementFailed Tests

    [Fact]
    public void EnforcementFailed_ShouldReturnCorrectCode()
    {
        var error = RetentionErrors.EnforcementFailed("Store unreachable");
        error.GetCode().Match(
            Some: code => code.ShouldBe(RetentionErrors.EnforcementFailedCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public void EnforcementFailed_ShouldIncludeReasonInMessage()
    {
        var error = RetentionErrors.EnforcementFailed("Store unreachable");
        error.Message.ShouldContain("Store unreachable");
    }

    [Fact]
    public void EnforcementFailed_WithException_ShouldReturnCorrectCode()
    {
        var innerException = new InvalidOperationException("Connection timeout");
        var error = RetentionErrors.EnforcementFailed("Store unreachable", innerException);
        error.GetCode().Match(
            Some: code => code.ShouldBe(RetentionErrors.EnforcementFailedCode),
            None: () => Assert.Fail("Expected error code"));
    }

    #endregion

    #region DeletionFailed Tests

    [Fact]
    public void DeletionFailed_ShouldReturnCorrectCode()
    {
        var error = RetentionErrors.DeletionFailed("entity-456", "Foreign key constraint");
        error.GetCode().Match(
            Some: code => code.ShouldBe(RetentionErrors.DeletionFailedCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public void DeletionFailed_ShouldIncludeEntityIdInMessage()
    {
        var error = RetentionErrors.DeletionFailed("entity-456", "Foreign key constraint");
        error.Message.ShouldContain("entity-456");
    }

    [Fact]
    public void DeletionFailed_ShouldIncludeReasonInMessage()
    {
        var error = RetentionErrors.DeletionFailed("entity-456", "Foreign key constraint");
        error.Message.ShouldContain("Foreign key constraint");
    }

    #endregion

    #region StoreError Tests

    [Fact]
    public void StoreError_ShouldReturnCorrectCode()
    {
        var error = RetentionErrors.StoreError("Create", "Connection timeout");
        error.GetCode().Match(
            Some: code => code.ShouldBe(RetentionErrors.StoreErrorCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public void StoreError_ShouldIncludeOperationInMessage()
    {
        var error = RetentionErrors.StoreError("Create", "Connection timeout");
        error.Message.ShouldContain("Create");
    }

    [Fact]
    public void StoreError_WithException_ShouldReturnCorrectCode()
    {
        var innerException = new InvalidOperationException("Deadlock");
        var error = RetentionErrors.StoreError("Create", "Connection timeout", innerException);
        error.GetCode().Match(
            Some: code => code.ShouldBe(RetentionErrors.StoreErrorCode),
            None: () => Assert.Fail("Expected error code"));
    }

    #endregion

    #region InvalidParameter Tests

    [Fact]
    public void InvalidParameter_ShouldReturnCorrectCode()
    {
        var error = RetentionErrors.InvalidParameter("retentionPeriod", "Must be greater than zero");
        error.GetCode().Match(
            Some: code => code.ShouldBe(RetentionErrors.InvalidParameterCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public void InvalidParameter_ShouldIncludeParameterNameInMessage()
    {
        var error = RetentionErrors.InvalidParameter("retentionPeriod", "Must be greater than zero");
        error.Message.ShouldContain("retentionPeriod");
    }

    #endregion

    #region NoPolicyForCategory Tests

    [Fact]
    public void NoPolicyForCategory_ShouldReturnCorrectCode()
    {
        var error = RetentionErrors.NoPolicyForCategory("session-logs");
        error.GetCode().Match(
            Some: code => code.ShouldBe(RetentionErrors.NoPolicyForCategoryCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public void NoPolicyForCategory_ShouldIncludeDataCategoryInMessage()
    {
        var error = RetentionErrors.NoPolicyForCategory("session-logs");
        error.Message.ShouldContain("session-logs");
    }

    #endregion

    #region PipelineRecordCreationFailed Tests

    [Fact]
    public void PipelineRecordCreationFailed_ShouldReturnCorrectCode()
    {
        var error = RetentionErrors.PipelineRecordCreationFailed("marketing-consent", "Store write failed");
        error.GetCode().Match(
            Some: code => code.ShouldBe(RetentionErrors.PipelineRecordCreationFailedCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public void PipelineRecordCreationFailed_ShouldIncludeDataCategoryInMessage()
    {
        var error = RetentionErrors.PipelineRecordCreationFailed("marketing-consent", "Store write failed");
        error.Message.ShouldContain("marketing-consent");
    }

    [Fact]
    public void PipelineRecordCreationFailed_WithException_ShouldReturnCorrectCode()
    {
        var innerException = new InvalidOperationException("Database unavailable");
        var error = RetentionErrors.PipelineRecordCreationFailed("marketing-consent", "Store write failed", innerException);
        error.GetCode().Match(
            Some: code => code.ShouldBe(RetentionErrors.PipelineRecordCreationFailedCode),
            None: () => Assert.Fail("Expected error code"));
    }

    #endregion

    #region PipelineEntityIdNotFound Tests

    [Fact]
    public void PipelineEntityIdNotFound_ShouldReturnCorrectCode()
    {
        var error = RetentionErrors.PipelineEntityIdNotFound("CreateOrderResponse");
        error.GetCode().Match(
            Some: code => code.ShouldBe(RetentionErrors.PipelineEntityIdNotFoundCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public void PipelineEntityIdNotFound_ShouldIncludeResponseTypeInMessage()
    {
        var error = RetentionErrors.PipelineEntityIdNotFound("CreateOrderResponse");
        error.Message.ShouldContain("CreateOrderResponse");
    }

    #endregion

    #region InvalidStateTransition Tests

    [Fact]
    public void InvalidStateTransition_ShouldReturnCorrectCode()
    {
        var aggregateId = Guid.NewGuid();
        var error = RetentionErrors.InvalidStateTransition(aggregateId, "Deactivate");
        error.GetCode().Match(
            Some: code => code.ShouldBe(RetentionErrors.InvalidStateTransitionCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public void InvalidStateTransition_ShouldIncludeAggregateIdInMessage()
    {
        var aggregateId = Guid.NewGuid();
        var error = RetentionErrors.InvalidStateTransition(aggregateId, "Deactivate");
        error.Message.ShouldContain(aggregateId.ToString());
    }

    [Fact]
    public void InvalidStateTransition_ShouldIncludeOperationInMessage()
    {
        var aggregateId = Guid.NewGuid();
        var error = RetentionErrors.InvalidStateTransition(aggregateId, "Deactivate");
        error.Message.ShouldContain("Deactivate");
    }

    #endregion

    #region ServiceError Tests

    [Fact]
    public void ServiceError_ShouldReturnCorrectCode()
    {
        var error = RetentionErrors.ServiceError("CreatePolicy");
        error.GetCode().Match(
            Some: code => code.ShouldBe(RetentionErrors.ServiceErrorCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public void ServiceError_ShouldIncludeOperationInMessage()
    {
        var error = RetentionErrors.ServiceError("CreatePolicy");
        error.Message.ShouldContain("CreatePolicy");
    }

    [Fact]
    public void ServiceError_WithException_ShouldReturnCorrectCode()
    {
        var innerException = new InvalidOperationException("Unexpected failure");
        var error = RetentionErrors.ServiceError("CreatePolicy", innerException);
        error.GetCode().Match(
            Some: code => code.ShouldBe(RetentionErrors.ServiceErrorCode),
            None: () => Assert.Fail("Expected error code"));
    }

    #endregion

    #region EventHistoryUnavailable Tests

    [Fact]
    public void EventHistoryUnavailable_ShouldReturnCorrectCode()
    {
        var aggregateId = Guid.NewGuid();
        var error = RetentionErrors.EventHistoryUnavailable(aggregateId);
        error.GetCode().Match(
            Some: code => code.ShouldBe(RetentionErrors.EventHistoryUnavailableCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public void EventHistoryUnavailable_ShouldIncludeAggregateIdInMessage()
    {
        var aggregateId = Guid.NewGuid();
        var error = RetentionErrors.EventHistoryUnavailable(aggregateId);
        error.Message.ShouldContain(aggregateId.ToString());
    }

    #endregion

    #region Error Code Constants Tests

    [Theory]
    [InlineData(nameof(RetentionErrors.PolicyNotFoundCode), "retention.policy_not_found")]
    [InlineData(nameof(RetentionErrors.PolicyAlreadyExistsCode), "retention.policy_already_exists")]
    [InlineData(nameof(RetentionErrors.RecordNotFoundCode), "retention.record_not_found")]
    [InlineData(nameof(RetentionErrors.RecordAlreadyExistsCode), "retention.record_already_exists")]
    [InlineData(nameof(RetentionErrors.HoldNotFoundCode), "retention.hold_not_found")]
    [InlineData(nameof(RetentionErrors.HoldAlreadyActiveCode), "retention.hold_already_active")]
    [InlineData(nameof(RetentionErrors.HoldAlreadyReleasedCode), "retention.hold_already_released")]
    [InlineData(nameof(RetentionErrors.EnforcementFailedCode), "retention.enforcement_failed")]
    [InlineData(nameof(RetentionErrors.DeletionFailedCode), "retention.deletion_failed")]
    [InlineData(nameof(RetentionErrors.StoreErrorCode), "retention.store_error")]
    [InlineData(nameof(RetentionErrors.InvalidParameterCode), "retention.invalid_parameter")]
    [InlineData(nameof(RetentionErrors.NoPolicyForCategoryCode), "retention.no_policy_for_category")]
    [InlineData(nameof(RetentionErrors.PipelineRecordCreationFailedCode), "retention.pipeline_record_creation_failed")]
    [InlineData(nameof(RetentionErrors.PipelineEntityIdNotFoundCode), "retention.pipeline_entity_id_not_found")]
    [InlineData(nameof(RetentionErrors.InvalidStateTransitionCode), "retention.invalid_state_transition")]
    [InlineData(nameof(RetentionErrors.ServiceErrorCode), "retention.service_error")]
    [InlineData(nameof(RetentionErrors.EventHistoryUnavailableCode), "retention.event_history_unavailable")]
    public void ErrorCodeConstant_ShouldHaveCorrectValue(string constantName, string expectedValue)
    {
        var actualValue = typeof(RetentionErrors)
            .GetField(constantName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)!
            .GetValue(null) as string;

        actualValue.ShouldBe(expectedValue);
    }

    [Fact]
    public void ErrorCodes_ShouldFollowRetentionConvention()
    {
        RetentionErrors.PolicyNotFoundCode.ShouldStartWith("retention.");
        RetentionErrors.PolicyAlreadyExistsCode.ShouldStartWith("retention.");
        RetentionErrors.RecordNotFoundCode.ShouldStartWith("retention.");
        RetentionErrors.RecordAlreadyExistsCode.ShouldStartWith("retention.");
        RetentionErrors.HoldNotFoundCode.ShouldStartWith("retention.");
        RetentionErrors.HoldAlreadyActiveCode.ShouldStartWith("retention.");
        RetentionErrors.HoldAlreadyReleasedCode.ShouldStartWith("retention.");
        RetentionErrors.EnforcementFailedCode.ShouldStartWith("retention.");
        RetentionErrors.DeletionFailedCode.ShouldStartWith("retention.");
        RetentionErrors.StoreErrorCode.ShouldStartWith("retention.");
        RetentionErrors.InvalidParameterCode.ShouldStartWith("retention.");
        RetentionErrors.NoPolicyForCategoryCode.ShouldStartWith("retention.");
        RetentionErrors.PipelineRecordCreationFailedCode.ShouldStartWith("retention.");
        RetentionErrors.PipelineEntityIdNotFoundCode.ShouldStartWith("retention.");
        RetentionErrors.InvalidStateTransitionCode.ShouldStartWith("retention.");
        RetentionErrors.ServiceErrorCode.ShouldStartWith("retention.");
        RetentionErrors.EventHistoryUnavailableCode.ShouldStartWith("retention.");
    }

    [Fact]
    public void ErrorCodes_ShouldAllBeUnique()
    {
        var codes = new[]
        {
            RetentionErrors.PolicyNotFoundCode,
            RetentionErrors.PolicyAlreadyExistsCode,
            RetentionErrors.RecordNotFoundCode,
            RetentionErrors.RecordAlreadyExistsCode,
            RetentionErrors.HoldNotFoundCode,
            RetentionErrors.HoldAlreadyActiveCode,
            RetentionErrors.HoldAlreadyReleasedCode,
            RetentionErrors.EnforcementFailedCode,
            RetentionErrors.DeletionFailedCode,
            RetentionErrors.StoreErrorCode,
            RetentionErrors.InvalidParameterCode,
            RetentionErrors.NoPolicyForCategoryCode,
            RetentionErrors.PipelineRecordCreationFailedCode,
            RetentionErrors.PipelineEntityIdNotFoundCode,
            RetentionErrors.InvalidStateTransitionCode,
            RetentionErrors.ServiceErrorCode,
            RetentionErrors.EventHistoryUnavailableCode
        };

        codes.Distinct().Count().ShouldBe(codes.Count);
    }

    #endregion
}
