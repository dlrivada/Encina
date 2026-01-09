using System.ComponentModel.DataAnnotations;
using Encina.DataAnnotations;
using Encina.FluentValidation;
using Encina.MiniValidator;
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
using ValidationResult = Encina.Validation.ValidationResult;

namespace Encina.PropertyTests;

/// <summary>
/// Cross-provider property-based tests for validation invariants.
/// Tests that all validation providers behave consistently.
/// </summary>
public sealed class ValidationProviderProperties : PropertyTestBase, IDisposable
{
    private readonly ServiceProvider _fluentValidationServiceProvider;
    private readonly ServiceProvider _dataAnnotationsServiceProvider;
    private readonly ServiceProvider _miniValidatorServiceProvider;
    private readonly IValidationProvider _fluentValidationProvider;
    private readonly IValidationProvider _dataAnnotationsProvider;
    private readonly IValidationProvider _miniValidatorProvider;

    public ValidationProviderProperties()
    {
        var fluentServices = new ServiceCollection();
        fluentServices.AddEncinaFluentValidation(typeof(CommonTestCommandValidator).Assembly);
        _fluentValidationServiceProvider = fluentServices.BuildServiceProvider();
        _fluentValidationProvider = _fluentValidationServiceProvider.GetRequiredService<IValidationProvider>();

        var dataAnnotationsServices = new ServiceCollection();
        dataAnnotationsServices.AddDataAnnotationsValidation();
        _dataAnnotationsServiceProvider = dataAnnotationsServices.BuildServiceProvider();
        _dataAnnotationsProvider = _dataAnnotationsServiceProvider.GetRequiredService<IValidationProvider>();

        var miniValidatorServices = new ServiceCollection();
        miniValidatorServices.AddMiniValidation();
        _miniValidatorServiceProvider = miniValidatorServices.BuildServiceProvider();
        _miniValidatorProvider = _miniValidatorServiceProvider.GetRequiredService<IValidationProvider>();
    }

    public void Dispose()
    {
        _fluentValidationServiceProvider?.Dispose();
        _dataAnnotationsServiceProvider?.Dispose();
        _miniValidatorServiceProvider?.Dispose();
    }

    #region Test Request Types (Common across providers)

    /// <summary>
    /// Common test request with validation attributes (works with DataAnnotations and MiniValidator).
    /// </summary>
    public sealed record CommonTestCommand : ICommand<string>
    {
        [Required(ErrorMessage = "Name is required.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters.")]
        public string? Name { get; init; }

        [Required(ErrorMessage = "Value is required.")]
        public string? Value { get; init; }

        [Range(1, 1000, ErrorMessage = "Count must be between 1 and 1000.")]
        public int Count { get; init; }
    }

    /// <summary>
    /// FluentValidation validator for CommonTestCommand.
    /// </summary>
    public sealed class CommonTestCommandValidator : AbstractValidator<CommonTestCommand>
    {
        public CommonTestCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MaximumLength(100).WithMessage("Name must not exceed 100 characters.")
                .MinimumLength(2).WithMessage("Name must be at least 2 characters.");

            RuleFor(x => x.Value)
                .NotEmpty().WithMessage("Value is required.");

