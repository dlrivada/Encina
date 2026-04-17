#pragma warning disable CA2012

using Encina.Compliance.ProcessorAgreements;
using Shouldly;

namespace Encina.UnitTests.Compliance.ProcessorAgreements;

public class ProcessorAgreementErrorsTests
{
    #region NotFound

    [Fact]
    public void NotFound_ValidProcessorId_ReturnsErrorWithCorrectCodeAndMessage()
    {
        // Arrange
        var processorId = "proc-123";

        // Act
        var error = ProcessorAgreementErrors.NotFound(processorId);

        // Assert
        error.GetCode().IfNone("").ShouldBe(ProcessorAgreementErrors.NotFoundCode);
        error.Message.ShouldContain(processorId);
    }

    #endregion

    #region AlreadyExists

    [Fact]
    public void AlreadyExists_ValidProcessorId_ReturnsErrorWithCorrectCodeAndMessage()
    {
        // Arrange
        var processorId = "proc-456";

        // Act
        var error = ProcessorAgreementErrors.AlreadyExists(processorId);

        // Assert
        error.GetCode().IfNone("").ShouldBe(ProcessorAgreementErrors.AlreadyExistsCode);
        error.Message.ShouldContain(processorId);
    }

    #endregion

    #region DPANotFound

    [Fact]
    public void DPANotFound_ValidDpaId_ReturnsErrorWithCorrectCodeAndMessage()
    {
        // Arrange
        var dpaId = "dpa-789";

        // Act
        var error = ProcessorAgreementErrors.DPANotFound(dpaId);

        // Assert
        error.GetCode().IfNone("").ShouldBe(ProcessorAgreementErrors.DPANotFoundCode);
        error.Message.ShouldContain(dpaId);
    }

    #endregion

    #region DPAMissing

    [Fact]
    public void DPAMissing_ValidProcessorId_ReturnsErrorWithCorrectCodeAndMessage()
    {
        // Arrange
        var processorId = "proc-101";

        // Act
        var error = ProcessorAgreementErrors.DPAMissing(processorId);

        // Assert
        error.GetCode().IfNone("").ShouldBe(ProcessorAgreementErrors.DPAMissingCode);
        error.Message.ShouldContain(processorId);
    }

    #endregion

    #region DPAExpired

    [Fact]
    public void DPAExpired_ValidParameters_ReturnsErrorWithCorrectCodeAndMessage()
    {
        // Arrange
        var processorId = "proc-202";
        var dpaId = "dpa-303";
        var expiredAtUtc = new DateTimeOffset(2025, 6, 15, 0, 0, 0, TimeSpan.Zero);

        // Act
        var error = ProcessorAgreementErrors.DPAExpired(processorId, dpaId, expiredAtUtc);

        // Assert
        error.GetCode().IfNone("").ShouldBe(ProcessorAgreementErrors.DPAExpiredCode);
        error.Message.ShouldContain(processorId);
        error.Message.ShouldContain(dpaId);
    }

    #endregion

    #region DPATerminated

    [Fact]
    public void DPATerminated_ValidParameters_ReturnsErrorWithCorrectCodeAndMessage()
    {
        // Arrange
        var processorId = "proc-404";
        var dpaId = "dpa-505";

        // Act
        var error = ProcessorAgreementErrors.DPATerminated(processorId, dpaId);

        // Assert
        error.GetCode().IfNone("").ShouldBe(ProcessorAgreementErrors.DPATerminatedCode);
        error.Message.ShouldContain(processorId);
        error.Message.ShouldContain(dpaId);
    }

    #endregion

    #region DPAPendingRenewal

    [Fact]
    public void DPAPendingRenewal_ValidParameters_ReturnsErrorWithCorrectCodeAndMessage()
    {
        // Arrange
        var processorId = "proc-606";
        var dpaId = "dpa-707";

        // Act
        var error = ProcessorAgreementErrors.DPAPendingRenewal(processorId, dpaId);

        // Assert
        error.GetCode().IfNone("").ShouldBe(ProcessorAgreementErrors.DPAPendingRenewalCode);
        error.Message.ShouldContain(processorId);
        error.Message.ShouldContain(dpaId);
    }

    #endregion

    #region DPAIncomplete

    [Fact]
    public void DPAIncomplete_ValidParameters_ReturnsErrorWithCorrectCodeAndMessage()
    {
        // Arrange
        var processorId = "proc-808";
        var dpaId = "dpa-909";
        IReadOnlyList<string> missingTerms = ["data retention", "breach notification"];

        // Act
        var error = ProcessorAgreementErrors.DPAIncomplete(processorId, dpaId, missingTerms);

        // Assert
        error.GetCode().IfNone("").ShouldBe(ProcessorAgreementErrors.DPAIncompleteCode);
        error.Message.ShouldContain(processorId);
        error.Message.ShouldContain(dpaId);
    }

    #endregion

    #region SubProcessorUnauthorized

