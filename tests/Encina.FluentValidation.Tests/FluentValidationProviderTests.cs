using Encina.TestInfrastructure.PropertyTests;
using Encina.Validation;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Encina.FluentValidation.Tests;

/// <summary>
/// Tests for <see cref="FluentValidationProvider"/> implementation.
/// </summary>
public sealed class FluentValidationProviderTests
{
    #region Test Request Types and Validators

    /// <summary>
    /// Request with required field validation.
    /// </summary>
    private sealed record RequiredFieldRequest(string Name) : ICommand<string>;

    private sealed class RequiredFieldValidator : AbstractValidator<RequiredFieldRequest>
    {
        public RequiredFieldValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.");
        }
    }

    /// <summary>
    /// Request with length constraint validation.
    /// </summary>
    private sealed record LengthConstraintRequest(string Username) : ICommand<string>;

    private sealed class LengthConstraintValidator : AbstractValidator<LengthConstraintRequest>
    {
        public LengthConstraintValidator()
        {
            RuleFor(x => x.Username)
                .MinimumLength(3).WithMessage("Username must be at least 3 characters.")
                .MaximumLength(20).WithMessage("Username must not exceed 20 characters.");
        }
    }

    /// <summary>
    /// Request with format/email validation.
    /// </summary>
    private sealed record FormatValidationRequest(string Email) : ICommand<string>;

    private sealed class FormatValidationValidator : AbstractValidator<FormatValidationRequest>
    {
        public FormatValidationValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Email must be a valid email address.");
        }
    }

    /// <summary>
    /// Request with multiple validation rules.
    /// </summary>
    private sealed record MultipleRulesRequest(string Name, int Age, string Email) : ICommand<string>;

    private sealed class MultipleRulesValidator : AbstractValidator<MultipleRulesRequest>
    {
        public MultipleRulesValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.");
            RuleFor(x => x.Age).GreaterThan(0).WithMessage("Age must be positive.");
            RuleFor(x => x.Email).EmailAddress().WithMessage("Email must be valid.");
        }
    }

    /// <summary>
    /// Request without any validators registered.
    /// </summary>
    private sealed record NoValidatorRequest(string Value) : ICommand<string>;

    /// <summary>
    /// Request for testing parallel validator execution.
    /// </summary>
    private sealed record ParallelValidationRequest(string Value1, string Value2) : ICommand<string>;

    private sealed class ParallelValidator1 : AbstractValidator<ParallelValidationRequest>
    {
        public ParallelValidator1()
        {
            RuleFor(x => x.Value1).NotEmpty().WithMessage("Value1 is required.");
        }
    }

    private sealed class ParallelValidator2 : AbstractValidator<ParallelValidationRequest>
    {
        public ParallelValidator2()
        {
            RuleFor(x => x.Value2).NotEmpty().WithMessage("Value2 is required.");
        }
    }

    #endregion

    #region Successful Validation Tests

    [Fact]
    public async Task ValidateAsync_WithValidRequest_ShouldReturnSuccess()
    {
        // Arrange
        using var scope = CreateProvider<RequiredFieldRequest, RequiredFieldValidator>();
        var request = new RequiredFieldRequest("John");
        var context = new TestRequestContext();

        // Act
        var result = await scope.Provider.ValidateAsync(request, context, CancellationToken.None);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public async Task ValidateAsync_WithValidLengthConstraint_ShouldReturnSuccess()
    {
        // Arrange
        using var scope = CreateProvider<LengthConstraintRequest, LengthConstraintValidator>();
        var request = new LengthConstraintRequest("validuser");
        var context = new TestRequestContext();

        // Act
        var result = await scope.Provider.ValidateAsync(request, context, CancellationToken.None);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public async Task ValidateAsync_WithValidEmail_ShouldReturnSuccess()
    {
        // Arrange
        using var scope = CreateProvider<FormatValidationRequest, FormatValidationValidator>();
        var request = new FormatValidationRequest("user@example.com");
        var context = new TestRequestContext();

        // Act
        var result = await scope.Provider.ValidateAsync(request, context, CancellationToken.None);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public async Task ValidateAsync_WithAllValidFields_ShouldReturnSuccess()
    {
        // Arrange
        using var scope = CreateProvider<MultipleRulesRequest, MultipleRulesValidator>();
        var request = new MultipleRulesRequest("John", 25, "john@example.com");
        var context = new TestRequestContext();

        // Act
        var result = await scope.Provider.ValidateAsync(request, context, CancellationToken.None);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    #endregion

    #region Validation Failure Tests

    [Fact]
    public async Task ValidateAsync_WithEmptyRequiredField_ShouldReturnFailure()
    {
        // Arrange
        using var scope = CreateProvider<RequiredFieldRequest, RequiredFieldValidator>();
        var request = new RequiredFieldRequest("");
        var context = new TestRequestContext();

        // Act
        var result = await scope.Provider.ValidateAsync(request, context, CancellationToken.None);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.Length.ShouldBe(1);
        result.Errors[0].PropertyName.ShouldBe("Name");
        result.Errors[0].ErrorMessage.ShouldBe("Name is required.");
    }

    [Fact]
    public async Task ValidateAsync_WithTooShortUsername_ShouldReturnFailure()
    {
        // Arrange
        using var scope = CreateProvider<LengthConstraintRequest, LengthConstraintValidator>();
        var request = new LengthConstraintRequest("ab");
        var context = new TestRequestContext();

        // Act
        var result = await scope.Provider.ValidateAsync(request, context, CancellationToken.None);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.Length.ShouldBe(1);
        result.Errors[0].PropertyName.ShouldBe("Username");
        result.Errors[0].ErrorMessage.ShouldBe("Username must be at least 3 characters.");
    }

    [Fact]
    public async Task ValidateAsync_WithTooLongUsername_ShouldReturnFailure()
    {
        // Arrange
        using var scope = CreateProvider<LengthConstraintRequest, LengthConstraintValidator>();
        var request = new LengthConstraintRequest("thisusernameiswaytoolong");
        var context = new TestRequestContext();

        // Act
        var result = await scope.Provider.ValidateAsync(request, context, CancellationToken.None);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.Length.ShouldBe(1);
        result.Errors[0].PropertyName.ShouldBe("Username");
        result.Errors[0].ErrorMessage.ShouldBe("Username must not exceed 20 characters.");
    }

    [Fact]
    public async Task ValidateAsync_WithInvalidEmail_ShouldReturnFailure()
    {
        // Arrange
        using var scope = CreateProvider<FormatValidationRequest, FormatValidationValidator>();
        var request = new FormatValidationRequest("not-an-email");
        var context = new TestRequestContext();

        // Act
        var result = await scope.Provider.ValidateAsync(request, context, CancellationToken.None);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.Length.ShouldBe(1);
        result.Errors[0].PropertyName.ShouldBe("Email");
        result.Errors[0].ErrorMessage.ShouldBe("Email must be a valid email address.");
    }

    [Fact]
    public async Task ValidateAsync_WithMultipleInvalidFields_ShouldReturnAllErrors()
    {
        // Arrange
        using var scope = CreateProvider<MultipleRulesRequest, MultipleRulesValidator>();
        var request = new MultipleRulesRequest("", -5, "invalid");
        var context = new TestRequestContext();

        // Act
        var result = await scope.Provider.ValidateAsync(request, context, CancellationToken.None);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.Length.ShouldBe(3);
        result.Errors.ShouldContain(e => e.PropertyName == "Name" && e.ErrorMessage == "Name is required.");
        result.Errors.ShouldContain(e => e.PropertyName == "Age" && e.ErrorMessage == "Age must be positive.");
        result.Errors.ShouldContain(e => e.PropertyName == "Email" && e.ErrorMessage == "Email must be valid.");
    }

    #endregion

    #region No Validator Tests

    [Fact]
    public async Task ValidateAsync_WithNoValidatorRegistered_ShouldReturnSuccess()
    {
        // Arrange
        var services = new ServiceCollection();
        using var serviceProvider = services.BuildServiceProvider();
        var provider = new FluentValidationProvider(serviceProvider);
        var request = new NoValidatorRequest("any value");
        var context = new TestRequestContext();

        // Act
        var result = await provider.ValidateAsync(request, context, CancellationToken.None);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.ShouldBe(ValidationResult.Success);
    }

    #endregion

    #region Parallel Validator Execution Tests

    [Fact]
    public async Task ValidateAsync_WithMultipleValidators_ShouldExecuteAllValidators()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IValidator<ParallelValidationRequest>, ParallelValidator1>();
        services.AddSingleton<IValidator<ParallelValidationRequest>, ParallelValidator2>();
        using var serviceProvider = services.BuildServiceProvider();
        var provider = new FluentValidationProvider(serviceProvider);
        var request = new ParallelValidationRequest("", "");
        var context = new TestRequestContext();

        // Act
        var result = await provider.ValidateAsync(request, context, CancellationToken.None);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.Length.ShouldBeGreaterThanOrEqualTo(2);
        // Both validators should have contributed errors
        result.Errors.ShouldContain(e => e.PropertyName == "Value1");
        result.Errors.ShouldContain(e => e.PropertyName == "Value2");
    }

    [Fact]
    public async Task ValidateAsync_WithMultipleValidatorsAllPassing_ShouldReturnSuccess()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IValidator<ParallelValidationRequest>, ParallelValidator1>();
        services.AddSingleton<IValidator<ParallelValidationRequest>, ParallelValidator2>();
        using var serviceProvider = services.BuildServiceProvider();
        var provider = new FluentValidationProvider(serviceProvider);
        var request = new ParallelValidationRequest("value1", "value2");
        var context = new TestRequestContext();

        // Act
        var result = await provider.ValidateAsync(request, context, CancellationToken.None);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    #endregion

    #region Context Metadata Tests

    [Fact]
    public async Task ValidateAsync_ShouldPassContextMetadataToValidator()
    {
        // Arrange
        string? capturedCorrelationId = null;
        string? capturedUserId = null;
        string? capturedTenantId = null;

        var services = new ServiceCollection();
        services.AddSingleton<IValidator<RequiredFieldRequest>>(_ =>
        {
            var validator = new InlineValidator<RequiredFieldRequest>();
            validator.RuleFor(x => x.Name)
                .Custom((_, validationContext) =>
                {
                    capturedCorrelationId = validationContext.RootContextData.TryGetValue("CorrelationId", out var cid)
                        ? cid?.ToString()
                        : null;
                    capturedUserId = validationContext.RootContextData.TryGetValue("UserId", out var uid)
                        ? uid?.ToString()
                        : null;
                    capturedTenantId = validationContext.RootContextData.TryGetValue("TenantId", out var tid)
                        ? tid?.ToString()
                        : null;
                });
            return validator;
        });

        using var serviceProvider = services.BuildServiceProvider();
        var provider = new FluentValidationProvider(serviceProvider);
        var request = new RequiredFieldRequest("John");
        var context = new TestRequestContext { UserId = "user-123", TenantId = "tenant-abc" };

        // Act
        await provider.ValidateAsync(request, context, CancellationToken.None);

        // Assert
        capturedCorrelationId.ShouldBe("test-correlation");
        capturedUserId.ShouldBe("user-123");
        capturedTenantId.ShouldBe("tenant-abc");
    }

    #endregion

    #region Guard Clause Tests

    [Fact]
    public async Task ValidateAsync_WithNullRequest_ShouldThrowArgumentNullException()
    {
        // Arrange
        using var scope = CreateProvider<RequiredFieldRequest, RequiredFieldValidator>();
        var context = new TestRequestContext();

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await scope.Provider.ValidateAsync<RequiredFieldRequest>(null!, context, CancellationToken.None));
    }

    [Fact]
    public async Task ValidateAsync_WithNullContext_ShouldThrowArgumentNullException()
    {
        // Arrange
        using var scope = CreateProvider<RequiredFieldRequest, RequiredFieldValidator>();
        var request = new RequiredFieldRequest("John");

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await scope.Provider.ValidateAsync(request, null!, CancellationToken.None));
    }

    [Fact]
    public void Constructor_WithNullServiceProvider_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new FluentValidationProvider(null!));
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Disposable wrapper for FluentValidationProvider and its ServiceProvider.
    /// Ensures the ServiceProvider is disposed after test use.
    /// </summary>
    private readonly struct ProviderScope(FluentValidationProvider provider, ServiceProvider serviceProvider) : IDisposable
    {
        public FluentValidationProvider Provider { get; } = provider;

        public void Dispose() => serviceProvider.Dispose();
    }

    private static ProviderScope CreateProvider<TRequest, TValidator>()
        where TValidator : class, IValidator<TRequest>, new()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IValidator<TRequest>, TValidator>();
        var serviceProvider = services.BuildServiceProvider();
        return new ProviderScope(new FluentValidationProvider(serviceProvider), serviceProvider);
    }

    #endregion
}

