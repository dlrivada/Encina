using Encina.Cdc.Errors;
using Shouldly;

namespace Encina.UnitTests.Cdc;

/// <summary>
/// Unit tests for <see cref="CdcErrors"/> factory methods and <see cref="CdcErrorCodes"/> constants.
/// </summary>
public sealed class CdcErrorsTests
{
    #region CdcErrorCodes

    [Fact]
    public void ConnectionFailed_Code_HasCorrectFormat()
    {
        CdcErrorCodes.ConnectionFailed.ShouldBe("encina.cdc.connection_failed");
    }

    [Fact]
    public void PositionInvalid_Code_HasCorrectFormat()
    {
        CdcErrorCodes.PositionInvalid.ShouldBe("encina.cdc.position_invalid");
    }

    [Fact]
    public void StreamInterrupted_Code_HasCorrectFormat()
    {
        CdcErrorCodes.StreamInterrupted.ShouldBe("encina.cdc.stream_interrupted");
    }

    [Fact]
    public void HandlerFailed_Code_HasCorrectFormat()
    {
        CdcErrorCodes.HandlerFailed.ShouldBe("encina.cdc.handler_failed");
    }

    [Fact]
    public void DeserializationFailed_Code_HasCorrectFormat()
    {
        CdcErrorCodes.DeserializationFailed.ShouldBe("encina.cdc.deserialization_failed");
    }

    [Fact]
    public void PositionStoreFailed_Code_HasCorrectFormat()
    {
        CdcErrorCodes.PositionStoreFailed.ShouldBe("encina.cdc.position_store_failed");
    }

    #endregion

    #region CdcErrors Factory Methods

    [Fact]
    public void ConnectionFailed_WithReason_ContainsReasonInMessage()
    {
        var error = CdcErrors.ConnectionFailed("server unreachable");

        error.ToString().ShouldContain("server unreachable");
        error.ToString().ShouldContain(CdcErrorCodes.ConnectionFailed);
    }

    [Fact]
    public void ConnectionFailed_WithException_ContainsExceptionInfo()
    {
        var ex = new InvalidOperationException("test exception");
        var error = CdcErrors.ConnectionFailed("server down", ex);

        error.ToString().ShouldContain("server down");
        error.ToString().ShouldContain(CdcErrorCodes.ConnectionFailed);
    }

    [Fact]
    public void PositionInvalid_ContainsPositionInfo()
    {
        var position = new TestCdcPosition(42);
        var error = CdcErrors.PositionInvalid(position);

        error.ToString().ShouldContain("TestPosition(42)");
        error.ToString().ShouldContain(CdcErrorCodes.PositionInvalid);
    }

    [Fact]
    public void StreamInterrupted_ContainsExceptionInfo()
    {
        var ex = new TimeoutException("connection timed out");
        var error = CdcErrors.StreamInterrupted(ex);

        error.ToString().ShouldContain(CdcErrorCodes.StreamInterrupted);
    }

    [Fact]
    public void HandlerFailed_ContainsTableName()
    {
        var ex = new InvalidOperationException("handler error");
        var error = CdcErrors.HandlerFailed("Orders", ex);

        error.ToString().ShouldContain("Orders");
        error.ToString().ShouldContain(CdcErrorCodes.HandlerFailed);
    }

    [Fact]
    public void DeserializationFailed_ContainsTableAndType()
    {
        var ex = new FormatException("invalid format");
        var error = CdcErrors.DeserializationFailed("Products", typeof(string), ex);

        error.ToString().ShouldContain("Products");
        error.ToString().ShouldContain("String");
        error.ToString().ShouldContain(CdcErrorCodes.DeserializationFailed);
    }

    [Fact]
    public void PositionStoreFailed_ContainsConnectorId()
    {
        var ex = new IOException("disk full");
        var error = CdcErrors.PositionStoreFailed("my-connector", ex);

        error.ToString().ShouldContain("my-connector");
        error.ToString().ShouldContain(CdcErrorCodes.PositionStoreFailed);
    }

    #endregion
}
