using System.ComponentModel.DataAnnotations;
using Encina.DataAnnotations;
using Encina.Testing.FsCheck;
using Encina.Testing.Shouldly;
using Encina.Validation;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using ValidationResult = Encina.Validation.ValidationResult;

namespace Encina.DataAnnotations.PropertyTests;

/// <summary>
/// Property-based tests for DataAnnotations validation provider invariants.
/// Tests validation behavior using generated test data.
/// </summary>
public sealed class ValidationInvariantProperties : PropertyTestBase
{
    #region Test Request Types

    /// <summary>
    /// Test request with DataAnnotations validation attributes.
    /// </summary>
    public sealed record TestUserCommand : ICommand<string>
    {
        [Required(ErrorMessage = "Username is required.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters.")]
        public string Username { get; init; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Email must be a valid email address.")]
        public string Email { get; init; } = string.Empty;

        [Range(1, 150, ErrorMessage = "Age must be between 1 and 150.")]
        public int Age { get; init; }
    }

    /// <summary>
    /// Test request without validation attributes.
    /// </summary>
    public sealed record TestSimpleCommand(string Value) : ICommand<string>;

    /// <summary>
    /// Test request with empty collection validation.
    /// </summary>
    public sealed record TestCollectionCommand : ICommand<string>
    {
        [Required(ErrorMessage = "Items is required.")]
        [MinLength(1, ErrorMessage = "Items must have at least one element.")]
        public List<string>? Items { get; init; }
    }

    #endregion

    #region Setup

    private static IValidationProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddDataAnnotationsValidation();
        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IValidationProvider>();
    }

    private static IRequestContext CreateContext()
    {
        return RequestContext.Create(Guid.NewGuid().ToString());
    }

