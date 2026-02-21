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
/// Unit tests for <see cref="OutputEncodingPipelineBehavior{TRequest, TResponse}"/>.
/// </summary>
public sealed class OutputEncodingPipelineBehaviorTests : IDisposable
{
    private readonly IOutputEncoder _encoder;
    private readonly SanitizationOptions _options;
    private readonly IRequestContext _context;

    public OutputEncodingPipelineBehaviorTests()
    {
        _encoder = Substitute.For<IOutputEncoder>();
        _options = new SanitizationOptions();
        _context = RequestContext.CreateForTest(userId: "test-user");

        EncodingPropertyCache.ClearCache();
    }

    public void Dispose()
    {
        EncodingPropertyCache.ClearCache();
    }

    #region No Attributes (Passthrough)

    [Fact]
    public async Task Handle_NoEncodedProperties_PassesThrough()
    {
        var response = new PlainResponse { Name = "John" };
        var behavior = CreateBehavior<PlainQuery, PlainResponse>();

        var result = await behavior.Handle(
            new PlainQuery(),
            _context,
            () => ValueTask.FromResult<Either<EncinaError, PlainResponse>>(Right(response)),
            CancellationToken.None);

        result.IsRight.Should().BeTrue();
        var rightValue = result.Match(Right: r => r, Left: _ => null!);
        rightValue.Name.Should().Be("John");
    }

    [Fact]
    public async Task Handle_NoAttributes_NoAutoEncode_DoesNotCallEncoder()
    {
        var response = new PlainResponse { Name = "John" };
        var behavior = CreateBehavior<PlainQuery, PlainResponse>();

        await behavior.Handle(
            new PlainQuery(),
            _context,
            () => ValueTask.FromResult<Either<EncinaError, PlainResponse>>(Right(response)),
            CancellationToken.None);

        _encoder.DidNotReceive().EncodeForHtml(Arg.Any<string>());
        _encoder.DidNotReceive().EncodeForJavaScript(Arg.Any<string>());
        _encoder.DidNotReceive().EncodeForUrl(Arg.Any<string>());
    }

    #endregion

    #region Post-handler Encoding (Attribute-based)

    [Fact]
    public async Task Handle_WithEncodeForHtmlAttribute_CallsEncodeForHtml()
    {
        var response = new HtmlEncodedResponse { Title = "<b>test</b>" };
        var behavior = CreateBehavior<HtmlQuery, HtmlEncodedResponse>();

        _encoder.EncodeForHtml("<b>test</b>").Returns("&lt;b&gt;test&lt;/b&gt;");

        var result = await behavior.Handle(
            new HtmlQuery(),
            _context,
            () => ValueTask.FromResult<Either<EncinaError, HtmlEncodedResponse>>(Right(response)),
            CancellationToken.None);

        result.IsRight.Should().BeTrue();
        _encoder.Received(1).EncodeForHtml("<b>test</b>");
    }

    [Fact]
    public async Task Handle_WithEncodeForJavaScriptAttribute_CallsEncodeForJavaScript()
    {
        var response = new JavaScriptEncodedResponse { JsonData = "alert('xss')" };
        var behavior = CreateBehavior<JsQuery, JavaScriptEncodedResponse>();

        _encoder.EncodeForJavaScript(Arg.Any<string>()).Returns("alert\\u0028\\u0027xss\\u0027\\u0029");

        var result = await behavior.Handle(
            new JsQuery(),
            _context,
            () => ValueTask.FromResult<Either<EncinaError, JavaScriptEncodedResponse>>(Right(response)),
            CancellationToken.None);

        result.IsRight.Should().BeTrue();
        _encoder.Received(1).EncodeForJavaScript(Arg.Any<string>());
    }

    [Fact]
    public async Task Handle_WithEncodeForUrlAttribute_CallsEncodeForUrl()
    {
        var response = new UrlEncodedResponse { RedirectUrl = "https://evil.com?q=<script>" };
        var behavior = CreateBehavior<UrlQuery, UrlEncodedResponse>();

        _encoder.EncodeForUrl(Arg.Any<string>()).Returns("https%3A%2F%2Fevil.com%3Fq%3D%3Cscript%3E");

        var result = await behavior.Handle(
            new UrlQuery(),
            _context,
            () => ValueTask.FromResult<Either<EncinaError, UrlEncodedResponse>>(Right(response)),
            CancellationToken.None);

        result.IsRight.Should().BeTrue();
        _encoder.Received(1).EncodeForUrl(Arg.Any<string>());
    }