            RuleFor(x => x.Count)
                .InclusiveBetween(1, 1000).WithMessage("Count must be between 1 and 1000.");
        }
    }

    /// <summary>
    /// Simple request without validation rules.
    /// </summary>
    public sealed record NoValidationCommand(string Data) : ICommand<string>;

    #endregion

    #region Provider Factory Methods

    private IValidationProvider FluentValidationProvider => _fluentValidationProvider;

    private IValidationProvider DataAnnotationsProvider => _dataAnnotationsProvider;

    private IValidationProvider MiniValidatorProvider => _miniValidatorProvider;

    private static IRequestContext CreateContext()
    {
        return RequestContext.Create(Guid.NewGuid().ToString());
    }

    private IEnumerable<IValidationProvider> AllProviders()
    {
        yield return FluentValidationProvider;
        yield return DataAnnotationsProvider;
        yield return MiniValidatorProvider;
    }

    /// <summary>
    /// Provides provider names for Theory tests.
    /// Each provider is tested independently so failures are reported per-provider.
    /// </summary>
    public static TheoryData<string> ProviderNames => new()
    {
        "FluentValidation",
        "DataAnnotations",
        "MiniValidator"
    };

    /// <summary>
    /// Resolves a validation provider by name.
    /// </summary>
    private IValidationProvider GetProviderByName(string providerName) => providerName switch
    {
        "FluentValidation" => FluentValidationProvider,
        "DataAnnotations" => DataAnnotationsProvider,
        "MiniValidator" => MiniValidatorProvider,
        _ => throw new ArgumentException($"Unknown provider: {providerName}", nameof(providerName))
    };

    /// <summary>
    /// Synchronously validates a request using the given provider.
    /// FsCheck's Prop.ForAll does not support async lambdas, so we must block here.
    /// This is acceptable in property tests because:
    /// 1. The validation operations are CPU-bound and complete quickly
    /// 2. Property tests run sequentially within each test method
    /// 3. The blocking call is isolated to test code, not production code
    /// </summary>
    private static Validation.ValidationResult ValidateSync<TRequest>(
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

    #region Null Input Consistency

    [Theory]
    [MemberData(nameof(ProviderNames))]
    public async Task Provider_NullRequest_ThrowsArgumentNullException(string providerName)
    {
        // Arrange
        var provider = GetProviderByName(providerName);
        var context = CreateContext();

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await provider.ValidateAsync<CommonTestCommand>(null!, context, CancellationToken.None));
    }

    [Theory]
    [MemberData(nameof(ProviderNames))]
    public async Task Provider_NullContext_ThrowsArgumentNullException(string providerName)
    {
        // Arrange
        var provider = GetProviderByName(providerName);
        var request = new CommonTestCommand { Name = "Test", Value = "Data", Count = 10 };

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await provider.ValidateAsync(request, null!, CancellationToken.None));
    }

    #endregion

    #region Valid Request Consistency

    [EncinaProperty]
    public Property AllProviders_ValidRequest_ReturnsSuccess()
    {
        var providers = AllProviders().ToList();
        var validNameGen = Arb.From(Gen.Elements("ValidName", "TestItem", "DataValue"));
        var validValueGen = Arb.From(Gen.Elements("Value1", "Value2", "Value3"));
        var validCountGen = Arb.From(Gen.Choose(1, 1000));

        return Prop.ForAll(validNameGen, validValueGen, validCountGen, (name, value, count) =>
        {
            var context = CreateContext();
            var request = new CommonTestCommand { Name = name, Value = value, Count = count };

            foreach (var provider in providers)
            {
                var result = ValidateSync(provider, request, context);
                if (!result.IsValid)
                {
                    return false;
                }
            }

            return true;
        });
    }

    [EncinaProperty]
    public Property AllProviders_RequestWithoutValidator_ReturnsSuccess()
    {
        var providers = AllProviders().ToList();
        var dataGen = Arb.From(Gen.Elements("test", "data", "value", string.Empty));

        return Prop.ForAll(dataGen, data =>
        {
            var context = CreateContext();
            var request = new NoValidationCommand(data);

            foreach (var provider in providers)
            {
                var result = ValidateSync(provider, request, context);
                if (!result.IsValid)
                {
                    return false;
                }
            }

            return true;
        });
    }

    #endregion

    #region PropertyName in ValidationError Consistency

    [EncinaProperty]
    public Property AllProviders_FieldLevelError_IncludesPropertyName()
    {
        var providers = AllProviders().ToList();
        return Prop.ForAll(Arb.From(Gen.Choose(1, 100)), _ =>
        {
            var context = CreateContext();
            var request = new CommonTestCommand { Name = "", Value = "Valid", Count = 10 }; // Only Name is invalid

            foreach (var provider in providers)
            {
                var result = ValidateSync(provider, request, context);
                if (!result.IsInvalid)
                {
                    return false;
                }

                // All providers should include PropertyName for field-level errors
                if (!result.Errors.Any(e => e.PropertyName == "Name"))
                {
                    return false;
                }
            }

            return true;
        });
    }

    [Fact]
    public void AllProviders_MultipleFieldErrors_IncludeAllPropertyNames()
    {
        var providers = AllProviders().ToList();
        var context = CreateContext();
        var request = new CommonTestCommand { Name = "", Value = "", Count = 10 }; // Name and Value invalid

        foreach (var provider in providers)
        {
            var result = ValidateSync(provider, request, context);
            result.IsInvalid.ShouldBeTrue();

            var propertyNames = result.Errors.Select(e => e.PropertyName).ToHashSet();
            propertyNames.ShouldContain("Name");
            propertyNames.ShouldContain("Value");
        }
    }

    #endregion

    #region ValidationResult Consistency

    [EncinaProperty]
    public Property AllProviders_IsValidAndIsInvalid_MutuallyExclusive()
    {
        var providers = AllProviders().ToList();
        var nameGen = Arb.From(Gen.Elements("ValidName", ""));
        var valueGen = Arb.From(Gen.Elements("ValidValue", ""));
        var countGen = Arb.From(Gen.Choose(-10, 2000));

        return Prop.ForAll(nameGen, valueGen, countGen, (name, value, count) =>
        {
            var context = CreateContext();
            var request = new CommonTestCommand { Name = name, Value = value, Count = count };

            foreach (var provider in providers)
            {
                var result = ValidateSync(provider, request, context);
                if (result.IsValid == result.IsInvalid)
                {
                    return false;
                }
            }

            return true;
        });
    }

    [Fact]
    public void AllProviders_InvalidResult_HasNonEmptyErrorMessage()
    {
        var providers = AllProviders().ToList();
        var context = CreateContext();
        var request = new CommonTestCommand { Name = "", Value = "", Count = -5 };

        foreach (var provider in providers)
        {
            var result = ValidateSync(provider, request, context);
            result.Errors.ShouldAllBe(e => !string.IsNullOrEmpty(e.ErrorMessage));
        }
    }

    #endregion

    #region Pipeline Behavior Consistency

    [Fact]
    public void AllProviders_ValidationFailure_ConvertsToEitherLeft()
    {
        var providers = AllProviders().ToList();
        var context = CreateContext();
        var request = new CommonTestCommand { Name = "", Value = "", Count = -5 };

        foreach (var provider in providers)
        {
            var result = ValidateSync(provider, request, context);
            result.IsInvalid.ShouldBeTrue();

            // Verify the error message format
            var errorMessage = result.ToErrorMessage(nameof(CommonTestCommand));
            errorMessage.ShouldContain(ValidationResult.ValidationFailedPrefix);
            errorMessage.ShouldContain(nameof(CommonTestCommand));
        }
    }

    [EncinaProperty]
    public Property AllProviders_ValidationSuccess_ResultIsValidWithNoErrors()
    {
        var providers = AllProviders().ToList();
        var validNameGen = Arb.From(Gen.Elements("ValidName", "TestData", "ItemValue"));
        var validValueGen = Arb.From(Gen.Elements("Data1", "Data2", "Data3"));
        var validCountGen = Arb.From(Gen.Choose(1, 1000));

        return Prop.ForAll(validNameGen, validValueGen, validCountGen, (name, value, count) =>
        {
            var context = CreateContext();
            var request = new CommonTestCommand { Name = name, Value = value, Count = count };

            foreach (var provider in providers)
            {
                var result = ValidateSync(provider, request, context);

                if (!result.IsValid || !result.Errors.IsEmpty)
                {
                    return false;
                }
            }

            return true;
        });
    }

    #endregion

    #region Required Field Tests

    [Fact]
    public void AllProviders_EmptyRequiredField_ReturnsError()
    {
        var providers = AllProviders().ToList();
        var context = CreateContext();
        var request = new CommonTestCommand { Name = "Valid", Value = "", Count = 10 }; // Value is empty

        foreach (var provider in providers)
        {
            var result = ValidateSync(provider, request, context);
            result.IsInvalid.ShouldBeTrue();
        }
    }

    #endregion

    #region String Length Tests

    [Fact]
    public void AllProviders_StringTooShort_ReturnsError()
    {
        var providers = AllProviders().ToList();
        var context = CreateContext();
        var request = new CommonTestCommand { Name = "A", Value = "Valid", Count = 10 }; // Name too short

        foreach (var provider in providers)
        {
            var result = ValidateSync(provider, request, context);
            result.IsInvalid.ShouldBeTrue();
        }
    }

    #endregion

    #region Range Validation Tests

    [EncinaProperty]
    public Property AllProviders_ValueOutOfRange_ReturnsError()
    {
        var providers = AllProviders().ToList();
        var outOfRangeCountGen = Arb.From(Gen.OneOf(
            Gen.Choose(-100, 0),
            Gen.Choose(1001, 2000)
        ));

        return Prop.ForAll(outOfRangeCountGen, count =>
        {
            var context = CreateContext();
            var request = new CommonTestCommand { Name = "ValidName", Value = "ValidValue", Count = count };

            foreach (var provider in providers)
            {
                var result = ValidateSync(provider, request, context);
                if (!result.IsInvalid)
                {
                    return false;
                }
            }

            return true;
        });
    }

    #endregion
}
