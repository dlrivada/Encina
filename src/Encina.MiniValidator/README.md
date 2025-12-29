# Encina.MiniValidator

[![NuGet](https://img.shields.io/nuget/v/Encina.MiniValidator.svg)](https://www.nuget.org/packages/Encina.MiniValidator)
[![License](https://img.shields.io/github/license/dlrivada/Encina)](https://github.com/dlrivada/Encina/blob/main/LICENSE)

MiniValidation integration for Encina with Railway Oriented Programming (ROP) support. **Ultra-lightweight** (~20KB) validation perfect for Minimal APIs.

## Features

- 🚦 **Automatic Request Validation** - Validates requests before handler execution
- 🛤️ **ROP Integration** - Returns validation failures as `Left<EncinaError>` for functional error handling
- 🪶 **Ultra-Lightweight** - MiniValidation is only ~20KB (vs 500KB FluentValidation)
- 🎯 **Zero Boilerplate** - No need to manually call validators in handlers
- ⚡ **Minimal APIs Optimized** - Designed specifically for lightweight scenarios
- 🧪 **Fully Tested** - Comprehensive test coverage

## Installation

```bash
dotnet add package Encina.MiniValidator
```

**Lightweight dependency**: MiniValidation (~20KB)

## Quick Start

### 1. Decorate Your Requests with Validation Attributes

MiniValidation uses Data Annotations under the hood, so you can use standard validation attributes:

```csharp
using System.ComponentModel.DataAnnotations;

public record CreateUser : ICommand<UserId>
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; init; } = string.Empty;

    [Required(ErrorMessage = "Name is required")]
    [MinLength(3, ErrorMessage = "Name must be at least 3 characters")]
    public string Name { get; init; } = string.Empty;

    [Range(18, 120, ErrorMessage = "Age must be between 18 and 120")]
    public int Age { get; init; }
}
```

### 2. Register MiniValidation

```csharp
using Encina.MiniValidator;

var builder = WebApplication.CreateBuilder(args);

// Register Encina
builder.Services.AddEncina(cfg =>
{
    // Configuration if needed
}, typeof(CreateUser).Assembly);

// Register MiniValidation
builder.Services.AddMiniValidation();

var app = builder.Build();
```

### 3. Write Your Handler (Validation is Automatic!)

```csharp
public sealed class CreateUserHandler : ICommandHandler<CreateUser, UserId>
{
    private readonly IUserRepository _users;

    public CreateUserHandler(IUserRepository users) => _users = users;

    public async Task<Either<EncinaError, UserId>> Handle(
        CreateUser request,
        CancellationToken ct)
    {
        // request is GUARANTEED to be valid here!
        // No need to manually validate - the behavior did it for you

        var user = new User(request.Email, request.Name, request.Age);
        await _users.Save(user, ct);
        return Right<EncinaError, UserId>(user.Id);
    }
}
```

### 4. Use in Minimal APIs

Perfect integration with Minimal APIs:

```csharp
app.MapPost("/users", async (CreateUser request, IEncina Encina) =>
{
    var result = await Encina.Send(request);

    return result.Match(
        Right: userId => Results.Created($"/users/{userId}", userId),
        Left: error => Results.BadRequest(new
        {
            error.Message,
            errors = error.Message.Split(", ").Skip(1) // Extract validation errors
        })
    );
});
```

## Why MiniValidation?

### Perfect for Minimal APIs

MiniValidation was designed specifically for Minimal APIs where:

- You want validation without ceremony
- Package size matters (serverless, Lambda, containers)
- You don't need complex validation logic
- Data Annotations are sufficient

### Size Comparison

| Library | Size | Best For |
|---------|------|----------|
| **MiniValidation** | ~20KB | Minimal APIs, microservices, serverless |
| **Data Annotations** | 0KB (built-in) | Legacy code, simple apps |
| **FluentValidation** | ~500KB | Enterprise apps, complex validation |

### When to Use MiniValidation

✅ **Use MiniValidation When:**

- Building **Minimal APIs** with ASP.NET Core
- Package size is a constraint (serverless, Lambda)
- Simple validation rules are sufficient
- You want lightweight validation without FluentValidation overhead
- Microservices where every KB counts

❌ **Consider FluentValidation When:**

- Complex multi-field validation logic
- Async validation (database checks, external APIs)
- Composable validation rules
- Enterprise applications with complex business rules

## Supported Validation Attributes

MiniValidation supports all standard Data Annotations attributes:

### Basic Validation

- `[Required]` - Property must have a value
- `[StringLength]` - String length constraints
- `[MinLength]` / `[MaxLength]` - Length constraints
- `[Range]` - Numeric range validation

### Format Validation

- `[EmailAddress]` - Valid email format
- `[Phone]` - Valid phone number format
- `[Url]` - Valid URL format
- `[CreditCard]` - Valid credit card number
- `[RegularExpression]` - Pattern matching

### Comparison

- `[Compare]` - Compare with another property

### Custom Validation

- Custom `ValidationAttribute` implementations

## Advanced Usage

### Custom Validation Attributes

Create your own validation attributes:

```csharp
public class StartsWithAttribute : ValidationAttribute
{
    private readonly string _prefix;

    public StartsWithAttribute(string prefix)
    {
        _prefix = prefix;
    }

    protected override ValidationResult? IsValid(
        object? value,
        ValidationContext validationContext)
    {
        if (value is string str &&
            str.StartsWith(_prefix, StringComparison.OrdinalIgnoreCase))
        {
            return ValidationResult.Success;
        }

        return new ValidationResult(
            ErrorMessage ?? $"Value must start with '{_prefix}'");
    }
}

// Use it
public record CreateProject : ICommand<ProjectId>
{
    [StartsWith("PRJ-")]
    public string ProjectCode { get; init; } = string.Empty;
}
```

### Validation Error Structure

When validation fails:

```csharp
var result = await Encina.Send(new CreateUser
{
    Email = "invalid",
    Name = "",
    Age = 15
});

result.Match(
    Right: userId => Console.WriteLine($"Created: {userId}"),
    Left: error =>
    {
        // error.Message contains all validation errors
        // Example: "Validation failed for CreateUser with 3 error(s): Name: Name is required, Email: Invalid email format, Age: Age must be between 18 and 120"
        Console.WriteLine(error.Message);
    }
);
```

### Minimal API Error Handling

Extract validation errors for API responses:

```csharp
app.MapPost("/users", async (CreateUser request, IEncina Encina) =>
{
    var result = await Encina.Send(request);

    return result.Match(
        Right: userId => Results.Created($"/users/{userId}", userId),
        Left: error =>
        {
            // Parse validation errors from message
            var errorParts = error.Message
                .Split(": ", 2)[1]  // Skip "Validation failed for..."
                .Split(", ")
                .Select(e => e.Split(": "))
                .Where(parts => parts.Length == 2)
                .ToDictionary(parts => parts[0], parts => new[] { parts[1] });

            return Results.ValidationProblem(errorParts);
        }
    );
});
```

## Comparison: MiniValidation vs Others

| Feature | MiniValidation | Data Annotations | FluentValidation |
|---------|----------------|------------------|------------------|
| **Size** | ~20KB | 0KB (built-in) | ~500KB |
| **Setup** | `AddMiniValidation()` | `AddDataAnnotationsValidation()` | `AddFluentValidation(...)` |
| **Syntax** | Attributes | Attributes | Fluent API |
| **Async** | ❌ No | ⚠️ Limited | ✅ Full support |
| **Complex Rules** | ❌ Limited | ❌ Limited | ✅ Excellent |
| **Performance** | ⚡ Fast | ⚡ Fast | ⚠️ Slower (compiled) |
| **Best For** | Minimal APIs | Legacy/simple | Enterprise/complex |

## How It Works

The centralized `Encina.Validation.ValidationPipelineBehavior<TRequest, TResponse>` intercepts all requests:

1. **Validate**: Calls `MiniValidator.TryValidate` on the request via `MiniValidationProvider`
2. **Collect Errors**: Aggregates all validation failures
3. **Short-Circuit**: If validation fails, returns `Left<EncinaError>` with error message
4. **Continue**: If validation passes, calls the next pipeline step (handler)

> **Note**: MiniValidator uses the shared `ValidationPipelineBehavior` from `Encina.Validation` through the Orchestrator pattern. The `MiniValidationProvider` implements `IValidationProvider` to integrate MiniValidation with the centralized behavior.

## Performance

- **Minimal Overhead** - MiniValidation is optimized for speed
- **Small Package Size** - ~20KB vs 500KB FluentValidation
- **Fast Validation** - Uses optimized Data Annotations internally
- **Zero Allocation** when validation passes
- **Perfect for Serverless** - Small cold start footprint

## Real-World Example: Minimal API CRUD

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEncina(cfg => { }, typeof(Program).Assembly);
builder.Services.AddMiniValidation();

var app = builder.Build();

// Create
app.MapPost("/users", async (CreateUser cmd, IEncina Encina) =>
{
    var result = await Encina.Send(cmd);
    return result.Match(
        Right: id => Results.Created($"/users/{id}", id),
        Left: err => Results.BadRequest(err.Message)
    );
});

// Update
app.MapPut("/users/{id}", async (int id, UpdateUser cmd, IEncina Encina) =>
{
    var result = await Encina.Send(cmd with { Id = id });
    return result.Match(
        Right: _ => Results.NoContent(),
        Left: err => Results.BadRequest(err.Message)
    );
});

app.Run();

// Commands with validation
public record CreateUser : ICommand<int>
{
    [Required, EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required, MinLength(3), MaxLength(100)]
    public string Name { get; init; } = string.Empty;

    [Range(18, 120)]
    public int Age { get; init; }
}

public record UpdateUser : ICommand<Unit>
{
    public int Id { get; init; }

    [Required, EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required, MinLength(3), MaxLength(100)]
    public string Name { get; init; } = string.Empty;
}
```

## Best Practices

### ✅ DO

- **Use for Minimal APIs** - MiniValidation was built for this
- **Keep validation simple** - Use standard attributes
- **Validate format and required fields** - What Data Annotations does best
- **Consider package size** - Perfect for serverless/containers
- **Combine with other behaviors** - Works great with FluentValidation on complex commands

### ❌ DON'T

- **Don't use for complex validation** - Use FluentValidation instead
- **Don't do async validation** - Not supported by MiniValidation
- **Don't validate business logic** - That belongs in handlers/domain
- **Don't use if size doesn't matter** - FluentValidation offers more features

## Migration Between Validation Libraries

Since all three libraries use the same pipeline behavior pattern, you can easily switch:

### From Data Annotations to MiniValidation

```csharp
// Before
services.AddDataAnnotationsValidation();

// After
services.AddMiniValidation();

// No changes to request classes needed!
```

### Mix and Match

You can even use different validation libraries for different commands:

```csharp
// Use MiniValidation for simple commands
services.AddMiniValidation();

// Use FluentValidation for complex commands
services.AddEncinaFluentValidation(typeof(ComplexValidator).Assembly);

// Both work together in the pipeline!
```

## Testing

Testing is simple since validation happens automatically:

```csharp
[Fact]
public async Task CreateUserHandler_CreatesUser_WhenRequestIsValid()
{
    // Arrange
    var repository = new InMemoryUserRepository();
    var handler = new CreateUserHandler(repository);
    var request = new CreateUser
    {
        Email = "john@example.com",
        Name = "John Doe",
        Age = 25
    };

    // Act - validation already happened in pipeline
    var result = await handler.Handle(request, CancellationToken.None);

    // Assert
    result.IsRight.ShouldBeTrue();
}
```

## Contributing

Contributions are welcome! Please read the [contributing guidelines](https://github.com/dlrivada/Encina/blob/main/CONTRIBUTING.md) first.

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/dlrivada/Encina/blob/main/LICENSE) file for details.

## Related Packages

- **Encina** - Core Encina library
- **Encina.FluentValidation** - FluentValidation integration (for complex validation)
- **Encina.DataAnnotations** - Data Annotations integration (zero dependencies)
- **Encina.AspNetCore** - ASP.NET Core integration (coming soon)

## Support

- 📖 [Documentation](https://github.com/dlrivada/Encina)
- 🐛 [Issue Tracker](https://github.com/dlrivada/Encina/issues)
- 💬 [Discussions](https://github.com/dlrivada/Encina/discussions)

## Why "MiniValidation"?

MiniValidation (by [@DamianEdwards](https://github.com/DamianEdwards)) is a minimalist validation library created specifically for .NET Minimal APIs. It's:

- **Tiny** - ~20KB
- **Fast** - Optimized for performance
- **Simple** - Uses Data Annotations without ceremony
- **Modern** - Built for .NET 6+ Minimal APIs

Perfect fit for Encina's philosophy of giving developers **choice** without compromising on quality.