    [Fact]
    public async Task Handle_WithMultipleAttributes_EncodesAll()
    {
        var response = new MultiEncodedResponse
        {
            HtmlField = "<b>html</b>",
            JsField = "alert(1)"
        };
        var behavior = CreateBehavior<MultiQuery, MultiEncodedResponse>();

        _encoder.EncodeForHtml(Arg.Any<string>()).Returns(x => $"encoded-{x[0]}");
        _encoder.EncodeForJavaScript(Arg.Any<string>()).Returns(x => $"encoded-{x[0]}");

        var result = await behavior.Handle(
            new MultiQuery(),
            _context,
            () => ValueTask.FromResult<Either<EncinaError, MultiEncodedResponse>>(Right(response)),
            CancellationToken.None);

        result.IsRight.Should().BeTrue();
        _encoder.Received(1).EncodeForHtml(Arg.Any<string>());
        _encoder.Received(1).EncodeForJavaScript(Arg.Any<string>());
    }

    [Fact]
    public async Task Handle_EncodedPropertyModified_ResponseContainsEncodedValue()
    {
        var response = new HtmlEncodedResponse { Title = "<b>test</b>" };
        var behavior = CreateBehavior<HtmlQuery, HtmlEncodedResponse>();

        _encoder.EncodeForHtml("<b>test</b>").Returns("&lt;b&gt;test&lt;/b&gt;");

        var result = await behavior.Handle(
            new HtmlQuery(),
            _context,
            () => ValueTask.FromResult<Either<EncinaError, HtmlEncodedResponse>>(Right(response)),
            CancellationToken.None);

        var rightValue = result.Match(Right: r => r, Left: _ => null!);
        rightValue.Title.Should().Be("&lt;b&gt;test&lt;/b&gt;");
    }

    [Fact]
    public async Task Handle_NullPropertyValue_SkipsProperty()
    {
        var response = new HtmlEncodedResponse { Title = null! };
        var behavior = CreateBehavior<HtmlQuery, HtmlEncodedResponse>();

        var result = await behavior.Handle(
            new HtmlQuery(),
            _context,
            () => ValueTask.FromResult<Either<EncinaError, HtmlEncodedResponse>>(Right(response)),
            CancellationToken.None);

        result.IsRight.Should().BeTrue();
        _encoder.DidNotReceive().EncodeForHtml(Arg.Any<string>());
    }

    #endregion

    #region Auto-Encode Mode

    [Fact]
    public async Task Handle_AutoEncodeEnabled_EncodesAllStringProperties()
    {
        _options.EncodeAllOutputs = true;
        var response = new PlainResponse { Name = "<b>test</b>" };
        var behavior = CreateBehavior<PlainQuery, PlainResponse>();

        _encoder.EncodeForHtml(Arg.Any<string>()).Returns(x => $"encoded-{x[0]}");

        var result = await behavior.Handle(
            new PlainQuery(),
            _context,
            () => ValueTask.FromResult<Either<EncinaError, PlainResponse>>(Right(response)),
            CancellationToken.None);

        result.IsRight.Should().BeTrue();
        _encoder.Received().EncodeForHtml(Arg.Any<string>());
    }

    [Fact]
    public async Task Handle_AutoEncode_SkipsPropertiesWithExplicitAttributes()
    {
        _options.EncodeAllOutputs = true;
        var response = new MixedEncodedResponse
        {
            HtmlField = "<b>test</b>",
            PlainField = "plain text"
        };
        var behavior = CreateBehavior<MixedQuery, MixedEncodedResponse>();

        _encoder.EncodeForHtml(Arg.Any<string>()).Returns(x => $"encoded-{x[0]}");

        var result = await behavior.Handle(
            new MixedQuery(),
            _context,
            () => ValueTask.FromResult<Either<EncinaError, MixedEncodedResponse>>(Right(response)),
            CancellationToken.None);

        result.IsRight.Should().BeTrue();
        // EncodeForHtml called for both: HtmlField (attribute) and PlainField (auto-encode)
        _encoder.Received(2).EncodeForHtml(Arg.Any<string>());
    }

