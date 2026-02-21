# Input Sanitization and Output Encoding in Encina

Encina.Security.Sanitization provides automatic, attribute-based input sanitization and output encoding at the CQRS pipeline level, preventing XSS, SQL injection, command injection, and other OWASP Top 10 injection attacks.

## Table of Contents

1. [Overview](#overview)
2. [The Problem](#the-problem)
3. [The Solution](#the-solution)
4. [Quick Start](#quick-start)
5. [Input Sanitization Attributes](#input-sanitization-attributes)
6. [Output Encoding Attributes](#output-encoding-attributes)
7. [Sanitization Profiles](#sanitization-profiles)
8. [Custom Profiles](#custom-profiles)
9. [Configuration Reference](#configuration-reference)
10. [Pipeline Order](#pipeline-order)
11. [Observability](#observability)
12. [Health Check](#health-check)
13. [Error Handling](#error-handling)
14. [Testing](#testing)
15. [Troubleshooting](#troubleshooting)
16. [FAQ](#faq)

---

## Overview

Encina.Security.Sanitization separates security concerns from business logic through two complementary pipeline behaviors:

| Component | Description |
|-----------|-------------|
| **`ISanitizer`** | Context-aware input sanitization (HTML, SQL, Shell, JSON, XML) |
| **`IOutputEncoder`** | Context-aware output encoding (HTML, JavaScript, URL, CSS) |
| **`ISanitizationProfile`** | Immutable profile defining allowed tags, attributes, protocols |
| **`SanitizationOrchestrator`** | Discovers annotated properties and applies sanitization |
| **`InputSanitizationPipelineBehavior`** | Pre-handler — sanitizes request properties before handler |
| **`OutputEncodingPipelineBehavior`** | Post-handler — encodes response properties after handler |
| **`SanitizationHealthCheck`** | Verifies sanitization services are registered and operational |

**Key characteristics**:

- **Attribute-based** — decorate properties with `[SanitizeHtml]`, `[EncodeForHtml]`, etc.
- **Context-aware** — different sanitization strategies for different attack vectors
- **Railway Oriented Programming** — errors return `Either<EncinaError, T>`, no exceptions
- **Zero overhead** for requests without sanitization attributes
- **Compiled delegates** — cached property accessors for high performance
- **Thread-safe** — stateless sanitizer, concurrent-safe property caches

---

## The Problem

Input sanitization and output encoding logic pollutes business handlers:

```csharp
// Without Encina — security logic mixed with business logic
public class CreatePostHandler : ICommandHandler<CreatePostCommand, PostId>
{
    private readonly IHtmlSanitizer _sanitizer;

    public async ValueTask<Either<EncinaError, PostId>> Handle(
        CreatePostCommand command, IRequestContext context, CancellationToken ct)
    {
        // Security concerns mixed with business logic
        var safeTitle = HttpUtility.HtmlEncode(command.Title);
        var safeContent = _sanitizer.Sanitize(command.Content);
        var safeSql = command.SearchTerm.Replace("'", "''");

        var post = new Post(safeTitle, safeContent);
        // ... save post
    }
}
```

Every handler must know about HTML encoding, SQL escaping, and XSS prevention — violating Single Responsibility.

---

## The Solution

With Encina, declare sanitization requirements on properties and let the pipeline handle everything:

```csharp
// With Encina — clean separation of concerns
public sealed record CreatePostCommand(
    [property: SanitizeHtml] string Title,
    [property: SanitizeHtml] string Content,
    [property: SanitizeSql] string SearchTerm
) : ICommand<PostId>;

// Handler has zero sanitization awareness
public class CreatePostHandler : ICommandHandler<CreatePostCommand, PostId>
{
    public async ValueTask<Either<EncinaError, PostId>> Handle(
        CreatePostCommand command, IRequestContext context, CancellationToken ct)
    {
        // command.Title and command.Content are already sanitized
        // command.SearchTerm has SQL injection patterns removed
        var post = new Post(command.Title, command.Content);
        // ... save post
    }
}
```

---

## Quick Start

### 1. Install

```bash
dotnet add package Encina.Security.Sanitization
```

### 2. Register Services

```csharp
services.AddEncinaSanitization(options =>
{
    options.AddHealthCheck = true;
    options.EnableTracing = true;
});
```

### 3. Decorate Properties

```csharp
// Input sanitization
public sealed record CreatePostCommand(
    [property: SanitizeHtml] string Title,
    [property: SanitizeHtml] string Content
) : ICommand<PostId>;

// Output encoding
public sealed record PostResponse(
    [property: EncodeForHtml] string Title,
    [property: EncodeForHtml] string Content
);
```

---

## Input Sanitization Attributes

### `[SanitizeHtml]` — HTML Sanitization

Sanitizes HTML input using HtmlSanitizer (Ganss.Xss), stripping dangerous tags and attributes while preserving safe HTML.

```csharp
public sealed record CreatePostCommand(
    [property: SanitizeHtml] string Content   // "<script>alert('xss')</script><p>Safe</p>" → "<p>Safe</p>"
) : ICommand<PostId>;
```

**What it removes**: `<script>`, `<iframe>`, `<object>`, `<embed>`, `onclick`, `onerror`, `onload`, `javascript:` URIs.

### `[SanitizeSql]` — SQL Injection Prevention

Defense-in-depth sanitization for scenarios where parameterized queries are not possible (dynamic column names, ORDER BY clauses).

```csharp
public sealed record SearchCommand(
    [property: SanitizeSql] string SortColumn  // "name; DROP TABLE users--" → "name DROP TABLE users"
) : IQuery<SearchResult>;
```

**What it does**: Escapes `'` → `''`, removes `--`, `/* */`, `;`, `xp_*` patterns.

> **Important**: Parameterized queries are always the preferred defense. Use `[SanitizeSql]` as an additional layer.

### `[StripHtml]` — Strip All HTML

Removes all HTML tags, leaving only text content.

```csharp
public sealed record CreateCommentCommand(
    [property: StripHtml] string Text   // "<b>Hello</b> <script>bad</script>" → "Hello "
) : ICommand<CommentId>;
```

### `[Sanitize(Profile = "...")]` — Custom Profile

Uses a named profile registered in `SanitizationOptions`.

```csharp
public sealed record CreateArticleCommand(
    [property: Sanitize(Profile = "BlogPost")] string Content
) : ICommand<ArticleId>;
```

---

## Output Encoding Attributes

### `[EncodeForHtml]` — HTML Entity Encoding

Encodes `<`, `>`, `&`, `"`, `'` as HTML entities.

```csharp
public sealed record UserResponse(
    [property: EncodeForHtml] string DisplayName  // "<script>" → "&lt;script&gt;"
);
```

### `[EncodeForJavaScript]` — JavaScript Encoding

Encodes characters using `\uXXXX` Unicode escapes for safe inclusion in JavaScript strings.

```csharp
public sealed record ConfigResponse(
    [property: EncodeForJavaScript] string Value
);
```

### `[EncodeForUrl]` — URL Encoding

Percent-encodes characters per RFC 3986 for safe inclusion in URLs.

```csharp
public sealed record RedirectResponse(
    [property: EncodeForUrl] string ReturnPath
);
```

---

## Sanitization Profiles

Built-in profiles cover common use cases:

| Profile | Allowed Tags | Use Case |
|---------|-------------|----------|
| **`None`** | All (pass-through) | When sanitization is handled elsewhere |
| **`StrictText`** | None (strips all HTML) | Plain text fields, comments |
| **`BasicFormatting`** | `<b>`, `<i>`, `<em>`, `<strong>`, `<br>`, `<p>` | Simple formatted text |
| **`RichText`** | Headings, links, images, lists, tables | Blog posts, articles |
| **`Markdown`** | All Markdown-rendered HTML including `<code>`, `<pre>` | Markdown content |

### Profile Properties

Each profile defines:

| Property | Description |
|----------|-------------|
| `AllowedTags` | HTML tags that are preserved (others are stripped) |
| `AllowedAttributes` | HTML attributes that are preserved |
| `AllowedProtocols` | URL protocols allowed in `href`/`src` (e.g., `https`, `mailto`) |
| `StripComments` | Whether to remove HTML comments |
| `StripScripts` | Whether to remove `<script>` elements |

---

## Custom Profiles

Register custom profiles via the fluent builder:

```csharp
services.AddEncinaSanitization(options =>
{
    options.AddProfile("BlogPost", profile =>
    {
        profile.AllowTags("p", "h1", "h2", "h3", "a", "img", "ul", "ol", "li");
        profile.AllowAttributes("href", "src", "alt", "title");
        profile.AllowProtocols("https", "mailto");
        profile.WithStripScripts(true);
        profile.WithStripComments(true);
    });

    options.AddProfile("PlainText", profile =>
    {
        // No tags allowed — strips everything
        profile.WithStripScripts(true);
        profile.WithStripComments(true);
    });
});
```

Use with the `[Sanitize]` attribute:

```csharp
public sealed record CreatePostCommand(
    [property: Sanitize(Profile = "BlogPost")] string Content,
    [property: Sanitize(Profile = "PlainText")] string Summary
) : ICommand<PostId>;
```

---

## Configuration Reference

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `SanitizeAllStringInputs` | `bool` | `false` | Auto-sanitize all string properties (uses `DefaultProfile`) |
| `DefaultProfile` | `ISanitizationProfile?` | `null` (falls back to `StrictText`) | Default profile for auto-sanitization |
| `EncodeAllOutputs` | `bool` | `false` | Auto-encode all string response properties as HTML |
| `AddHealthCheck` | `bool` | `false` | Register sanitization health check |
| `EnableTracing` | `bool` | `false` | Emit OpenTelemetry traces |
| `EnableMetrics` | `bool` | `false` | Emit OpenTelemetry metrics |

### Global Auto-Sanitization

When `SanitizeAllStringInputs = true`, ALL string properties on request objects are sanitized using the `DefaultProfile` (or `StrictText` if not set), unless they have explicit sanitization attributes:

```csharp
services.AddEncinaSanitization(options =>
{
    options.SanitizeAllStringInputs = true;
    options.DefaultProfile = SanitizationProfiles.StrictText;
});
```

### Advanced HTML Sanitizer Configuration

For settings not covered by profiles, configure the underlying Ganss.Xss HtmlSanitizer directly:

```csharp
options.UseHtmlSanitizer(sanitizer =>
{
    sanitizer.AllowedCssProperties.Add("color");
    sanitizer.AllowedCssProperties.Add("font-size");
});
```

---

## Pipeline Order

The sanitization behaviors integrate with the Encina CQRS pipeline:

```
Request → Validation → Security → [Input Sanitization] → Handler → [Output Encoding] → Response
```

1. **Input Sanitization** (pre-handler): Cleans request properties before the handler sees them
2. **Output Encoding** (post-handler): Encodes response properties before returning to the caller

---

## Observability

### Tracing

When `EnableTracing = true`, activities are emitted via the `Encina.Security.Sanitization` ActivitySource:

| Activity | Kind | Description |
|----------|------|-------------|
| `Sanitization.Input` | Internal | Input sanitization pipeline invocation |
| `Sanitization.Output` | Internal | Output encoding pipeline invocation |

**Tags**:

| Tag | Description |
|-----|-------------|
| `sanitization.request_type` | Full type name of the request/response |
| `sanitization.operation` | `sanitize` or `encode` |
| `sanitization.type` | Sanitization type (Html, Sql, Shell) |
| `sanitization.profile` | Profile name (for custom profiles) |
| `sanitization.property_count` | Number of properties processed |
| `sanitization.outcome` | `success` or `failure` |

### Metrics

When `EnableMetrics = true`, instruments are emitted via the `Encina.Security.Sanitization` Meter:

| Instrument | Type | Description |
|------------|------|-------------|
| `sanitization.operations` | Counter | Total sanitization/encoding operations |
| `sanitization.properties.processed` | Counter | Total properties sanitized or encoded |
| `sanitization.failures` | Counter | Total failed operations |
| `sanitization.duration` | Histogram (ms) | Duration of operations |

### Logging

9 structured log events using `LoggerMessage` source generation (zero-allocation):

| EventId | Level | Message |
|---------|-------|---------|
| 1 | Debug | Input sanitization started |
| 2 | Debug | Input sanitization completed |
| 3 | Warning | Input sanitization property failed |
| 4 | Debug | Output encoding started |
| 5 | Debug | Output encoding completed |
| 6 | Warning | Output encoding property failed |
| 7 | Debug | Input sanitization skipped (no properties) |
| 8 | Debug | Output encoding skipped (no properties) |
| 9 | Debug | Auto-sanitization started |

---

## Health Check

Opt-in health check verifying sanitization services are resolvable from DI:

```csharp
options.AddHealthCheck = true;
```

**Name**: `encina-sanitization`
**Tags**: `encina`, `sanitization`, `ready`

The health check verifies `ISanitizer`, `IOutputEncoder`, and `SanitizationOrchestrator` can be resolved from the service provider.

---

## Error Handling

All sanitization errors follow Railway Oriented Programming — failures return `Either<EncinaError, T>` instead of throwing exceptions.

| Error Code | Description | Metadata |
|------------|-------------|----------|
| `sanitization.profile_not_found` | Requested profile not registered | `profileName`, `stage` |
| `sanitization.property_error` | Sanitization of a property failed | `propertyName`, `stage` |

Example error handling:

```csharp
var result = orchestrator.Sanitize(command);

result.Match(
    Left: error =>
    {
        var code = error.GetCode().IfNone(string.Empty);
        var details = error.GetDetails();
        logger.LogWarning("Sanitization failed: {Code} - {Message}", code, error.Message);
    },
    Right: _ => { /* Success — command properties are sanitized in-place */ }
);
```

---

## Testing

### Test Coverage

| Test Type | Count | Description |
|-----------|-------|-------------|
| **Unit Tests** | 235 | All components, behaviors, profiles, attributes |
| **Guard Tests** | 21 | Null parameter validation on all public methods |
| **Property Tests** | 27 | FsCheck invariant verification (HTML never contains `<script>`, SQL escaping, encoding safety) |
| **Total** | 283 | |

### Testing with Custom Implementations

Register custom implementations before calling `AddEncinaSanitization()`:

```csharp
// Custom sanitizer replaces default
services.AddSingleton<ISanitizer, MySanitizer>();
services.AddEncinaSanitization(); // TryAdd won't override your registration
```

---

## Troubleshooting

### Properties Not Being Sanitized

1. Ensure `AddEncinaSanitization()` is called in DI registration
2. Verify properties have sanitization attributes (`[SanitizeHtml]`, etc.)
3. Check that properties are `string` type — non-string properties are skipped
4. If using auto-sanitization, verify `SanitizeAllStringInputs = true`

### Custom Profile Not Found

1. Verify profile name matches (case-insensitive)
2. Ensure `AddProfile()` is called in the options delegate
3. Check the error: `sanitization.profile_not_found` includes the `profileName` in metadata

### Performance Considerations

- Property discovery is cached via `ConcurrentDictionary` — first call per type has reflection cost, subsequent calls are O(1)
- Compiled delegates (Expression trees) are used for property get/set — no reflection overhead at runtime
- Requests without sanitization attributes skip the pipeline entirely (zero overhead)

---

## FAQ

**Q: Does this replace parameterized queries?**
No. `[SanitizeSql]` is defense-in-depth for scenarios where parameterized queries cannot be used (dynamic column names, ORDER BY clauses). Always use parameterized queries as your primary defense.

**Q: Can I use custom sanitization logic?**
Yes. Implement `ISanitizer` and register it before calling `AddEncinaSanitization()`. The `TryAdd` semantics will preserve your registration.

**Q: What happens if sanitization fails?**
The pipeline returns `Either<EncinaError, T>` with a `sanitization.property_error` code. The handler is not invoked.

**Q: Is thread-safe?**
Yes. `DefaultSanitizer` is registered as a singleton and is stateless. Property caches use `ConcurrentDictionary`. The `SanitizationOrchestrator` is scoped per request.

**Q: Does it affect performance?**
Negligible. Property caches eliminate reflection overhead after the first call. Requests without sanitization attributes bypass all checks. HTML sanitization (Ganss.Xss) is the most expensive operation but is only invoked for properties marked with `[SanitizeHtml]`.
