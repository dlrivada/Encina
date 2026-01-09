using System.ComponentModel.DataAnnotations;
using Encina.MiniValidator;
using Encina.Testing.FsCheck;
using Encina.Testing.Shouldly;
using Encina.Validation;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Encina.MiniValidator.PropertyTests;

/// <summary>
/// Property-based tests for MiniValidator provider invariants.
/// Tests lightweight validation behavior for Minimal API scenarios using generated test data.
/// </summary>
/// <remarks>
/// FsCheck's Prop.ForAll does not support async lambdas, so validation calls use
/// synchronous blocking (.AsTask().GetAwaiter().GetResult()).
/// Each test creates and disposes its own ServiceProvider for proper isolation.
/// These tests are marked with Category=PropertyTests trait for CI filtering.
/// To exclude: dotnet test --filter "Category!=PropertyTests"
/// To run only: dotnet test --filter "Category=PropertyTests"
/// </remarks>
[Properties(MaxTest = 100)]
[Trait("Category", "PropertyTests")]
public sealed class ValidationInvariantProperties : PropertyTestBase
{

    #region Test Request Types

    /// <summary>
    /// Lightweight request for Minimal API validation.
    /// </summary>
    public sealed record TestProductCommand : ICommand<string>
    {
        [Required(ErrorMessage = "Name is required.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters.")]
        public string Name { get; init; } = string.Empty;

        [Range(0.01, 999999.99, ErrorMessage = "Price must be between 0.01 and 999999.99.")]
        public decimal Price { get; init; }

        [Required(ErrorMessage = "SKU is required.")]
        [RegularExpression(@"^[A-Z]{3}-\d{4}$", ErrorMessage = "SKU must be in format ABC-1234.")]
        public string Sku { get; init; } = string.Empty;
    }

    /// <summary>
    /// Simple request without validation (health check scenario).
    /// </summary>
    public sealed record TestHealthCheckCommand : ICommand<string>;

    /// <summary>
    /// Request with multiple validation rules on same property.
    /// </summary>
    public sealed record TestPasswordCommand : ICommand<string>
    {
        [Required(ErrorMessage = "Password is required.")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
        [RegularExpression(@".*[A-Z].*\d.*|.*\d.*[A-Z].*", ErrorMessage = "Password must contain at least one digit and one uppercase letter.")]
        public string Password { get; init; } = string.Empty;
    }

    #endregion

    #region Setup

    private static ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddMiniValidation();
        return services.BuildServiceProvider();
    }

    private static IRequestContext CreateContext()
    {
        return RequestContext.Create(Guid.NewGuid().ToString());
    }

    #endregion

    #region Null Input Invariants

