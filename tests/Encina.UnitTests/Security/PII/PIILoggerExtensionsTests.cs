using Encina.Security.PII;
using Encina.Security.PII.Abstractions;
using Encina.Security.PII.Attributes;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Encina.UnitTests.Security.PII;

public sealed class PIILoggerExtensionsTests
{
    #region Test DTO

    private sealed class LogTestDto
    {
        [PII(PIIType.Email)]
        public string Email { get; set; } = "secret@example.com";

        public string Public { get; set; } = "visible";
    }

    #endregion

    #region LogMasked

    [Fact]
    public void LogMasked_WhenLevelEnabled_LogsMaskedData()
    {
        var logger = Substitute.For<ILogger>();
        logger.IsEnabled(LogLevel.Information).Returns(true);
        var masker = Substitute.For<IPIIMasker>();
        var data = new LogTestDto();
        var maskedData = new LogTestDto { Email = "s***@example.com", Public = "visible" };
        masker.MaskObject(data).Returns(maskedData);

        logger.LogMasked(masker, LogLevel.Information, "Processing: {@Data}", data);

        masker.Received(1).MaskObject(data);
        logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Is<Exception?>(e => e == null),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void LogMasked_WhenLevelDisabled_DoesNotMask()
    {
        var logger = Substitute.For<ILogger>();
        logger.IsEnabled(LogLevel.Debug).Returns(false);
        var masker = Substitute.For<IPIIMasker>();
        var data = new LogTestDto();

        logger.LogMasked(masker, LogLevel.Debug, "Processing: {@Data}", data);

        masker.DidNotReceive().MaskObject(Arg.Any<LogTestDto>());
    }

    [Fact]
    public void LogMasked_WhenMaskingFails_LogsFallback()
    {
        var logger = Substitute.For<ILogger>();
        logger.IsEnabled(LogLevel.Information).Returns(true);
        logger.IsEnabled(LogLevel.Warning).Returns(true);
        var masker = Substitute.For<IPIIMasker>();
        var data = new LogTestDto();
        masker.MaskObject(data).Throws(new InvalidOperationException("masking error"));

        logger.LogMasked(masker, LogLevel.Information, "Processing: {@Data}", data);

        // Should have logged at Information level (fallback) and Warning level (exception)
        logger.Received().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Is<Exception?>(e => e == null),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void LogMasked_NullLogger_ThrowsArgumentNull()
    {
        ILogger logger = null!;
        var masker = Substitute.For<IPIIMasker>();
        var data = new LogTestDto();

        Should.Throw<ArgumentNullException>(() =>
            logger.LogMasked(masker, LogLevel.Information, "msg", data));
    }

    [Fact]
    public void LogMasked_NullMasker_ThrowsArgumentNull()
    {
        var logger = Substitute.For<ILogger>();
        var data = new LogTestDto();

        Should.Throw<ArgumentNullException>(() =>
            logger.LogMasked(null!, LogLevel.Information, "msg", data));
    }

    #endregion

    #region LogInformationMasked

    [Fact]
    public void LogInformationMasked_CallsLogMaskedWithInformationLevel()
    {
        var logger = Substitute.For<ILogger>();
        logger.IsEnabled(LogLevel.Information).Returns(true);
        var masker = Substitute.For<IPIIMasker>();
        var data = new LogTestDto();
        var maskedData = new LogTestDto { Email = "masked" };
        masker.MaskObject(data).Returns(maskedData);

        logger.LogInformationMasked(masker, "Info: {@Data}", data);

        masker.Received(1).MaskObject(data);
        logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Is<Exception?>(e => e == null),
            Arg.Any<Func<object, Exception?, string>>());
    }

    #endregion

    #region LogWarningMasked

    [Fact]
    public void LogWarningMasked_CallsLogMaskedWithWarningLevel()
    {
        var logger = Substitute.For<ILogger>();
        logger.IsEnabled(LogLevel.Warning).Returns(true);
        var masker = Substitute.For<IPIIMasker>();
        var data = new LogTestDto();
        var maskedData = new LogTestDto { Email = "masked" };
        masker.MaskObject(data).Returns(maskedData);

        logger.LogWarningMasked(masker, "Warning: {@Data}", data);

        masker.Received(1).MaskObject(data);
        logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Is<Exception?>(e => e == null),
            Arg.Any<Func<object, Exception?, string>>());
    }

    #endregion

    #region LogErrorMasked

    [Fact]
    public void LogErrorMasked_CallsLogMaskedWithErrorLevel()
    {
        var logger = Substitute.For<ILogger>();
        logger.IsEnabled(LogLevel.Error).Returns(true);
        var masker = Substitute.For<IPIIMasker>();
        var data = new LogTestDto();
        var maskedData = new LogTestDto { Email = "masked" };
        masker.MaskObject(data).Returns(maskedData);

        logger.LogErrorMasked(masker, "Error: {@Data}", data);

        masker.Received(1).MaskObject(data);
        logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Is<Exception?>(e => e == null),
            Arg.Any<Func<object, Exception?, string>>());
    }

    #endregion
}
