using Encina.FluentValidation;
using Encina.Testing.FsCheck;
using Encina.Testing.Shouldly;
using Encina.Validation;
using FluentValidation;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Encina.PropertyTests.Validation.FluentValidation;

/// <summary>
/// Property-based tests for FluentValidation provider invariants.
/// Tests validation behavior using generated test data.
/// </summary>
public sealed class ValidationInvariantProperties : PropertyTestBase
{
    #region Test Request Types

    /// <summary>
    /// Test request with validation rules.
    /// </summary>
    public sealed record TestUserCommand(string Name, string Email, int Age) : ICommand<string>;

    /// <summary>
    /// Validator for TestUserCommand.
    /// </summary>
    public sealed class TestUserCommandValidator : AbstractValidator<TestUserCommand>
    {
        public TestUserCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MaximumLength(100).WithMessage("Name must not exceed 100 characters.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Email must be a valid email address.");

            RuleFor(x => x.Age)
                .InclusiveBetween(1, 150).WithMessage("Age must be between 1 and 150.");
        }
    }

    /// <summary>
    /// Test request without validation rules.
    /// </summary>
    public sealed record TestSimpleCommand(string Value) : ICommand<string>;

    #endregion

    #region Setup

    private static IValidationProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddEncinaFluentValidation(typeof(TestUserCommandValidator).Assembly);
        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IValidationProvider>();
    }

    private static IRequestContext CreateContext()
    {
        return RequestContext.Create(Guid.NewGuid().ToString());
    }

    /// <summary>
    /// Synchronously validates a request using the given provider.
    /// FsCheck's Prop.ForAll does not support async lambdas, so we must block here.
    /// This helper is ONLY used for property tests that require generated values.
    /// Tests with constant/deterministic inputs should use async [Fact] methods instead.
    /// Rule of thumb: use <see cref="ValidateSync{TRequest}"/> with [EncinaProperty] for tests
    /// with generated/random inputs; use <c>async Task</c> methods with [Fact] for deterministic tests.
    /// The blocking is acceptable in these property tests because:
    /// 1. The validation operations are CPU-bound and complete quickly
    /// 2. Property tests run sequentially within each test method
    /// 3. The blocking call is isolated to test code, not production code
    /// </summary>
    private static global::Encina.Validation.ValidationResult ValidateSync<TRequest>(
        IValidationProvider provider,
        TRequest request,
        IRequestContext context)
        where TRequest : notnull
    {
        return provider.ValidateAsync(request, context, CancellationToken.None)
            .AsTask()
            .GetAwaiter()
            .GetResult();
    }

    #endregion

    #region Null Input Invariants

    [Fact]
    public async Task ValidateAsync_NullRequest_ThrowsArgumentNullException()
    {
        var provider = CreateProvider();
        var context = CreateContext();

        await Should.ThrowAsync<ArgumentNullException>(
            () => provider.ValidateAsync<TestUserCommand>(null!, context, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task ValidateAsync_NullContext_ThrowsArgumentNullException()
    {
        var provider = CreateProvider();
        var request = new TestUserCommand("Valid", "valid@test.com", 25);

        await Should.ThrowAsync<ArgumentNullException>(
            () => provider.ValidateAsync(request, null!, CancellationToken.None).AsTask());
    }

    #endregion

    #region Validation Idempotency

    [EncinaProperty]
    public Property ValidateAsync_SameRequestMultipleTimes_ReturnsSameResult()
    {
        var validNameGen = Arb.From(Gen.Elements("John", "Jane", "Alice", "Bob"));
        var validEmailGen = Arb.From(Gen.Elements("john@test.com", "jane@test.com", "alice@test.com", "bob@test.com"));
        var validAgeGen = Arb.From(Gen.Choose(1, 150));

        return Prop.ForAll(validNameGen, validEmailGen, validAgeGen, (name, email, age) =>
        {
            var provider = CreateProvider();
            var context = CreateContext();
            var request = new TestUserCommand(name, email, age);

            var result1 = ValidateSync(provider, request, context);
            var result2 = ValidateSync(provider, request, context);
            var result3 = ValidateSync(provider, request, context);

            return result1.IsValid == result2.IsValid &&
                   result2.IsValid == result3.IsValid &&
                   result1.Errors.Length == result2.Errors.Length &&
                   result2.Errors.Length == result3.Errors.Length;
        });
    }

    [Fact]
    public async Task ValidateAsync_InvalidRequestMultipleTimes_ReturnsSameErrorCount()
    {
        var provider = CreateProvider();
        var context = CreateContext();
        var request = new TestUserCommand("", "invalid", -5); // Multiple failures

        var result1 = await provider.ValidateAsync(request, context, CancellationToken.None);
        var result2 = await provider.ValidateAsync(request, context, CancellationToken.None);

        result1.IsInvalid.ShouldBeTrue();
        result2.IsInvalid.ShouldBeTrue();
        result1.Errors.Length.ShouldBe(result2.Errors.Length);
    }

    #endregion

    #region Error Aggregation

    [EncinaProperty]
    public Property ValidateAsync_MultipleFailures_CapturesAllErrors()
    {
        // Generate variety of invalid names (empty, whitespace-only)
        var invalidNameGen = Arb.From(Gen.OneOf(
            Gen.Constant(string.Empty),
            Gen.Elements(" ", "\t", "\r\n", "  ", "\t\t")));

        // Generate variety of invalid emails (empty, whitespace, missing @, malformed)
        var invalidEmailGen = Arb.From(Gen.OneOf(
            Gen.Constant(string.Empty),
            Gen.Elements(
                " ", "\t",
                "notanemail", "missing-at-sign",
                "no@tld", "@nodomain",
                "user@", "@@double.at")));

        var invalidAgeGen = Arb.From(Gen.Choose(-100, 0));

        return Prop.ForAll(invalidNameGen, invalidEmailGen, invalidAgeGen, (name, email, age) =>
        {
            var provider = CreateProvider();
            var context = CreateContext();
            var request = new TestUserCommand(name, email, age);

            var result = ValidateSync(provider, request, context);

            // Should have errors for multiple invalid fields (Name, Email, Age)
            return result.IsInvalid && result.Errors.Length >= 2;
        });
    }

    [Fact]
    public async Task ValidateAsync_FieldLevelErrors_IncludePropertyName()
    {
        var provider = CreateProvider();
        var context = CreateContext();
        var request = new TestUserCommand("", "valid@test.com", 25); // Only Name is invalid

        var result = await provider.ValidateAsync(request, context, CancellationToken.None);

        result.IsInvalid.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task ValidateAsync_AllErrorsHaveMessages()
    {
        var provider = CreateProvider();
        var context = CreateContext();
        var request = new TestUserCommand("", "invalid", -10);

        var result = await provider.ValidateAsync(request, context, CancellationToken.None);

        result.IsInvalid.ShouldBeTrue();
        result.Errors.ShouldAllBe(e => !string.IsNullOrEmpty(e.ErrorMessage));
    }

    #endregion

    #region Valid Request Invariants

    [EncinaProperty]
    public Property ValidateAsync_ValidRequest_ReturnsSuccess()
    {
        var validNameGen = Arb.From(Gen.Elements("John", "Jane", "Alice", "Bob", "Charlie"));
        var validEmailGen = Arb.From(Gen.Elements("john@test.com", "jane@example.com", "alice@domain.org"));
        var validAgeGen = Arb.From(Gen.Choose(1, 150));

        return Prop.ForAll(validNameGen, validEmailGen, validAgeGen, (name, email, age) =>
        {
            var provider = CreateProvider();
            var context = CreateContext();
            var request = new TestUserCommand(name, email, age);

            var result = ValidateSync(provider, request, context);

            return result.IsValid && result.Errors.IsEmpty;
        });
    }

    [QuickProperty]
    public Property ValidateAsync_RequestWithoutValidator_ReturnsSuccess()
    {
        var valueGen = Arb.From(Gen.Elements("test", "value", "data", string.Empty));

        return Prop.ForAll(valueGen, value =>
        {
            var provider = CreateProvider();
            var context = CreateContext();
            var request = new TestSimpleCommand(value);

            var result = ValidateSync(provider, request, context);

            return result.IsValid;
        });
    }

    #endregion

    #region ValidationResult Invariants

    [Fact]
    public async Task ValidationResult_IsValidAndIsInvalid_AreMutuallyExclusive()
    {
        var provider = CreateProvider();
        var context = CreateContext();
        var request = new TestUserCommand("Valid", "valid@test.com", 25);

        var result = await provider.ValidateAsync(request, context, CancellationToken.None);

        result.IsValid.ShouldNotBe(result.IsInvalid);
    }

    [Fact]
    public async Task ValidationResult_InvalidResult_HasAtLeastOneError()
    {
        var provider = CreateProvider();
        var context = CreateContext();
        var request = new TestUserCommand("", "", 0);

        var result = await provider.ValidateAsync(request, context, CancellationToken.None);

        result.IsValid.ShouldBeFalse();
        result.Errors.Length.ShouldBeGreaterThanOrEqualTo(1);
    }

    #endregion

    #region ServiceCollection Extension Invariants

    [Fact]
    public void AddEncinaFluentValidation_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection? services = null;

        var action = () => services!.AddEncinaFluentValidation(typeof(TestUserCommandValidator).Assembly);

        action.ShouldThrow<ArgumentNullException>();
    }

    [Fact]
    public void AddEncinaFluentValidation_NullAssemblies_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();

        var action = () => services.AddEncinaFluentValidation(null!);

        action.ShouldThrow<ArgumentNullException>();
    }

    [Fact]
    public void AddEncinaFluentValidation_EmptyAssemblies_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();

        var action = () => services.AddEncinaFluentValidation(Array.Empty<System.Reflection.Assembly>());

        action.ShouldThrow<ArgumentNullException>();
    }

    [EncinaProperty]
    public Property AddEncinaFluentValidation_WithLifetime_RegistersProviderCorrectly()
    {
        var lifetimeGen = Arb.From(Gen.Elements(ServiceLifetime.Singleton, ServiceLifetime.Scoped, ServiceLifetime.Transient));

        return Prop.ForAll(lifetimeGen, lifetime =>
        {
            var services = new ServiceCollection();
            services.AddEncinaFluentValidation(lifetime, typeof(TestUserCommandValidator).Assembly);
            var provider = services.BuildServiceProvider();

            var validationProvider = provider.GetService<IValidationProvider>();

            return validationProvider is not null;
        });
    }

    [Fact]
    public void AddEncinaFluentValidation_WithLifetime_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection? services = null;

        var action = () => services!.AddEncinaFluentValidation(ServiceLifetime.Scoped, typeof(TestUserCommandValidator).Assembly);

        action.ShouldThrow<ArgumentNullException>();
    }

    [Fact]
    public void AddEncinaFluentValidation_WithLifetime_NullAssemblies_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();

        var action = () => services.AddEncinaFluentValidation(ServiceLifetime.Scoped, null!);

        action.ShouldThrow<ArgumentNullException>();
    }

    [Fact]
    public void AddEncinaFluentValidation_WithLifetime_EmptyAssemblies_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();

        var action = () => services.AddEncinaFluentValidation(ServiceLifetime.Scoped, Array.Empty<System.Reflection.Assembly>());

        action.ShouldThrow<ArgumentNullException>();
    }

    [Fact]
    public void AddEncinaFluentValidation_RegistersValidationOrchestrator()
    {
        var services = new ServiceCollection();
        services.AddEncinaFluentValidation(typeof(TestUserCommandValidator).Assembly);
        using var provider = services.BuildServiceProvider();

        var orchestrator = provider.GetService<ValidationOrchestrator>();

        orchestrator.ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaFluentValidation_RegistersValidators()
    {
        var services = new ServiceCollection();
        services.AddEncinaFluentValidation(typeof(TestUserCommandValidator).Assembly);
        using var provider = services.BuildServiceProvider();

        var validators = provider.GetServices<IValidator<TestUserCommand>>().ToList();

        validators.Count.ShouldBeGreaterThan(0);
    }

    #endregion
}