    #endregion

    #region Post-handler Behavior

    [Fact]
    public async Task Handle_HandlerReturnsError_ReturnsErrorWithoutEncoding()
    {
        var error = EncinaError.New("handler failed");
        var behavior = CreateBehavior<HtmlQuery, HtmlEncodedResponse>();

        var result = await behavior.Handle(
            new HtmlQuery(),
            _context,
            () => ValueTask.FromResult<Either<EncinaError, HtmlEncodedResponse>>(Left(error)),
            CancellationToken.None);

        result.IsLeft.Should().BeTrue();
        _encoder.DidNotReceive().EncodeForHtml(Arg.Any<string>());
    }

    [Fact]
    public async Task Handle_AlwaysCallsNextStepFirst()
    {
        var nextStepCalled = false;
        var response = new PlainResponse { Name = "test" };
        var behavior = CreateBehavior<PlainQuery, PlainResponse>();

        await behavior.Handle(
            new PlainQuery(),
            _context,
            () =>
            {
                nextStepCalled = true;
                return ValueTask.FromResult<Either<EncinaError, PlainResponse>>(Right(response));
            },
            CancellationToken.None);

        nextStepCalled.Should().BeTrue();
    }

    #endregion

    #region Error Handling

    [Fact]
    public async Task Handle_EncoderThrowsException_ReturnsError()
    {
        var response = new HtmlEncodedResponse { Title = "test" };
        var behavior = CreateBehavior<HtmlQuery, HtmlEncodedResponse>();

        _encoder.EncodeForHtml(Arg.Any<string>())
            .Throws(new InvalidOperationException("Encoder failure"));

        var result = await behavior.Handle(
            new HtmlQuery(),
            _context,
            () => ValueTask.FromResult<Either<EncinaError, HtmlEncodedResponse>>(Right(response)),
            CancellationToken.None);

        result.IsLeft.Should().BeTrue();
    }

    #endregion

    #region Helpers

    private OutputEncodingPipelineBehavior<TRequest, TResponse> CreateBehavior<TRequest, TResponse>()
        where TRequest : IRequest<TResponse>
    {
        var logger = new NullLoggerFactory().CreateLogger<OutputEncodingPipelineBehavior<TRequest, TResponse>>();
        return new OutputEncodingPipelineBehavior<TRequest, TResponse>(
            _encoder,
            Options.Create(_options),
            logger);
    }

    #endregion

    #region Test Types

    // Each query type matches its response type for IRequest<TResponse> constraint
    public sealed class PlainQuery : IQuery<PlainResponse> { }
    public sealed class HtmlQuery : IQuery<HtmlEncodedResponse> { }
    public sealed class JsQuery : IQuery<JavaScriptEncodedResponse> { }
    public sealed class UrlQuery : IQuery<UrlEncodedResponse> { }
    public sealed class MultiQuery : IQuery<MultiEncodedResponse> { }
    public sealed class MixedQuery : IQuery<MixedEncodedResponse> { }

    public sealed class PlainResponse
    {
        public string Name { get; set; } = string.Empty;
    }

    public sealed class HtmlEncodedResponse
    {
        [EncodeForHtml]
        public string Title { get; set; } = string.Empty;
    }

    public sealed class JavaScriptEncodedResponse
    {
        [EncodeForJavaScript]
        public string JsonData { get; set; } = string.Empty;
    }

    public sealed class UrlEncodedResponse
    {
        [EncodeForUrl]
        public string RedirectUrl { get; set; } = string.Empty;
    }

    public sealed class MultiEncodedResponse
    {
        [EncodeForHtml]
        public string HtmlField { get; set; } = string.Empty;

        [EncodeForJavaScript]
        public string JsField { get; set; } = string.Empty;
    }

    public sealed class MixedEncodedResponse
    {
        [EncodeForHtml]
        public string HtmlField { get; set; } = string.Empty;

        public string PlainField { get; set; } = string.Empty;
    }

    #endregion
}
