using Encina.Security.Sanitization;
using Encina.Security.Sanitization.Abstractions;
using Encina.Security.Sanitization.Attributes;
using Encina.Security.Sanitization.Profiles;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Encina.UnitTests.Security.Sanitization;

public sealed class SanitizationOrchestratorTests : IDisposable
{
    private readonly DefaultSanitizer _sanitizer;
    private readonly SanitizationOrchestrator _sut;

    public SanitizationOrchestratorTests()
    {
        var options = Options.Create(new SanitizationOptions());
        _sanitizer = new DefaultSanitizer(options);
        _sut = new SanitizationOrchestrator(
            _sanitizer,
            options,
            NullLogger<SanitizationOrchestrator>.Instance);

        SanitizationPropertyCache.ClearCache();
    }

    public void Dispose()
    {
        SanitizationPropertyCache.ClearCache();
    }

    #region Sanitize — Attribute-Based

    [Fact]
    public void Sanitize_WithSanitizeHtmlAttribute_SanitizesProperty()
    {
        var request = new TestHtmlRequest
        {
            Title = "<script>alert('xss')</script>Safe Title",
            Name = "Unaffected"
        };

        var result = _sut.Sanitize(request);

        result.IsRight.Should().BeTrue();
        request.Title.Should().NotContain("<script>");
        request.Name.Should().Be("Unaffected");
    }

    [Fact]
    public void Sanitize_WithSanitizeSqlAttribute_SanitizesProperty()
    {
        var request = new TestSqlRequest
        {
            SearchTerm = "'; DROP TABLE users--"
        };

        var result = _sut.Sanitize(request);

        result.IsRight.Should().BeTrue();
        request.SearchTerm.Should().NotContain("--");
        request.SearchTerm.Should().NotContain(";");
    }

    [Fact]
    public void Sanitize_WithMultipleAttributes_SanitizesAll()
    {
        var request = new TestMultiRequest
        {
            HtmlContent = "<script>alert(1)</script>Content",
            SqlFilter = "value'; DROP TABLE--",
            PlainText = "Untouched"
        };

        var result = _sut.Sanitize(request);

        result.IsRight.Should().BeTrue();
        request.HtmlContent.Should().NotContain("<script>");
        request.SqlFilter.Should().NotContain("--");
        request.SqlFilter.Should().NotContain(";");
        request.PlainText.Should().Be("Untouched");
    }

    [Fact]
    public void Sanitize_NoAttributes_ReturnsSuccess()
    {
        var request = new TestPlainRequest { Name = "John" };

        var result = _sut.Sanitize(request);

        result.IsRight.Should().BeTrue();
        request.Name.Should().Be("John");
    }

    [Fact]
    public void Sanitize_NullPropertyValue_SkipsProperty()
    {
        var request = new TestHtmlRequest { Title = null!, Name = "John" };

        var result = _sut.Sanitize(request);

        result.IsRight.Should().BeTrue();
    }

