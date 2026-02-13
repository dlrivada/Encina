namespace Encina.UnitTests.IdGeneration;

/// <summary>
/// Unit tests for <see cref="IdGenerationErrorCodes"/> and <see cref="IdGenerationErrors"/>.
/// </summary>
public sealed class IdGenerationErrorCodesTests
{
    [Fact]
    public void ClockDriftDetected_HasCorrectCode()
    {
        IdGenerationErrorCodes.ClockDriftDetected.ShouldBe("encina.idgen.clock_drift_detected");
    }

    [Fact]
    public void SequenceExhausted_HasCorrectCode()
    {
        IdGenerationErrorCodes.SequenceExhausted.ShouldBe("encina.idgen.sequence_exhausted");
    }

    [Fact]
    public void InvalidShardId_HasCorrectCode()
    {
        IdGenerationErrorCodes.InvalidShardId.ShouldBe("encina.idgen.invalid_shard_id");
    }

    [Fact]
    public void IdParseFailure_HasCorrectCode()
    {
        IdGenerationErrorCodes.IdParseFailure.ShouldBe("encina.idgen.id_parse_failure");
    }

    [Fact]
    public void ClockDriftDetected_Error_ContainsMessage()
    {
        var error = IdGenerationErrors.ClockDriftDetected(100);
        error.ToString().ShouldContain("100ms");
    }

    [Fact]
    public void SequenceExhausted_Error_ContainsMaxSequence()
    {
        var error = IdGenerationErrors.SequenceExhausted(4095);
        error.ToString().ShouldContain("4095");
    }

    [Fact]
    public void InvalidShardId_Error_ContainsShardId()
    {
        var error = IdGenerationErrors.InvalidShardId("bad-shard", "out of range");
        error.ToString().ShouldContain("bad-shard");
        error.ToString().ShouldContain("out of range");
    }

    [Fact]
    public void InvalidShardId_NullShardId_ContainsNullMarker()
    {
        var error = IdGenerationErrors.InvalidShardId(null, "cannot be null");
        error.ToString().ShouldContain("(null)");
    }

    [Fact]
    public void IdParseFailure_Error_ContainsValueAndType()
    {
        var error = IdGenerationErrors.IdParseFailure("bad-value", "SnowflakeId");
        error.ToString().ShouldContain("bad-value");
        error.ToString().ShouldContain("SnowflakeId");
    }

    [Fact]
    public void IdParseFailure_WithException_ContainsExceptionMessage()
    {
        var exception = new FormatException("bad format");
        var error = IdGenerationErrors.IdParseFailure("val", "UlidId", exception);
        error.ToString().ShouldContain("bad format");
    }
}
