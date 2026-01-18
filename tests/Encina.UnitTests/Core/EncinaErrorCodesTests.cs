namespace Encina.UnitTests.Core;

public sealed class EncinaErrorCodesTests
{
    [Fact]
    public void RequestNull_HasCorrectValue()
    {
        EncinaErrorCodes.RequestNull.ShouldBe("encina.request.null");
    }

    [Fact]
    public void NotificationNull_HasCorrectValue()
    {
        EncinaErrorCodes.NotificationNull.ShouldBe("encina.notification.null");
    }

    [Fact]
    public void HandlerMissing_HasCorrectValue()
    {
        EncinaErrorCodes.HandlerMissing.ShouldBe("encina.handler.missing");
    }

    [Fact]
    public void RequestHandlerMissing_HasCorrectValue()
    {
        EncinaErrorCodes.RequestHandlerMissing.ShouldBe("encina.request.handler_missing");
    }

    [Fact]
    public void HandlerException_HasCorrectValue()
    {
        EncinaErrorCodes.HandlerException.ShouldBe("encina.handler.exception");
    }

    [Fact]
    public void BehaviorException_HasCorrectValue()
    {
        EncinaErrorCodes.BehaviorException.ShouldBe("encina.behavior.exception");
    }

    [Fact]
    public void PipelineException_HasCorrectValue()
    {
        EncinaErrorCodes.PipelineException.ShouldBe("encina.pipeline.exception");
    }

    [Fact]
    public void Timeout_HasCorrectValue()
    {
        EncinaErrorCodes.Timeout.ShouldBe("encina.timeout");
    }

    [Fact]
    public void RateLimitExceeded_HasCorrectValue()
    {
        EncinaErrorCodes.RateLimitExceeded.ShouldBe("encina.ratelimit.exceeded");
    }

    [Fact]
    public void AllErrorCodes_StartWithEncinaPrefix()
    {
        var fields = typeof(EncinaErrorCodes)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string));

        foreach (var field in fields)
        {
            var value = (string)field.GetValue(null)!;
            value.ShouldStartWith("encina.");
        }
    }
}