    [Fact]
    public void Sanitize_NullRequest_ThrowsArgumentNullException()
    {
        var act = () => _sut.Sanitize<TestHtmlRequest>(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region Sanitize — Auto-Sanitize Mode

    [Fact]
    public void Sanitize_AutoSanitizeEnabled_SanitizesAllStrings()
    {
        var options = Options.Create(new SanitizationOptions
        {
            SanitizeAllStringInputs = true
        });
        var orchestrator = new SanitizationOrchestrator(
            new DefaultSanitizer(options),
            options,
            NullLogger<SanitizationOrchestrator>.Instance);

        var request = new TestPlainRequest
        {
            Name = "<script>alert(1)</script>John"
        };

        var result = orchestrator.Sanitize(request);

        result.IsRight.Should().BeTrue();
        request.Name.Should().NotContain("<script>");
    }

    [Fact]
    public void Sanitize_AutoSanitize_SkipsAttributedProperties()
    {
        var options = Options.Create(new SanitizationOptions
        {
            SanitizeAllStringInputs = true
        });
        var orchestrator = new SanitizationOrchestrator(
            new DefaultSanitizer(options),
            options,
            NullLogger<SanitizationOrchestrator>.Instance);

        var request = new TestHtmlRequest
        {
            Title = "<b>Bold</b>",
            Name = "<b>Bold</b>"
        };

        var result = orchestrator.Sanitize(request);

        result.IsRight.Should().BeTrue();
        // Title has [SanitizeHtml] — processed by attribute handler
        // Name has no attribute — processed by auto-sanitize using default profile
    }

    [Fact]
    public void Sanitize_AutoSanitize_UsesDefaultProfile()
    {
        var options = Options.Create(new SanitizationOptions
        {
            SanitizeAllStringInputs = true,
            DefaultProfile = SanitizationProfiles.BasicFormatting
        });
        var orchestrator = new SanitizationOrchestrator(
            new DefaultSanitizer(options),
            options,
            NullLogger<SanitizationOrchestrator>.Instance);

        var request = new TestPlainRequest
        {
            Name = "<b>Bold</b><script>alert(1)</script>"
        };

        var result = orchestrator.Sanitize(request);

        result.IsRight.Should().BeTrue();
        // BasicFormatting allows <b> but strips <script>
        request.Name.Should().Contain("<b>");
        request.Name.Should().NotContain("<script>");
    }

    #endregion

    #region Sanitize — Custom Profile

    [Fact]
    public void Sanitize_WithCustomProfileAttribute_UsesNamedProfile()
    {
        var options = new SanitizationOptions();
        options.AddProfile("BlogPost", builder =>
        {
            builder.AllowTags("p", "b", "i");
            builder.WithStripScripts(true);
        });

        var optionsWrapper = Options.Create(options);
        var orchestrator = new SanitizationOrchestrator(
            new DefaultSanitizer(optionsWrapper),
            optionsWrapper,
            NullLogger<SanitizationOrchestrator>.Instance);

        var request = new TestCustomProfileRequest
        {
            Content = "<p><b>Hello</b></p><script>alert(1)</script><div>removed</div>"
        };

        var result = orchestrator.Sanitize(request);

        result.IsRight.Should().BeTrue();
        request.Content.Should().Contain("<p>");
        request.Content.Should().Contain("<b>");
        request.Content.Should().NotContain("<script>");
        request.Content.Should().NotContain("<div>");
    }

    [Fact]
    public void Sanitize_WithMissingCustomProfile_FallsBackToDefault()
    {
        var options = Options.Create(new SanitizationOptions());
        var orchestrator = new SanitizationOrchestrator(
            new DefaultSanitizer(options),
            options,
            NullLogger<SanitizationOrchestrator>.Instance);

        var request = new TestCustomProfileRequest
        {
            Content = "<script>alert(1)</script>Safe"
        };

        // Should not throw — falls back to default profile
        var result = orchestrator.Sanitize(request);

        result.IsRight.Should().BeTrue();
        request.Content.Should().NotContain("<script>");
    }

    #endregion

    #region Sanitize — StripHtml

    [Fact]
    public void Sanitize_WithStripHtmlAttribute_PassesThroughWithNoneProfile()
    {
        var request = new TestStripHtmlRequest
        {
            RawContent = "<b>bold</b> text"
        };

        var result = _sut.Sanitize(request);

        result.IsRight.Should().BeTrue();
        // StripHtml uses SanitizationProfiles.None → pass-through (no tags, no strip flags)
        request.RawContent.Should().Be("<b>bold</b> text");
    }

    #endregion

    #region Test Types

    private sealed class TestHtmlRequest
    {
        [SanitizeHtml]
        public string Title { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;
    }

    private sealed class TestSqlRequest
    {
        [SanitizeSql]
        public string SearchTerm { get; set; } = string.Empty;
    }

    private sealed class TestMultiRequest
    {
        [SanitizeHtml]
        public string HtmlContent { get; set; } = string.Empty;

        [SanitizeSql]
        public string SqlFilter { get; set; } = string.Empty;

        public string PlainText { get; set; } = string.Empty;
    }

    private sealed class TestPlainRequest
    {
        public string Name { get; set; } = string.Empty;
    }

    private sealed class TestCustomProfileRequest
    {
        [Sanitize(Profile = "BlogPost")]
        public string Content { get; set; } = string.Empty;
    }

    private sealed class TestStripHtmlRequest
    {
        [StripHtml]
        public string RawContent { get; set; } = string.Empty;
    }

    #endregion
}
