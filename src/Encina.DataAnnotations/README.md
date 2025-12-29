# Encina.DataAnnotations

[![NuGet](https://img.shields.io/nuget/v/Encina.DataAnnotations.svg)](https://www.nuget.org/packages/Encina.DataAnnotations)
[![License](https://img.shields.io/github/license/dlrivada/Encina)](https://github.com/dlrivada/Encina/blob/main/LICENSE)

Data Annotations validation for Encina with Railway Oriented Programming (ROP) support. **Zero external dependencies** - uses built-in .NET validation.

## Features

- 🚦 **Automatic Request Validation** - Validates requests before handler execution
- 🛤️ **ROP Integration** - Returns validation failures as `Left<EncinaError>` for functional error handling
- 📦 **Zero Dependencies** - Uses System.ComponentModel.DataAnnotations (built-in .NET)
- 🎯 **Zero Boilerplate** - No need to manually call validators in handlers
- 🔄 **Context Enrichment** - Passes correlation ID, user ID, and tenant ID to validators
- 🧪 **Fully Tested** - Comprehensive test coverage

## Installation

```bash
dotnet add package Encina.DataAnnotations
```

**No additional dependencies required** - uses built-in .NET validation.

## Quick Start

### 1. Decorate Your Requests with Validation Attributes

```csharp
using System.ComponentModel.DataAnnotations;

public record CreateUser : ICommand<UserId>
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; init; } = string.Empty;

    [Required(ErrorMessage = "Name is required")]
    [MinLength(3, ErrorMessage = "Name must be at least 3 characters")]
    [MaxLength(100, ErrorMessage = "Name too long")]
    public string Name { get; init; } = string.Empty;

    [Range(18, 120, ErrorMessage = "Age must be between 18 and 120")]
    public int Age { get; init; }
}
```

### 2. Register Data Annotations Validation

```csharp
using Encina.DataAnnotations;

var services = new ServiceCollection();

// Register Encina
services.AddEncina(cfg =>
{
    // Configuration if needed
}, typeof(CreateUser).Assembly);

// Register Data Annotations validation
services.AddDataAnnotationsValidation();
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

### 4. Handle Validation Errors Functionally

```csharp
var result = await Encina.Send(new CreateUser
{
    Email = "invalid-email",
    Name = "",
    Age = 15
});

result.Match(
    Right: userId => Console.WriteLine($"User created: {userId}"),
    Left: error =>
    {
        // Validation failed - error contains ValidationException
        Console.WriteLine($"Validation failed: {error.Message}");

        error.Exception.IfSome(ex =>
        {
            if (ex is ValidationException validationEx &&
                ex.Data["ValidationResults"] is List<ValidationResult> results)
            {
                foreach (var failure in results)
                {
                    Console.WriteLine($"  - {string.Join(", ", failure.MemberNames)}: {failure.ErrorMessage}");
                }
            }
        });
    }
);
```

## Supported Validation Attributes

All built-in .NET validation attributes are supported:

### Basic Validation

- `[Required]` - Property must have a value
- `[StringLength]` - String length constraints (min/max)
- `[MinLength]` / `[MaxLength]` - Length constraints
- `[Range]` - Numeric range validation

### Format Validation

- `[EmailAddress]` - Valid email format
- `[Phone]` - Valid phone number format
- `[Url]` - Valid URL format
- `[CreditCard]` - Valid credit card number
- `[RegularExpression]` - Pattern matching

### Comparison Validation

- `[Compare]` - Compare with another property (e.g., password confirmation)

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

### Context-Aware Validation

Access request context metadata inside custom validators:

```csharp
public class UniqueEmailAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(
        object? value,
        ValidationContext validationContext)
    {
        // Access Encina context metadata
        var correlationId = validationContext.Items["CorrelationId"] as string;
        var userId = validationContext.Items["UserId"] as string;
        var tenantId = validationContext.Items["TenantId"] as string;

        // Use for tenant-specific validation
        if (tenantId is not null && value is string email)
        {
            // Check uniqueness within tenant
            // (In real code, inject IUserRepository via service provider)
        }

        return ValidationResult.Success;
    }
}
```

### Combining with Other Validation Libraries

You can combine Data Annotations with other validation libraries:

```csharp
// Register both Data Annotations and FluentValidation
services.AddDataAnnotationsValidation();
services.AddEncinaFluentValidation(typeof(CreateUser).Assembly);

// Data Annotations runs first (pipeline order)
// Then FluentValidation runs
// Both must pass for handler to execute
```

## Validation Failure Structure

When validation fails, the error structure looks like this:

```csharp
EncinaError
{
    Message = "Validation failed for CreateUser with 3 error(s): Email is required, Name is required, Age must be between 18 and 120.",
    Exception = Some(ValidationException
    {
        Message = "Validation failed for CreateUser with 3 error(s): ...",
        Data =
        {
            ["ValidationResults"] = List<ValidationResult>
            [
                { MemberNames = ["Email"], ErrorMessage = "Email is required" },
                { MemberNames = ["Name"], ErrorMessage = "Name is required" },
                { MemberNames = ["Age"], ErrorMessage = "Age must be between 18 and 120" }
            ]
        }
    })
}
```

## How It Works

The centralized `Encina.Validation.ValidationPipelineBehavior<TRequest, TResponse>` intercepts all requests:

1. **Validate**: Calls `Validator.TryValidateObject` with `validateAllProperties: true` via `DataAnnotationsValidationProvider`
2. **Enrich Context**: Passes correlation ID, user ID, tenant ID to validation context
3. **Aggregate Failures**: Collects all validation errors
4. **Short-Circuit**: If validation fails, returns `Left<EncinaError>` with `ValidationException`
5. **Continue**: If validation passes, calls the next pipeline step (handler)

> **Note**: Data Annotations uses the shared `ValidationPipelineBehavior` from `Encina.Validation` through the Orchestrator pattern. The `DataAnnotationsValidationProvider` implements `IValidationProvider` to integrate Data Annotations with the centralized behavior.

## Performance

- **Zero External Dependencies** - no additional NuGet packages required
- **Zero Allocation** when validation passes (fast path)
- **Reflection-based** - slower than FluentValidation's compiled expressions, but minimal overhead
- **Transient Lifetime** - behavior instance created per request (stateless)

## Integration with ASP.NET Core

Example of extracting validation errors in minimal APIs:

```csharp
app.MapPost("/users", async (CreateUser request, IEncina Encina) =>
{
    var result = await Encina.Send(request);

    return result.Match(
        Right: userId => Results.Created($"/users/{userId}", userId),
        Left: error => error.Exception.Match(
            Some: ex => ex is ValidationException validationEx &&
                ex.Data["ValidationResults"] is List<ValidationResult> results
                ? Results.ValidationProblem(results
                    .GroupBy(r => r.MemberNames.FirstOrDefault() ?? "")
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(r => r.ErrorMessage ?? "").ToArray()))
                : Results.Problem(error.Message),
            None: () => Results.Problem(error.Message)
        )
    );
});
```

## When to Use Data Annotations

### ✅ Use Data Annotations When

- **Simple validation rules** - Required fields, email format, ranges
- **Zero dependencies required** - You want built-in .NET validation
- **Attribute-based validation** - You prefer declarative validation on models
- **Legacy code** - Your models already have Data Annotations
- **Quick prototyping** - Fast setup with minimal code

### ❌ Consider FluentValidation When

- **Complex validation logic** - Multi-field validation, business rules
- **Async validation** - Database checks, external API calls
- **Separation of concerns** - You want validators separate from models
- **Composable rules** - Building complex validators from smaller ones
- **Better testing** - Validators as independent, testable units

## Comparison: Data Annotations vs FluentValidation

| Feature | Data Annotations | FluentValidation |
|---------|------------------|------------------|
| **Setup** | ✅ Zero dependencies | Requires NuGet package |
| **Syntax** | Attributes on properties | Fluent API in separate class |
| **Async Validation** | ⚠️ Limited support | ✅ Full async support |
| **Complex Rules** | ❌ Difficult | ✅ Easy |
| **Testing** | ⚠️ Harder (attributes) | ✅ Easy (separate classes) |
| **Separation** | ❌ Coupled to model | ✅ Separate validators |
| **Performance** | ⚠️ Reflection-based | ✅ Compiled expressions |
| **Best For** | Simple CRUD, prototypes | Enterprise apps, CQRS/DDD |

## Best Practices

### ✅ DO

- **Use for simple validation** - Required fields, formats, ranges
- **Combine with FluentValidation** - Use both for layered validation
- **Set clear error messages** - Always provide `ErrorMessage` parameter
- **Use built-in attributes** - Leverage `[EmailAddress]`, `[Range]`, etc.
- **Keep it simple** - Don't put business logic in validation attributes

### ❌ DON'T

- **Don't use for complex validation** - Multi-field rules, business logic
- **Don't do async validation** - Data Annotations doesn't handle it well
- **Don't validate inside handlers** - Let the behavior do it
- **Don't create overly complex custom attributes** - Use FluentValidation instead

## Testing

Testing handlers is simpler because validation is already done:

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

    // Act - no need to worry about validation in handler tests!
    var result = await handler.Handle(request, CancellationToken.None);

    // Assert
    result.IsRight.ShouldBeTrue();
    repository.Users.ShouldHaveSingleItem();
}
```

Test validation separately using `Validator.TryValidateObject`:

```csharp
[Fact]
public void CreateUser_Validation_Fails_WhenEmailIsInvalid()
{
    // Arrange
    var request = new CreateUser
    {
        Email = "invalid-email",
        Name = "John",
        Age = 25
    };
    var context = new ValidationContext(request);
    var results = new List<ValidationResult>();

    // Act
    var isValid = Validator.TryValidateObject(
        request,
        context,
        results,
        validateAllProperties: true);

    // Assert
    isValid.ShouldBeFalse();
    results.ShouldContain(r => r.MemberNames.Contains("Email"));
}
```

## Migration from Manual Validation

**Before** (manual validation in handler):

```csharp
public sealed class CreateUserHandler : ICommandHandler<CreateUser, UserId>
{
    public async Task<Either<EncinaError, UserId>> Handle(
        CreateUser request,
        CancellationToken ct)
    {
        // ❌ Manual validation boilerplate
        var context = new ValidationContext(request);
        var results = new List<ValidationResult>();
        if (!Validator.TryValidateObject(request, context, results, true))
        {
            var errors = string.Join(", ", results.Select(r => r.ErrorMessage));
            return EncinaError.New($"Validation failed: {errors}");
        }

        var user = new User(request.Email, request.Name, request.Age);
        await _users.Save(user, ct);
        return user.Id;
    }
}
```

**After** (automatic validation):

```csharp
public sealed class CreateUserHandler : ICommandHandler<CreateUser, UserId>
{
    public async Task<Either<EncinaError, UserId>> Handle(
        CreateUser request,
        CancellationToken ct)
    {
        // ✅ No validation code needed - automatically validated!
        var user = new User(request.Email, request.Name, request.Age);
        await _users.Save(user, ct);
        return user.Id;
    }
}
```

## Contributing

Contributions are welcome! Please read the [contributing guidelines](https://github.com/dlrivada/Encina/blob/main/CONTRIBUTING.md) first.

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/dlrivada/Encina/blob/main/LICENSE) file for details.

## Related Packages

- **Encina** - Core Encina library
- **Encina.FluentValidation** - FluentValidation integration (for complex validation)
- **Encina.MiniValidator** - Lightweight validation for Minimal APIs
- **Encina.AspNetCore** - ASP.NET Core integration

## Support

- 📖 [Documentation](https://github.com/dlrivada/Encina)
- 🐛 [Issue Tracker](https://github.com/dlrivada/Encina/issues)
- 💬 [Discussions](https://github.com/dlrivada/Encina/discussions)
