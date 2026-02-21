#pragma warning disable CA2012 // ValueTask instances used in NSubstitute mock setup

using Encina.Security.Sanitization;
using Encina.Security.Sanitization.Abstractions;
using Encina.Security.Sanitization.Attributes;
using FluentAssertions;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Security.Sanitization;

/// <summary>
/// Unit tests for <see cref="InputSanitizationPipelineBehavior{TRequest, TResponse}"/>.
/// </summary>
public sealed class InputSanitizationPipelineBehaviorTests : IDisposable
{
    private readonly SanitizationOrchestrator _orchestrator;
    private readonly SanitizationOptions _options;
    private readonly ISanitizer _sanitizer;
    private readonly IRequestContext _context;

    public InputSanitizationPipelineBehaviorTests()
    {
        _sanitizer = Substitute.For<ISanitizer>();
        _options = new SanitizationOptions();
        _orchestrator = new SanitizationOrchestrator(
            _sanitizer,
            Options.Create(_options),
            NullLogger<SanitizationOrchestrator>.Instance);
        _context = RequestContext.CreateForTest(userId: "test-user");

        SanitizationPropertyCache.ClearCache();
    }

    public void Dispose()
    {
        SanitizationPropertyCache.ClearCache();
    }

    #region No Attributes (Passthrough)

