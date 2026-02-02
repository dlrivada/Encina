using Encina.OpenTelemetry;

namespace Encina.UnitTests.OpenTelemetry;

/// <summary>
/// Unit tests for <see cref="global::Encina.OpenTelemetry.ActivityTagNames.EventMetadata"/> constants.
/// </summary>
public sealed class ActivityTagNamesEventMetadataTests
{
    [Fact]
    public void MessageId_HasCorrectValue()
    {
        global::Encina.OpenTelemetry.ActivityTagNames.EventMetadata.MessageId.ShouldBe("event.message_id");
    }

    [Fact]
    public void CorrelationId_HasCorrectValue()
    {
        global::Encina.OpenTelemetry.ActivityTagNames.EventMetadata.CorrelationId.ShouldBe("event.correlation_id");
    }

    [Fact]
    public void CausationId_HasCorrectValue()
    {
        global::Encina.OpenTelemetry.ActivityTagNames.EventMetadata.CausationId.ShouldBe("event.causation_id");
    }

    [Fact]
    public void StreamId_HasCorrectValue()
    {
        global::Encina.OpenTelemetry.ActivityTagNames.EventMetadata.StreamId.ShouldBe("event.stream_id");
    }

    [Fact]
    public void TypeName_HasCorrectValue()
    {
        global::Encina.OpenTelemetry.ActivityTagNames.EventMetadata.TypeName.ShouldBe("event.type_name");
    }

    [Fact]
    public void Version_HasCorrectValue()
    {
        global::Encina.OpenTelemetry.ActivityTagNames.EventMetadata.Version.ShouldBe("event.version");
    }

    [Fact]
    public void Sequence_HasCorrectValue()
    {
        global::Encina.OpenTelemetry.ActivityTagNames.EventMetadata.Sequence.ShouldBe("event.sequence");
    }

    [Fact]
    public void Timestamp_HasCorrectValue()
    {
        global::Encina.OpenTelemetry.ActivityTagNames.EventMetadata.Timestamp.ShouldBe("event.timestamp");
    }

    [Fact]
    public void AllConstants_UseEventPrefix()
    {
        // Verify all event metadata constants follow naming convention
        global::Encina.OpenTelemetry.ActivityTagNames.EventMetadata.MessageId.ShouldStartWith("event.");
        global::Encina.OpenTelemetry.ActivityTagNames.EventMetadata.CorrelationId.ShouldStartWith("event.");
        global::Encina.OpenTelemetry.ActivityTagNames.EventMetadata.CausationId.ShouldStartWith("event.");
        global::Encina.OpenTelemetry.ActivityTagNames.EventMetadata.StreamId.ShouldStartWith("event.");
        global::Encina.OpenTelemetry.ActivityTagNames.EventMetadata.TypeName.ShouldStartWith("event.");
        global::Encina.OpenTelemetry.ActivityTagNames.EventMetadata.Version.ShouldStartWith("event.");
        global::Encina.OpenTelemetry.ActivityTagNames.EventMetadata.Sequence.ShouldStartWith("event.");
        global::Encina.OpenTelemetry.ActivityTagNames.EventMetadata.Timestamp.ShouldStartWith("event.");
    }

    [Fact]
    public void AllConstants_AreDistinct()
    {
        var constants = new[]
        {
            global::Encina.OpenTelemetry.ActivityTagNames.EventMetadata.MessageId,
            global::Encina.OpenTelemetry.ActivityTagNames.EventMetadata.CorrelationId,
            global::Encina.OpenTelemetry.ActivityTagNames.EventMetadata.CausationId,
            global::Encina.OpenTelemetry.ActivityTagNames.EventMetadata.StreamId,
            global::Encina.OpenTelemetry.ActivityTagNames.EventMetadata.TypeName,
            global::Encina.OpenTelemetry.ActivityTagNames.EventMetadata.Version,
            global::Encina.OpenTelemetry.ActivityTagNames.EventMetadata.Sequence,
            global::Encina.OpenTelemetry.ActivityTagNames.EventMetadata.Timestamp,
        };

        constants.ShouldBeUnique();
    }
}