    [Fact]
    public void SubProcessorUnauthorized_ValidParameters_ReturnsErrorWithCorrectCodeAndMessage()
    {
        // Arrange
        var processorId = "proc-111";
        var subProcessorId = "sub-222";

        // Act
        var error = ProcessorAgreementErrors.SubProcessorUnauthorized(processorId, subProcessorId);

        // Assert
        error.GetCode().IfNone("").ShouldBe(ProcessorAgreementErrors.SubProcessorUnauthorizedCode);
        error.Message.ShouldContain(processorId);
        error.Message.ShouldContain(subProcessorId);
    }

    #endregion

    #region SubProcessorDepthExceeded

    [Fact]
    public void SubProcessorDepthExceeded_ValidParameters_ReturnsErrorWithCorrectCodeAndMessage()
    {
        // Arrange
        var processorId = "proc-333";
        var requestedDepth = 5;
        var maxDepth = 3;

        // Act
        var error = ProcessorAgreementErrors.SubProcessorDepthExceeded(processorId, requestedDepth, maxDepth);

        // Assert
        error.GetCode().IfNone("").ShouldBe(ProcessorAgreementErrors.SubProcessorDepthExceededCode);
        error.Message.ShouldContain(processorId);
    }

    #endregion

    #region SCCRequired

    [Fact]
    public void SCCRequired_ValidParameters_ReturnsErrorWithCorrectCodeAndMessage()
    {
        // Arrange
        var processorId = "proc-444";
        var country = "Brazil";

        // Act
        var error = ProcessorAgreementErrors.SCCRequired(processorId, country);

        // Assert
        error.GetCode().IfNone("").ShouldBe(ProcessorAgreementErrors.SCCRequiredCode);
        error.Message.ShouldContain(processorId);
        error.Message.ShouldContain(country);
    }

    #endregion

    #region StoreError

    [Fact]
    public void StoreError_WithoutException_ReturnsErrorWithCorrectCodeAndMessage()
    {
        // Arrange
        var operation = "SaveAsync";
        var message = "Connection refused";

        // Act
        var error = ProcessorAgreementErrors.StoreError(operation, message);

        // Assert
        error.GetCode().IfNone("").ShouldBe(ProcessorAgreementErrors.StoreErrorCode);
        error.Message.ShouldContain(operation);
        error.Message.ShouldContain(message);
    }

    [Fact]
    public void StoreError_WithException_ReturnsErrorWithCorrectCodeAndMessage()
    {
        // Arrange
        var operation = "DeleteAsync";
        var message = "Timeout expired";
        var exception = new InvalidOperationException("DB timeout");

        // Act
        var error = ProcessorAgreementErrors.StoreError(operation, message, exception);

        // Assert
        error.GetCode().IfNone("").ShouldBe(ProcessorAgreementErrors.StoreErrorCode);
        error.Message.ShouldContain(operation);
        error.Message.ShouldContain(message);
    }

    #endregion

    #region ValidationFailed

    [Fact]
    public void ValidationFailed_ValidParameters_ReturnsErrorWithCorrectCodeAndMessage()
    {
        // Arrange
        var processorId = "proc-555";
        var message = "Name cannot be empty";

        // Act
        var error = ProcessorAgreementErrors.ValidationFailed(processorId, message);

        // Assert
        error.GetCode().IfNone("").ShouldBe(ProcessorAgreementErrors.ValidationFailedCode);
        error.Message.ShouldContain(processorId);
        error.Message.ShouldContain(message);
    }

    #endregion

    #region ErrorCodeConstants

    [Theory]
    [InlineData(nameof(ProcessorAgreementErrors.NotFoundCode), "processor.not_found")]
    [InlineData(nameof(ProcessorAgreementErrors.AlreadyExistsCode), "processor.already_exists")]
    [InlineData(nameof(ProcessorAgreementErrors.DPANotFoundCode), "processor.dpa_not_found")]
    [InlineData(nameof(ProcessorAgreementErrors.DPAMissingCode), "processor.dpa_missing")]
    [InlineData(nameof(ProcessorAgreementErrors.DPAExpiredCode), "processor.dpa_expired")]
    [InlineData(nameof(ProcessorAgreementErrors.DPATerminatedCode), "processor.dpa_terminated")]
    [InlineData(nameof(ProcessorAgreementErrors.DPAPendingRenewalCode), "processor.dpa_pending_renewal")]
    [InlineData(nameof(ProcessorAgreementErrors.DPAIncompleteCode), "processor.dpa_incomplete")]
    [InlineData(nameof(ProcessorAgreementErrors.SubProcessorUnauthorizedCode), "processor.sub_processor_unauthorized")]
    [InlineData(nameof(ProcessorAgreementErrors.SubProcessorDepthExceededCode), "processor.sub_processor_depth_exceeded")]
    [InlineData(nameof(ProcessorAgreementErrors.SCCRequiredCode), "processor.scc_required")]
    [InlineData(nameof(ProcessorAgreementErrors.StoreErrorCode), "processor.store_error")]
    [InlineData(nameof(ProcessorAgreementErrors.ValidationFailedCode), "processor.validation_failed")]
    public void ErrorCodeConstant_ShouldHaveExpectedValue(string fieldName, string expectedValue)
    {
        // Act
        var field = typeof(ProcessorAgreementErrors).GetField(fieldName);
        var actualValue = field?.GetValue(null) as string;

        // Assert
        actualValue.ShouldBe(expectedValue);
    }

    #endregion
}
