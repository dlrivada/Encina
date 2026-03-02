using Encina.Compliance.Retention;
using FluentAssertions;

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
            Some: code => code.Should().Be(RetentionErrors.PolicyNotFoundCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public void PolicyNotFound_ShouldIncludePolicyIdInMessage()
    {
        var error = RetentionErrors.PolicyNotFound("policy-001");
        error.Message.Should().Contain("policy-001");
    }

    #endregion

    #region PolicyAlreadyExists Tests

    [Fact]
    public void PolicyAlreadyExists_ShouldReturnCorrectCode()
    {
        var error = RetentionErrors.PolicyAlreadyExists("financial-records");
        error.GetCode().Match(
            Some: code => code.Should().Be(RetentionErrors.PolicyAlreadyExistsCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public void PolicyAlreadyExists_ShouldIncludeDataCategoryInMessage()
    {
        var error = RetentionErrors.PolicyAlreadyExists("financial-records");
        error.Message.Should().Contain("financial-records");
    }

    #endregion

    #region RecordNotFound Tests

    [Fact]
    public void RecordNotFound_ShouldReturnCorrectCode()
    {
        var error = RetentionErrors.RecordNotFound("record-001");
        error.GetCode().Match(
            Some: code => code.Should().Be(RetentionErrors.RecordNotFoundCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public void RecordNotFound_ShouldIncludeRecordIdInMessage()
    {
        var error = RetentionErrors.RecordNotFound("record-001");
        error.Message.Should().Contain("record-001");
    }

    #endregion

    #region RecordAlreadyExists Tests

    [Fact]
    public void RecordAlreadyExists_ShouldReturnCorrectCode()
    {
        var error = RetentionErrors.RecordAlreadyExists("record-002");
        error.GetCode().Match(
            Some: code => code.Should().Be(RetentionErrors.RecordAlreadyExistsCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public void RecordAlreadyExists_ShouldIncludeRecordIdInMessage()
    {
        var error = RetentionErrors.RecordAlreadyExists("record-002");
        error.Message.Should().Contain("record-002");
    }

    #endregion

    #region HoldNotFound Tests

    [Fact]
    public void HoldNotFound_ShouldReturnCorrectCode()
    {
        var error = RetentionErrors.HoldNotFound("hold-001");
        error.GetCode().Match(
            Some: code => code.Should().Be(RetentionErrors.HoldNotFoundCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public void HoldNotFound_ShouldIncludeHoldIdInMessage()
    {
        var error = RetentionErrors.HoldNotFound("hold-001");
        error.Message.Should().Contain("hold-001");
    }

    #endregion

    #region HoldAlreadyActive Tests

    [Fact]
    public void HoldAlreadyActive_ShouldReturnCorrectCode()
    {
        var error = RetentionErrors.HoldAlreadyActive("entity-123");
        error.GetCode().Match(
            Some: code => code.Should().Be(RetentionErrors.HoldAlreadyActiveCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public void HoldAlreadyActive_ShouldIncludeEntityIdInMessage()
    {
        var error = RetentionErrors.HoldAlreadyActive("entity-123");
        error.Message.Should().Contain("entity-123");
    }

    #endregion

    #region HoldAlreadyReleased Tests

    [Fact]
    public void HoldAlreadyReleased_ShouldReturnCorrectCode()
    {
        var error = RetentionErrors.HoldAlreadyReleased("hold-002");
        error.GetCode().Match(
            Some: code => code.Should().Be(RetentionErrors.HoldAlreadyReleasedCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public void HoldAlreadyReleased_ShouldIncludeHoldIdInMessage()
    {
        var error = RetentionErrors.HoldAlreadyReleased("hold-002");
        error.Message.Should().Contain("hold-002");
    }

    #endregion

    #region EnforcementFailed Tests

    [Fact]
    public void EnforcementFailed_ShouldReturnCorrectCode()
    {
        var error = RetentionErrors.EnforcementFailed("Store unreachable");
        error.GetCode().Match(
            Some: code => code.Should().Be(RetentionErrors.EnforcementFailedCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public void EnforcementFailed_ShouldIncludeReasonInMessage()
    {
        var error = RetentionErrors.EnforcementFailed("Store unreachable");
        error.Message.Should().Contain("Store unreachable");
    }

    [Fact]
    public void EnforcementFailed_WithException_ShouldReturnCorrectCode()
    {
        var innerException = new InvalidOperationException("Connection timeout");
        var error = RetentionErrors.EnforcementFailed("Store unreachable", innerException);
        error.GetCode().Match(
            Some: code => code.Should().Be(RetentionErrors.EnforcementFailedCode),
            None: () => Assert.Fail("Expected error code"));
    }

    #endregion

    #region DeletionFailed Tests

    [Fact]
    public void DeletionFailed_ShouldReturnCorrectCode()
    {
        var error = RetentionErrors.DeletionFailed("entity-456", "Foreign key constraint");
        error.GetCode().Match(
            Some: code => code.Should().Be(RetentionErrors.DeletionFailedCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public void DeletionFailed_ShouldIncludeEntityIdInMessage()
    {
        var error = RetentionErrors.DeletionFailed("entity-456", "Foreign key constraint");
        error.Message.Should().Contain("entity-456");
    }

    [Fact]
    public void DeletionFailed_ShouldIncludeReasonInMessage()
    {
        var error = RetentionErrors.DeletionFailed("entity-456", "Foreign key constraint");
        error.Message.Should().Contain("Foreign key constraint");
    }

    #endregion

    #region StoreError Tests

    [Fact]
    public void StoreError_ShouldReturnCorrectCode()
    {
        var error = RetentionErrors.StoreError("Create", "Connection timeout");
        error.GetCode().Match(
            Some: code => code.Should().Be(RetentionErrors.StoreErrorCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public void StoreError_ShouldIncludeOperationInMessage()
    {
        var error = RetentionErrors.StoreError("Create", "Connection timeout");
        error.Message.Should().Contain("Create");
    }

    [Fact]
    public void StoreError_WithException_ShouldReturnCorrectCode()
    {
        var innerException = new InvalidOperationException("Deadlock");
        var error = RetentionErrors.StoreError("Create", "Connection timeout", innerException);
        error.GetCode().Match(
            Some: code => code.Should().Be(RetentionErrors.StoreErrorCode),
            None: () => Assert.Fail("Expected error code"));
    }

    #endregion

    #region InvalidParameter Tests

    [Fact]
    public void InvalidParameter_ShouldReturnCorrectCode()
    {
        var error = RetentionErrors.InvalidParameter("retentionPeriod", "Must be greater than zero");
        error.GetCode().Match(
            Some: code => code.Should().Be(RetentionErrors.InvalidParameterCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public void InvalidParameter_ShouldIncludeParameterNameInMessage()
    {
        var error = RetentionErrors.InvalidParameter("retentionPeriod", "Must be greater than zero");
        error.Message.Should().Contain("retentionPeriod");
    }

    #endregion

    #region NoPolicyForCategory Tests

    [Fact]
    public void NoPolicyForCategory_ShouldReturnCorrectCode()
    {
        var error = RetentionErrors.NoPolicyForCategory("session-logs");
        error.GetCode().Match(
            Some: code => code.Should().Be(RetentionErrors.NoPolicyForCategoryCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public void NoPolicyForCategory_ShouldIncludeDataCategoryInMessage()
    {
        var error = RetentionErrors.NoPolicyForCategory("session-logs");
        error.Message.Should().Contain("session-logs");
    }

    #endregion

    #region PipelineRecordCreationFailed Tests

    [Fact]
    public void PipelineRecordCreationFailed_ShouldReturnCorrectCode()
    {
        var error = RetentionErrors.PipelineRecordCreationFailed("marketing-consent", "Store write failed");
        error.GetCode().Match(
            Some: code => code.Should().Be(RetentionErrors.PipelineRecordCreationFailedCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public void PipelineRecordCreationFailed_ShouldIncludeDataCategoryInMessage()
    {
        var error = RetentionErrors.PipelineRecordCreationFailed("marketing-consent", "Store write failed");
        error.Message.Should().Contain("marketing-consent");
    }

    [Fact]
    public void PipelineRecordCreationFailed_WithException_ShouldReturnCorrectCode()
    {
        var innerException = new InvalidOperationException("Database unavailable");
        var error = RetentionErrors.PipelineRecordCreationFailed("marketing-consent", "Store write failed", innerException);
        error.GetCode().Match(
            Some: code => code.Should().Be(RetentionErrors.PipelineRecordCreationFailedCode),
            None: () => Assert.Fail("Expected error code"));
    }

    #endregion

    #region PipelineEntityIdNotFound Tests

    [Fact]
    public void PipelineEntityIdNotFound_ShouldReturnCorrectCode()
    {
        var error = RetentionErrors.PipelineEntityIdNotFound("CreateOrderResponse");
        error.GetCode().Match(
            Some: code => code.Should().Be(RetentionErrors.PipelineEntityIdNotFoundCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public void PipelineEntityIdNotFound_ShouldIncludeResponseTypeInMessage()
    {
        var error = RetentionErrors.PipelineEntityIdNotFound("CreateOrderResponse");
        error.Message.Should().Contain("CreateOrderResponse");
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
    public void ErrorCodeConstant_ShouldHaveCorrectValue(string constantName, string expectedValue)
    {
        var actualValue = typeof(RetentionErrors)
            .GetField(constantName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)!
            .GetValue(null) as string;

        actualValue.Should().Be(expectedValue);
    }

    [Fact]
    public void ErrorCodes_ShouldFollowRetentionConvention()
    {
        RetentionErrors.PolicyNotFoundCode.Should().StartWith("retention.");
        RetentionErrors.PolicyAlreadyExistsCode.Should().StartWith("retention.");
        RetentionErrors.RecordNotFoundCode.Should().StartWith("retention.");
        RetentionErrors.RecordAlreadyExistsCode.Should().StartWith("retention.");
        RetentionErrors.HoldNotFoundCode.Should().StartWith("retention.");
        RetentionErrors.HoldAlreadyActiveCode.Should().StartWith("retention.");
        RetentionErrors.HoldAlreadyReleasedCode.Should().StartWith("retention.");
        RetentionErrors.EnforcementFailedCode.Should().StartWith("retention.");
        RetentionErrors.DeletionFailedCode.Should().StartWith("retention.");
        RetentionErrors.StoreErrorCode.Should().StartWith("retention.");
        RetentionErrors.InvalidParameterCode.Should().StartWith("retention.");
        RetentionErrors.NoPolicyForCategoryCode.Should().StartWith("retention.");
        RetentionErrors.PipelineRecordCreationFailedCode.Should().StartWith("retention.");
        RetentionErrors.PipelineEntityIdNotFoundCode.Should().StartWith("retention.");
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
            RetentionErrors.PipelineEntityIdNotFoundCode
        };

        codes.Should().OnlyHaveUniqueItems();
    }

    #endregion
}
