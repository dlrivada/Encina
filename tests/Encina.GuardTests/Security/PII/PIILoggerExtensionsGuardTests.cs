using Encina.Security.PII;
using Encina.Security.PII.Abstractions;

namespace Encina.GuardTests.Security.PII;

/// <summary>
/// Guard clause tests for <see cref="PIILoggerExtensions"/>: null logger and null masker are rejected
/// by <c>LogMasked</c> and its three level wrappers.
/// </summary>
public sealed class PIILoggerExtensionsGuardTests
{
    private sealed class Payload
    {
        public string Name { get; set; } = "x";
    }

    private static readonly IPIIMasker Masker = Substitute.For<IPIIMasker>();

    private static readonly ILogger Logger = Substitute.For<ILogger>();

    [Fact]
    public void LogMasked_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => PIILoggerExtensions.LogMasked(null!, Masker, LogLevel.Information, "m {@D}", new Payload());

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    [Fact]
    public void LogMasked_NullMasker_ThrowsArgumentNullException()
    {
        var act = () => Logger.LogMasked(null!, LogLevel.Information, "m {@D}", new Payload());

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("masker");
    }

    [Fact]
    public void LogInformationMasked_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => PIILoggerExtensions.LogInformationMasked(null!, Masker, "m {@D}", new Payload());

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    [Fact]
    public void LogInformationMasked_NullMasker_ThrowsArgumentNullException()
    {
        var act = () => Logger.LogInformationMasked(null!, "m {@D}", new Payload());

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("masker");
    }

    [Fact]
    public void LogWarningMasked_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => PIILoggerExtensions.LogWarningMasked(null!, Masker, "m {@D}", new Payload());

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    [Fact]
    public void LogWarningMasked_NullMasker_ThrowsArgumentNullException()
    {
        var act = () => Logger.LogWarningMasked(null!, "m {@D}", new Payload());

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("masker");
    }

    [Fact]
    public void LogErrorMasked_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => PIILoggerExtensions.LogErrorMasked(null!, Masker, "m {@D}", new Payload());

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    [Fact]
    public void LogErrorMasked_NullMasker_ThrowsArgumentNullException()
    {
        var act = () => Logger.LogErrorMasked(null!, "m {@D}", new Payload());

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("masker");
    }
}