    [Fact]
    public async Task ValidateAsync_NullRequest_ThrowsArgumentNullException()
    {
        using var serviceProvider = CreateServiceProvider();
        var provider = serviceProvider.GetRequiredService<IValidationProvider>();
        var context = CreateContext();

        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await provider.ValidateAsync<TestProductCommand>(null!, context, CancellationToken.None));
    }

    [Fact]
    public async Task ValidateAsync_NullContext_ThrowsArgumentNullException()
    {
        using var serviceProvider = CreateServiceProvider();
        var provider = serviceProvider.GetRequiredService<IValidationProvider>();
        var request = new TestProductCommand { Name = "Valid", Price = 9.99m, Sku = "ABC-1234" };

        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await provider.ValidateAsync(request, null!, CancellationToken.None));
    }

    #endregion

    #region Validation Idempotency

    [EncinaProperty]
    public Property ValidateAsync_SameRequestMultipleTimes_ReturnsSameResult()
    {
        var validNameGen = Arb.From(Gen.Elements("Widget", "Gadget", "Product", "Item"));
        var validPriceGen = Arb.From(Gen.Choose(1, 9999).Select(x => (decimal)x / 100));
        var validSkuGen = Arb.From(Gen.Elements("ABC-1234", "XYZ-5678", "DEF-9012"));

        return Prop.ForAll(validNameGen, validPriceGen, validSkuGen, (name, price, sku) =>
        {
            using var serviceProvider = CreateServiceProvider();
            var provider = serviceProvider.GetRequiredService<IValidationProvider>();
            var context = CreateContext();
            var request = new TestProductCommand { Name = name, Price = price, Sku = sku };

            var result1 = provider.ValidateAsync(request, context, CancellationToken.None).AsTask().Result;
            var result2 = provider.ValidateAsync(request, context, CancellationToken.None).AsTask().Result;
            var result3 = provider.ValidateAsync(request, context, CancellationToken.None).AsTask().Result;

            return result1.IsValid == result2.IsValid &&
                   result2.IsValid == result3.IsValid &&
                   result1.Errors.Length == result2.Errors.Length &&
                   result2.Errors.Length == result3.Errors.Length;
        });
    }

    [Fact]
    public async Task ValidateAsync_InvalidRequestMultipleTimes_ReturnsSameErrorCount()
    {
        using var serviceProvider = CreateServiceProvider();
        var provider = serviceProvider.GetRequiredService<IValidationProvider>();
        var context = CreateContext();
        var request = new TestProductCommand { Name = "", Price = -5m, Sku = "invalid" };

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
        var emptyStringGen = Arb.From(Gen.Constant(string.Empty));
        var invalidPriceGen = Arb.From(Gen.Choose(-100, 0).Select(x => (decimal)x));
        var invalidSkuGen = Arb.From(Gen.Elements("bad", "invalid", "123"));

        return Prop.ForAll(emptyStringGen, invalidPriceGen, invalidSkuGen, (name, price, sku) =>
        {
            using var serviceProvider = CreateServiceProvider();
            var provider = serviceProvider.GetRequiredService<IValidationProvider>();
            var context = CreateContext();
            var request = new TestProductCommand { Name = name, Price = price, Sku = sku };

            var result = provider.ValidateAsync(request, context, CancellationToken.None).AsTask().Result;

            // Should have errors for multiple invalid fields
            return result.IsInvalid && result.Errors.Length >= 2;
        });
    }

    [Fact]
    public async Task ValidateAsync_FieldLevelErrors_IncludePropertyName()
    {
        using var serviceProvider = CreateServiceProvider();
        var provider = serviceProvider.GetRequiredService<IValidationProvider>();
        var context = CreateContext();
        var request = new TestProductCommand { Name = "", Price = 9.99m, Sku = "ABC-1234" };

        var result = await provider.ValidateAsync(request, context, CancellationToken.None);

        result.IsInvalid.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task ValidateAsync_AllErrorsHaveMessages()
    {
        using var serviceProvider = CreateServiceProvider();
        var provider = serviceProvider.GetRequiredService<IValidationProvider>();
        var context = CreateContext();
        var request = new TestProductCommand { Name = "", Price = -10m, Sku = "bad" };

        var result = await provider.ValidateAsync(request, context, CancellationToken.None);

        result.IsInvalid.ShouldBeTrue();
        result.Errors.ShouldAllBe(e => !string.IsNullOrEmpty(e.ErrorMessage));
    }

    #endregion

    #region Minimal API Scenarios

    [Fact]
    public async Task ValidateAsync_HealthCheckCommand_ReturnsSuccess()
    {
        using var serviceProvider = CreateServiceProvider();
        var provider = serviceProvider.GetRequiredService<IValidationProvider>();
        var context = CreateContext();
        var request = new TestHealthCheckCommand();

        var result = await provider.ValidateAsync(request, context, CancellationToken.None);

        result.IsValid.ShouldBeTrue();
    }

    [EncinaProperty]
    public Property ValidateAsync_LightweightValidation_HandlesInputVariants()
    {
        var validNameGen = Arb.From(Gen.Elements("Product A", "Item B", "Widget C"));
        var validPriceGen = Arb.From(Gen.Choose(1, 999999).Select(x => (decimal)x / 100));
        var validSkuGen = Arb.From(Gen.Elements("ABC-1234", "XYZ-0001", "DEF-9999"));

        return Prop.ForAll(validNameGen, validPriceGen, validSkuGen, (name, price, sku) =>
        {
            using var serviceProvider = CreateServiceProvider();
            var provider = serviceProvider.GetRequiredService<IValidationProvider>();
            var context = CreateContext();
            var request = new TestProductCommand { Name = name, Price = price, Sku = sku };

            var result = provider.ValidateAsync(request, context, CancellationToken.None).AsTask().Result;

            return result.IsValid;
        });
    }

    #endregion

    #region Multiple Errors Same Property

    [EncinaProperty]
    public Property ValidateAsync_MultipleErrorsSameProperty_CapturesAll()
    {
        var shortPasswordGen = Arb.From(Gen.Elements("abc", "12", "A1"));

        return Prop.ForAll(shortPasswordGen, password =>
        {
            using var serviceProvider = CreateServiceProvider();
            var provider = serviceProvider.GetRequiredService<IValidationProvider>();
            var context = CreateContext();
            var request = new TestPasswordCommand { Password = password };

            var result = provider.ValidateAsync(request, context, CancellationToken.None).AsTask().Result;

            // Should have errors (short, possibly missing digit/uppercase)
            return result.IsInvalid && result.Errors.Length >= 1;
        });
    }

    [QuickProperty]
    public Property ValidateAsync_ValidComplexPassword_ReturnsSuccess()
    {
        var validPasswordGen = Arb.From(Gen.Elements("Password1", "SecurePass99", "Test1234A"));

        return Prop.ForAll(validPasswordGen, password =>
        {
            using var serviceProvider = CreateServiceProvider();
            var provider = serviceProvider.GetRequiredService<IValidationProvider>();
            var context = CreateContext();
            var request = new TestPasswordCommand { Password = password };

            var result = provider.ValidateAsync(request, context, CancellationToken.None).AsTask().Result;

            return result.IsValid;
        });
    }

    #endregion

    #region Valid Request Invariants

    [EncinaProperty]
    public Property ValidateAsync_ValidRequest_ReturnsSuccess()
    {
        var validNameGen = Arb.From(Gen.Elements("Widget Pro", "Gadget X", "Super Item"));
        var validPriceGen = Arb.From(Gen.Choose(1, 9999).Select(x => (decimal)x / 100));
        var validSkuGen = Arb.From(Gen.Elements("ABC-1234", "XYZ-5678", "DEF-0000"));

        return Prop.ForAll(validNameGen, validPriceGen, validSkuGen, (name, price, sku) =>
        {
            using var serviceProvider = CreateServiceProvider();
            var provider = serviceProvider.GetRequiredService<IValidationProvider>();
            var context = CreateContext();
            var request = new TestProductCommand { Name = name, Price = price, Sku = sku };

            var result = provider.ValidateAsync(request, context, CancellationToken.None).AsTask().Result;

            return result.IsValid && result.Errors.IsEmpty;
        });
    }

    #endregion

    #region ValidationResult Invariants

    [Fact]
    public async Task ValidationResult_IsValidAndIsInvalid_AreMutuallyExclusive()
    {
        using var serviceProvider = CreateServiceProvider();
        var provider = serviceProvider.GetRequiredService<IValidationProvider>();
        var context = CreateContext();
        var request = new TestProductCommand { Name = "Valid", Price = 9.99m, Sku = "ABC-1234" };

        var result = await provider.ValidateAsync(request, context, CancellationToken.None);

        (result.IsValid != result.IsInvalid).ShouldBeTrue();
    }

    [Fact]
    public async Task ValidationResult_InvalidResult_HasAtLeastOneError()
    {
        using var serviceProvider = CreateServiceProvider();
        var provider = serviceProvider.GetRequiredService<IValidationProvider>();
        var context = CreateContext();
        var request = new TestProductCommand { Name = "", Price = 0m, Sku = "" };

        var result = await provider.ValidateAsync(request, context, CancellationToken.None);

        // Invalid result must have at least one error
        result.IsValid.ShouldBeFalse();
        result.Errors.Length.ShouldBeGreaterThanOrEqualTo(1);
    }

    #endregion

    #region ServiceCollection Extension Invariants

    [Fact]
    public void AddMiniValidation_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection? services = null;

        var action = () => services!.AddMiniValidation();

        action.ShouldThrow<ArgumentNullException>();
    }

    [Fact]
    public void AddMiniValidation_RegistersValidationProvider()
    {
        var services = new ServiceCollection();
        services.AddMiniValidation();
        using var provider = services.BuildServiceProvider();

        var validationProvider = provider.GetService<IValidationProvider>();

        validationProvider.ShouldNotBeNull();
        validationProvider.ShouldBeOfType<MiniValidationProvider>();
    }

    [Fact]
    public void AddMiniValidation_RegistersValidationOrchestrator()
    {
        var services = new ServiceCollection();
        services.AddMiniValidation();
        using var provider = services.BuildServiceProvider();

        var orchestrator = provider.GetService<ValidationOrchestrator>();

        orchestrator.ShouldNotBeNull();
    }

    #endregion
}
