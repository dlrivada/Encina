using System.Diagnostics;
using Encina.Security.PII.Diagnostics;

namespace Encina.UnitTests.Security.PII.Diagnostics;

public sealed class PIIDiagnosticsTests
{
    [Fact]
    public void ActivitySource_HasCorrectName()
    {
        PIIDiagnostics.SourceName.ShouldBe("Encina.Security.PII");
    }

    [Fact]
    public void ActivitySource_HasCorrectVersion()
    {
        PIIDiagnostics.SourceVersion.ShouldBe("1.0");
    }

    [Fact]
    public void ActivitySource_Instance_HasCorrectName()
    {
        PIIDiagnostics.ActivitySource.Name.ShouldBe("Encina.Security.PII");
    }

    [Fact]
    public void Meter_HasCorrectName()
    {
        PIIDiagnostics.Meter.Name.ShouldBe("Encina.Security.PII");
    }

    [Fact]
    public void OperationsTotal_IsNotNull()
    {
        PIIDiagnostics.OperationsTotal.ShouldNotBeNull();
        PIIDiagnostics.OperationsTotal.Name.ShouldBe("pii.masking.operations");
    }

    [Fact]
    public void PropertiesMasked_IsNotNull()
    {
        PIIDiagnostics.PropertiesMasked.ShouldNotBeNull();
        PIIDiagnostics.PropertiesMasked.Name.ShouldBe("pii.masking.properties");
    }

    [Fact]
    public void ErrorsTotal_IsNotNull()
    {
        PIIDiagnostics.ErrorsTotal.ShouldNotBeNull();
        PIIDiagnostics.ErrorsTotal.Name.ShouldBe("pii.masking.errors");
    }

    [Fact]
    public void OperationDuration_IsNotNull()
    {
        PIIDiagnostics.OperationDuration.ShouldNotBeNull();
        PIIDiagnostics.OperationDuration.Name.ShouldBe("pii.masking.duration");
    }

    [Fact]
    public void PipelineOperationsTotal_IsNotNull()
    {
        PIIDiagnostics.PipelineOperationsTotal.ShouldNotBeNull();
        PIIDiagnostics.PipelineOperationsTotal.Name.ShouldBe("pii.pipeline.operations");
    }

    [Fact]
    public void TagConstants_HaveCorrectValues()
    {
        PIIDiagnostics.TagTypeName.ShouldBe("pii.type_name");
        PIIDiagnostics.TagPropertyCount.ShouldBe("pii.property_count");
        PIIDiagnostics.TagMaskedCount.ShouldBe("pii.masked_count");
        PIIDiagnostics.TagPiiType.ShouldBe("pii.pii_type");
        PIIDiagnostics.TagMaskingMode.ShouldBe("pii.masking_mode");
        PIIDiagnostics.TagPropertyName.ShouldBe("pii.property_name");
        PIIDiagnostics.TagStrategy.ShouldBe("pii.strategy");
        PIIDiagnostics.TagOutcome.ShouldBe("pii.outcome");
        PIIDiagnostics.TagErrorType.ShouldBe("pii.error_type");
    }

    [Fact]
    public void ActivityNameConstants_HaveCorrectValues()
    {
        PIIDiagnostics.ActivityMaskObject.ShouldBe("PII.MaskObject");
        PIIDiagnostics.ActivityMaskProperty.ShouldBe("PII.MaskProperty");
        PIIDiagnostics.ActivityApplyStrategy.ShouldBe("PII.ApplyStrategy");
    }

    [Fact]
    public void StartMaskObject_WithoutListeners_ReturnsNull()
    {
        var activity = PIIDiagnostics.StartMaskObject("TestType");

        activity.ShouldBeNull(); // No listeners registered
    }

    [Fact]
    public void StartApplyStrategy_WithoutListeners_ReturnsNull()
    {
        var activity = PIIDiagnostics.StartApplyStrategy("Email", "Email");

        activity.ShouldBeNull(); // No listeners registered
    }

    [Fact]
    public void RecordSuccess_WithNullActivity_DoesNotThrow()
    {
        // Act & Assert - should not throw
        var exception = Record.Exception(() => PIIDiagnostics.RecordSuccess(null, 5));

        exception.ShouldBeNull();
    }

    [Fact]
    public void RecordFailure_WithNullActivity_DoesNotThrow()
    {
        // Act & Assert - should not throw
        var exception = Record.Exception(() => PIIDiagnostics.RecordFailure(null, new InvalidOperationException("test")));

        exception.ShouldBeNull();
    }

    [Fact]
    public void RecordOperationMetrics_DoesNotThrow()
    {
        // Act & Assert - should not throw even when no listeners are configured
        var exception = Record.Exception(() =>
            PIIDiagnostics.RecordOperationMetrics("TestType", "Partial", success: true, maskedCount: 3, elapsedMs: 1.5));

        exception.ShouldBeNull();
    }

    [Fact]
    public void RecordErrorMetric_DoesNotThrow()
    {
        // Act & Assert - should not throw even when no listeners are configured
        var exception = Record.Exception(() =>
            PIIDiagnostics.RecordErrorMetric("InvalidOperationException"));

        exception.ShouldBeNull();
    }

    [Fact]
    public void RecordPipelineMetrics_DoesNotThrow()
    {
        // Act & Assert - should not throw even when no listeners are configured
        var exception = Record.Exception(() =>
            PIIDiagnostics.RecordPipelineMetrics("TestResponse", "success"));

        exception.ShouldBeNull();
    }
}