    [Fact]
    public async Task Handle_NoSanitizedProperties_PassesThrough()
    {
        var request = new PlainCommand { Name = "John" };
        var behavior = CreateBehavior<PlainCommand, Unit>();
        var nextStepCalled = false;

        var result = await behavior.Handle(
            request,
            _context,
            () =>
            {
                nextStepCalled = true;
                return ValueTask.FromResult<Either<EncinaError, Unit>>(Right(Unit.Default));
            },
            CancellationToken.None);

        result.IsRight.Should().BeTrue();
        nextStepCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_NoAttributes_NoAutoSanitize_DoesNotCallSanitizer()
    {
        var request = new PlainCommand { Name = "John" };
        var behavior = CreateBehavior<PlainCommand, Unit>();

        await behavior.Handle(
            request,
            _context,
            () => ValueTask.FromResult<Either<EncinaError, Unit>>(Right(Unit.Default)),
            CancellationToken.None);

        _sanitizer.DidNotReceive().SanitizeHtml(Arg.Any<string>());
        _sanitizer.DidNotReceive().SanitizeForSql(Arg.Any<string>());
    }

    #endregion

    #region Pre-handler Sanitization (Attribute-based)

    [Fact]
    public async Task Handle_WithSanitizeHtmlAttribute_CallsSanitizeHtml()
    {
        var request = new HtmlSanitizeCommand { Title = "<script>alert('xss')</script>Safe" };
        var behavior = CreateBehavior<HtmlSanitizeCommand, Unit>();

        _sanitizer.SanitizeHtml(Arg.Any<string>()).Returns("Safe");

        var result = await behavior.Handle(
            request,
            _context,
            () => ValueTask.FromResult<Either<EncinaError, Unit>>(Right(Unit.Default)),
            CancellationToken.None);

        result.IsRight.Should().BeTrue();
        _sanitizer.Received(1).SanitizeHtml(Arg.Any<string>());
    }

    [Fact]
    public async Task Handle_WithSanitizeSqlAttribute_CallsSanitizeForSql()
    {
        var request = new SqlSanitizeCommand { SearchTerm = "'; DROP TABLE--" };
        var behavior = CreateBehavior<SqlSanitizeCommand, Unit>();

        _sanitizer.SanitizeForSql(Arg.Any<string>()).Returns("DROP TABLE");

        var result = await behavior.Handle(
            request,
            _context,
            () => ValueTask.FromResult<Either<EncinaError, Unit>>(Right(Unit.Default)),
            CancellationToken.None);

        result.IsRight.Should().BeTrue();
        _sanitizer.Received(1).SanitizeForSql(Arg.Any<string>());
    }

    [Fact]
    public async Task Handle_WithMultipleAttributes_SanitizesAll()
    {
        var request = new MultiAttributeCommand
        {
            HtmlField = "<b>test</b>",
            SqlField = "safe-value"
        };
        var behavior = CreateBehavior<MultiAttributeCommand, Unit>();

        _sanitizer.SanitizeHtml(Arg.Any<string>()).Returns(x => (string)x[0]);
        _sanitizer.SanitizeForSql(Arg.Any<string>()).Returns(x => (string)x[0]);

        var result = await behavior.Handle(
            request,
            _context,
            () => ValueTask.FromResult<Either<EncinaError, Unit>>(Right(Unit.Default)),
            CancellationToken.None);

        result.IsRight.Should().BeTrue();
        _sanitizer.Received(1).SanitizeHtml(Arg.Any<string>());
        _sanitizer.Received(1).SanitizeForSql(Arg.Any<string>());
    }

    [Fact]
    public async Task Handle_SanitizedPropertyModified_RequestContainsSanitizedValue()
    {
        var request = new HtmlSanitizeCommand { Title = "<script>xss</script>Clean" };
        var behavior = CreateBehavior<HtmlSanitizeCommand, Unit>();

        _sanitizer.SanitizeHtml("<script>xss</script>Clean").Returns("Clean");

        await behavior.Handle(
            request,
            _context,
            () => ValueTask.FromResult<Either<EncinaError, Unit>>(Right(Unit.Default)),
            CancellationToken.None);

        request.Title.Should().Be("Clean");
    }

    [Fact]
    public async Task Handle_NullPropertyValue_SkipsProperty()
    {
        var request = new HtmlSanitizeCommand { Title = null! };
        var behavior = CreateBehavior<HtmlSanitizeCommand, Unit>();

        var result = await behavior.Handle(
            request,
            _context,
            () => ValueTask.FromResult<Either<EncinaError, Unit>>(Right(Unit.Default)),
            CancellationToken.None);

        result.IsRight.Should().BeTrue();
        _sanitizer.DidNotReceive().SanitizeHtml(Arg.Any<string>());
    }

    #endregion

    #region Auto-Sanitize Mode

    [Fact]
    public async Task Handle_AutoSanitizeEnabled_SanitizesAllStringProperties()
    {
        _options.SanitizeAllStringInputs = true;
        var request = new PlainCommand { Name = "test-value" };
        var behavior = CreateBehavior<PlainCommand, Unit>();

        _sanitizer.Custom(Arg.Any<string>(), Arg.Any<ISanitizationProfile>())
            .Returns(x => (string)x[0]);

        var result = await behavior.Handle(
            request,
            _context,
            () => ValueTask.FromResult<Either<EncinaError, Unit>>(Right(Unit.Default)),
            CancellationToken.None);

        result.IsRight.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_AutoSanitize_SkipsPropertiesWithExplicitAttributes()
    {
        _options.SanitizeAllStringInputs = true;
        var request = new MixedCommand
        {
            HtmlField = "<b>test</b>",
            PlainField = "plain text"
        };
        var behavior = CreateBehavior<MixedCommand, Unit>();

        _sanitizer.SanitizeHtml(Arg.Any<string>()).Returns(x => (string)x[0]);
        _sanitizer.Custom(Arg.Any<string>(), Arg.Any<ISanitizationProfile>())
            .Returns(x => (string)x[0]);

        var result = await behavior.Handle(
            request,
            _context,
            () => ValueTask.FromResult<Either<EncinaError, Unit>>(Right(Unit.Default)),
            CancellationToken.None);

        result.IsRight.Should().BeTrue();
        // HtmlField is sanitized by attribute, PlainField by auto-sanitize
        _sanitizer.Received(1).SanitizeHtml(Arg.Any<string>());
    }

    #endregion

    #region Error Handling

    [Fact]
    public async Task Handle_SanitizerThrowsException_ReturnsError()
    {
        var request = new HtmlSanitizeCommand { Title = "test" };
        var behavior = CreateBehavior<HtmlSanitizeCommand, Unit>();

        _sanitizer.SanitizeHtml(Arg.Any<string>())
            .Throws(new InvalidOperationException("Sanitizer failure"));

        var result = await behavior.Handle(
            request,
            _context,
            () => ValueTask.FromResult<Either<EncinaError, Unit>>(Right(Unit.Default)),
            CancellationToken.None);

        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_SanitizationFails_DoesNotCallNextStep()
    {
        var request = new HtmlSanitizeCommand { Title = "test" };
        var behavior = CreateBehavior<HtmlSanitizeCommand, Unit>();
        var nextStepCalled = false;

        _sanitizer.SanitizeHtml(Arg.Any<string>())
            .Throws(new InvalidOperationException("Sanitizer failure"));

        await behavior.Handle(
            request,
            _context,
            () =>
            {
                nextStepCalled = true;
                return ValueTask.FromResult<Either<EncinaError, Unit>>(Right(Unit.Default));
            },
            CancellationToken.None);

        nextStepCalled.Should().BeFalse();
    }

    #endregion

    #region NextStep Invocation

    [Fact]
    public async Task Handle_Successful_InvokesNextStep()
    {
        var request = new HtmlSanitizeCommand { Title = "safe" };
        var behavior = CreateBehavior<HtmlSanitizeCommand, Unit>();
        var nextStepCalled = false;

        _sanitizer.SanitizeHtml(Arg.Any<string>()).Returns(x => (string)x[0]);

        await behavior.Handle(
            request,
            _context,
            () =>
            {
                nextStepCalled = true;
                return ValueTask.FromResult<Either<EncinaError, Unit>>(Right(Unit.Default));
            },
            CancellationToken.None);

        nextStepCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_NextStepReturnsError_ReturnsError()
    {
        var request = new HtmlSanitizeCommand { Title = "safe" };
        var behavior = CreateBehavior<HtmlSanitizeCommand, Unit>();
        var error = EncinaError.New("handler failed");

        _sanitizer.SanitizeHtml(Arg.Any<string>()).Returns(x => (string)x[0]);

        var result = await behavior.Handle(
            request,
            _context,
            () => ValueTask.FromResult<Either<EncinaError, Unit>>(Left(error)),
            CancellationToken.None);

        result.IsLeft.Should().BeTrue();
    }

    #endregion

    #region Helpers

    private InputSanitizationPipelineBehavior<TRequest, TResponse> CreateBehavior<TRequest, TResponse>()
        where TRequest : IRequest<TResponse>
    {
        var logger = new NullLoggerFactory().CreateLogger<InputSanitizationPipelineBehavior<TRequest, TResponse>>();
        return new InputSanitizationPipelineBehavior<TRequest, TResponse>(
            _orchestrator,
            Options.Create(_options),
            logger);
    }

    #endregion

    #region Test Request Types

    public sealed class PlainCommand : ICommand<Unit>
    {
        public string Name { get; set; } = string.Empty;
    }

    public sealed class HtmlSanitizeCommand : ICommand<Unit>
    {
        [SanitizeHtml]
        public string Title { get; set; } = string.Empty;
    }

    public sealed class SqlSanitizeCommand : ICommand<Unit>
    {
        [SanitizeSql]
        public string SearchTerm { get; set; } = string.Empty;
    }

    public sealed class MultiAttributeCommand : ICommand<Unit>
    {
        [SanitizeHtml]
        public string HtmlField { get; set; } = string.Empty;

        [SanitizeSql]
        public string SqlField { get; set; } = string.Empty;
    }

    public sealed class MixedCommand : ICommand<Unit>
    {
        [SanitizeHtml]
        public string HtmlField { get; set; } = string.Empty;

        public string PlainField { get; set; } = string.Empty;
    }

    #endregion
}
