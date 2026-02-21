# Encina.Security.Sanitization

[![NuGet](https://img.shields.io/nuget/v/Encina.Security.Sanitization.svg)](https://www.nuget.org/packages/Encina.Security.Sanitization)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://opensource.org/licenses/MIT)

**Attribute-based input sanitization and output encoding for Encina CQRS pipelines.**

Encina.Security.Sanitization provides automatic sanitization of request properties and encoding of response properties at the CQRS pipeline level. Decorate properties with `[SanitizeHtml]`, `[SanitizeSql]`, `[EncodeForHtml]`, and let the pipeline handle XSS prevention, SQL injection defense, and OWASP Top 10 injection protection transparently.

## Key Features

- **5 input sanitization contexts** — HTML (via HtmlSanitizer), SQL, Shell, JSON, XML
- **5 output encoding contexts** — HTML, HTML attribute, JavaScript, URL, CSS
- **Attribute-based** — `[SanitizeHtml]`, `[SanitizeSql]`, `[StripHtml]`, `[EncodeForHtml]`, `[EncodeForJavaScript]`, `[EncodeForUrl]`
- **5 built-in profiles** — None, StrictText, BasicFormatting, RichText, Markdown
- **Custom profiles** — fluent builder for allowed tags, attributes, protocols
- **Railway Oriented Programming** — all operations return `Either<EncinaError, T>`
- **Zero overhead** for requests without sanitization attributes
- **OpenTelemetry** tracing and metrics (opt-in)
- **Health check** with DI verification (opt-in)

## Quick Start

```csharp
// 1. Register services
services.AddEncinaSanitization(options =>
{
    options.AddHealthCheck = true;
    options.EnableTracing = true;

    options.AddProfile("BlogPost", profile =>
    {
        profile.AllowTags("p", "h1", "h2", "a", "img");
        profile.AllowAttributes("href", "src", "alt");
        profile.AllowProtocols("https", "mailto");
    });
});

// 2. Decorate request properties
public sealed record CreatePostCommand(
    [property: SanitizeHtml] string Title,
    [property: Sanitize(Profile = "BlogPost")] string Content
) : ICommand<PostId>;

// 3. Decorate response properties
public sealed record PostResponse(
    [property: EncodeForHtml] string Title,
    [property: EncodeForHtml] string Content
);

// 4. Handler has zero sanitization awareness
public class CreatePostHandler : ICommandHandler<CreatePostCommand, PostId>
{
    public async ValueTask<Either<EncinaError, PostId>> Handle(
        CreatePostCommand command, IRequestContext context, CancellationToken ct)
    {
        // command.Title is already sanitized (dangerous HTML stripped)
        // command.Content is sanitized with BlogPost profile
        var post = new Post(command.Title, command.Content);
        await _repository.AddAsync(post, ct);
        return Right(post.Id);
    }
}
```

## Global Auto-Sanitization

Sanitize all string properties automatically without attributes:

```csharp
services.AddEncinaSanitization(options =>
{
    options.SanitizeAllStringInputs = true;
    options.DefaultProfile = SanitizationProfiles.StrictText;
    options.EncodeAllOutputs = true;
});
```

## Custom Sanitizer

Register custom implementations before `AddEncinaSanitization()`:

```csharp
services.AddSingleton<ISanitizer, MyCustomSanitizer>();
services.AddSingleton<IOutputEncoder, MyCustomEncoder>();
services.AddEncinaSanitization(); // TryAdd won't override your registrations
```

## Documentation

- [Sanitization Guide](../../docs/features/sanitization.md) — comprehensive documentation with examples
- [CHANGELOG](../../CHANGELOG.md) — version history and release notes

## Dependencies

- `Encina` (core abstractions)
- `HtmlSanitizer` (Ganss.Xss — HTML sanitization engine)
- `Microsoft.Extensions.DependencyInjection.Abstractions`
- `Microsoft.Extensions.Logging.Abstractions`
- `Microsoft.Extensions.Diagnostics.HealthChecks`
- `Microsoft.Extensions.Options`

## License

MIT License. See [LICENSE](../../LICENSE) for details.