    /// <summary>
    /// Synchronous wrapper for ValidateAsync to enable FsCheck property testing.
    /// FsCheck 3.x Prop.ForAll does not support async lambdas, so synchronous blocking is required.
    /// Uses GetAwaiter().GetResult() to propagate the original exception without wrapping in AggregateException.
    /// </summary>
    private static ValidationResult ValidateSync<T>(IValidationProvider provider, T request, IRequestContext context)
        where T : class
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
        // Arrange
        var provider = CreateProvider();
        var context = CreateContext();

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await provider.ValidateAsync<TestUserCommand>(null!, context, CancellationToken.None));
    }

    [Fact]
    public async Task ValidateAsync_NullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var provider = CreateProvider();
        var request = new TestUserCommand { Username = "Valid", Email = "valid@test.com", Age = 25 };

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await provider.ValidateAsync(request, null!, CancellationToken.None));
    }

    #endregion

    #region Validation Idempotency

    [EncinaProperty]
    public Property ValidateAsync_SameRequestMultipleTimes_ReturnsSameResult()
    {
        var validUsernameGen = Arb.From(Gen.Elements("john", "jane", "alice", "bobsmith"));
        var validEmailGen = Arb.From(Gen.Elements("john@test.com", "jane@test.com", "alice@test.com"));
        var validAgeGen = Arb.From(Gen.Choose(1, 150));

        return Prop.ForAll(validUsernameGen, validEmailGen, validAgeGen, (username, email, age) =>
        {
            var provider = CreateProvider();
            var context = CreateContext();
            var request = new TestUserCommand { Username = username, Email = email, Age = age };

            var result1 = ValidateSync(provider, request, context);
            var result2 = ValidateSync(provider, request, context);
            var result3 = ValidateSync(provider, request, context);

            return result1.IsValid == result2.IsValid &&
                   result2.IsValid == result3.IsValid &&
                   result1.Errors.Length == result2.Errors.Length &&
                   result2.Errors.Length == result3.Errors.Length;
        });
    }

    [QuickProperty]
    public Property ValidateAsync_InvalidRequestMultipleTimes_ReturnsSameErrorCount()
    {
        return Prop.ForAll(Arb.From(Gen.Choose(1, 100)), _ =>
        {
            var provider = CreateProvider();
            var context = CreateContext();
            var request = new TestUserCommand { Username = "", Email = "invalid", Age = -5 };

            var result1 = ValidateSync(provider, request, context);
            var result2 = ValidateSync(provider, request, context);

            return result1.IsInvalid &&
                   result2.IsInvalid &&
                   result1.Errors.Length == result2.Errors.Length;
        });
    }

    #endregion

    #region Error Aggregation

    [EncinaProperty]
    public Property ValidateAsync_MultipleFailures_CapturesAllErrors()
    {
        var emptyStringGen = Arb.From(Gen.Constant(string.Empty));
        var invalidEmailGen = Arb.From(Gen.Elements("invalid", "notanemail", "noatsign", "two@@ats"));
        var invalidAgeGen = Arb.From(Gen.Choose(-100, 0));

        return Prop.ForAll(emptyStringGen, invalidEmailGen, invalidAgeGen, (username, email, age) =>
        {
            var provider = CreateProvider();
            var context = CreateContext();
            var request = new TestUserCommand { Username = username, Email = email, Age = age };

            var result = ValidateSync(provider, request, context);

            // Should have errors for multiple invalid fields
            return result.IsInvalid && result.Errors.Length >= 2;
        });
    }

    [EncinaProperty]
    public Property ValidateAsync_FieldLevelErrors_IncludePropertyName()
    {
        return Prop.ForAll(Arb.From(Gen.Choose(1, 100)), _ =>
        {
            var provider = CreateProvider();
            var context = CreateContext();
            var request = new TestUserCommand { Username = "", Email = "valid@test.com", Age = 25 };

            var result = ValidateSync(provider, request, context);

            return result.IsInvalid &&
                   result.Errors.Any(e => e.PropertyName == "Username");
        });
    }

    [EncinaProperty]
    public Property ValidateAsync_AllErrorsHaveMessages()
    {
        return Prop.ForAll(Arb.From(Gen.Choose(1, 100)), _ =>
        {
            var provider = CreateProvider();
            var context = CreateContext();
            var request = new TestUserCommand { Username = "", Email = "invalid", Age = -10 };

            var result = ValidateSync(provider, request, context);

            return result.IsInvalid &&
                   result.Errors.All(e => !string.IsNullOrEmpty(e.ErrorMessage));
        });
    }

    #endregion

    #region Boundary Conditions

    [QuickProperty]
    public Property ValidateAsync_EmptyCollection_ReturnsError()
    {
        return Prop.ForAll(Arb.From(Gen.Choose(1, 100)), _ =>
        {
            var provider = CreateProvider();
            var context = CreateContext();
            var request = new TestCollectionCommand { Items = new List<string>() };

            var result = ValidateSync(provider, request, context);

            return result.IsInvalid;
        });
    }

    [QuickProperty]
    public Property ValidateAsync_NullCollection_ReturnsError()
    {
        return Prop.ForAll(Arb.From(Gen.Choose(1, 100)), _ =>
        {
            var provider = CreateProvider();
            var context = CreateContext();
            var request = new TestCollectionCommand { Items = null };

            var result = ValidateSync(provider, request, context);

            return result.IsInvalid;
        });
    }

    [EncinaProperty]
    public Property ValidateAsync_BoundaryValues_ValidatedCorrectly()
    {
        var minAgeGen = Arb.From(Gen.Constant(1));
        var validUsernameGen = Arb.From(Gen.Elements("abc", "validusername"));
        var validEmailGen = Arb.From(Gen.Elements("a@b.co", "valid@test.com"));

        return Prop.ForAll(validUsernameGen, validEmailGen, minAgeGen, (username, email, age) =>
        {
            var provider = CreateProvider();
            var context = CreateContext();
            var request = new TestUserCommand { Username = username, Email = email, Age = age };

            var result = ValidateSync(provider, request, context);

            return result.IsValid;
        });
    }

    #endregion

    #region Valid Request Invariants

    [EncinaProperty]
    public Property ValidateAsync_ValidRequest_ReturnsSuccess()
    {
        var validUsernameGen = Arb.From(Gen.Elements("johndoe", "janesmith", "alice_bob"));
        var validEmailGen = Arb.From(Gen.Elements("john@test.com", "jane@example.com", "alice@domain.org"));
        var validAgeGen = Arb.From(Gen.Choose(1, 150));

        return Prop.ForAll(validUsernameGen, validEmailGen, validAgeGen, (username, email, age) =>
        {
            var provider = CreateProvider();
            var context = CreateContext();
            var request = new TestUserCommand { Username = username, Email = email, Age = age };

            var result = ValidateSync(provider, request, context);

            return result.IsValid && result.Errors.IsEmpty;
        });
    }

    [QuickProperty]
    public Property ValidateAsync_RequestWithoutAttributes_ReturnsSuccess()
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

    [EncinaProperty]
    public Property ValidationResult_IsValidAndIsInvalid_AreMutuallyExclusive()
    {
        return Prop.ForAll(Arb.From(Gen.Choose(1, 100)), _ =>
        {
            var provider = CreateProvider();
            var context = CreateContext();
            var request = new TestUserCommand { Username = "Valid", Email = "valid@test.com", Age = 25 };

            var result = ValidateSync(provider, request, context);

            return result.IsValid != result.IsInvalid;
        });
    }

    [EncinaProperty]
    public Property ValidationResult_InvalidResult_HasAtLeastOneError()
    {
        return Prop.ForAll(Arb.From(Gen.Choose(1, 100)), _ =>
        {
            var provider = CreateProvider();
            var context = CreateContext();
            var request = new TestUserCommand { Username = "", Email = "", Age = 0 };

            var result = ValidateSync(provider, request, context);

            return !result.IsValid || result.Errors.Length >= 1;
        });
    }

    #endregion

    #region ServiceCollection Extension Invariants

    [EncinaProperty]
    public Property AddDataAnnotationsValidation_NullServices_ThrowsArgumentNullException()
    {
        return Prop.ForAll(Arb.From(Gen.Choose(1, 100)), _ =>
        {
            IServiceCollection? services = null;

            var action = () => services!.AddDataAnnotationsValidation();

            action.ShouldThrow<ArgumentNullException>();
            return true;
        });
    }

    [EncinaProperty]
    public Property AddDataAnnotationsValidation_RegistersValidationProvider()
    {
        return Prop.ForAll(Arb.From(Gen.Choose(1, 100)), _ =>
        {
            var services = new ServiceCollection();
            services.AddDataAnnotationsValidation();
            var provider = services.BuildServiceProvider();

            var validationProvider = provider.GetService<IValidationProvider>();

            return validationProvider is DataAnnotationsValidationProvider;
        });
    }

    [EncinaProperty]
    public Property AddDataAnnotationsValidation_RegistersValidationOrchestrator()
    {
        return Prop.ForAll(Arb.From(Gen.Choose(1, 100)), _ =>
        {
            var services = new ServiceCollection();
            services.AddDataAnnotationsValidation();
            var provider = services.BuildServiceProvider();

            var orchestrator = provider.GetService<ValidationOrchestrator>();

            return orchestrator is not null;
        });
    }

    #endregion

    #region Context Metadata Propagation

    // NOTE: DataAnnotations validators do not have access to IRequestContext metadata (UserId, TenantId).
    // Unlike FluentValidation which can access RootContextData, DataAnnotations uses a simpler
    // ValidationContext that doesn't receive the Encina request context.
    // These tests verify that validation still succeeds when context metadata is provided,
    // but cannot verify actual propagation since DataAnnotations doesn't support it.
    // For metadata-aware validation, use FluentValidation instead.

    [EncinaProperty]
    public Property ValidateAsync_ContextWithUserId_ValidationStillSucceeds()
    {
        var userIdGen = Arb.From(Gen.Elements("user-1", "user-2", "admin-1"));

        return Prop.ForAll(userIdGen, userId =>
        {
            var provider = CreateProvider();
            var context = RequestContext.CreateForTest(userId: userId);
            var request = new TestUserCommand { Username = "valid", Email = "valid@test.com", Age = 25 };

            // DataAnnotations doesn't consume userId from context, but validation should still work
            var result = ValidateSync(provider, request, context);

            return result.IsValid;
        });
    }

    [EncinaProperty]
    public Property ValidateAsync_ContextWithTenantId_ValidationStillSucceeds()
    {
        var tenantIdGen = Arb.From(Gen.Elements("tenant-a", "tenant-b", "tenant-c"));

        return Prop.ForAll(tenantIdGen, tenantId =>
        {
            var provider = CreateProvider();
            var context = RequestContext.CreateForTest(tenantId: tenantId);
            var request = new TestUserCommand { Username = "valid", Email = "valid@test.com", Age = 25 };

            // DataAnnotations doesn't consume tenantId from context, but validation should still work
            var result = ValidateSync(provider, request, context);

            return result.IsValid;
        });
    }

    [EncinaProperty]
    public Property ValidateAsync_ContextWithUserIdAndTenantId_ValidationStillSucceeds()
    {
        var userIdGen = Arb.From(Gen.Elements("user-1", "user-2"));
        var tenantIdGen = Arb.From(Gen.Elements("tenant-a", "tenant-b"));

        return Prop.ForAll(userIdGen, tenantIdGen, (userId, tenantId) =>
        {
            var provider = CreateProvider();
            var context = RequestContext.CreateForTest(userId: userId, tenantId: tenantId);
            var request = new TestUserCommand { Username = "valid", Email = "valid@test.com", Age = 25 };

            // DataAnnotations doesn't consume userId/tenantId from context, but validation should still work
            var result = ValidateSync(provider, request, context);

            return result.IsValid;
        });
    }

    #endregion
}
