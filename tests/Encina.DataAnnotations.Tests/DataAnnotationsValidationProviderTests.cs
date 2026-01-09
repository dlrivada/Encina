using System.ComponentModel.DataAnnotations;
using Encina.TestInfrastructure.PropertyTests;
using Encina.Validation;
using Shouldly;

namespace Encina.DataAnnotations.Tests;

/// <summary>
/// Tests for <see cref="DataAnnotationsValidationProvider"/> implementation.
/// </summary>
public sealed class DataAnnotationsValidationProviderTests
{
    #region Test Request Types

    /// <summary>
    /// Request with [Required] attribute.
    /// </summary>
    private sealed record RequiredFieldRequest : ICommand<string>
    {
        [Required(ErrorMessage = "Name is required.")]
        public string Name { get; init; } = string.Empty;
    }

    /// <summary>
    /// Request with [StringLength] attribute.
    /// </summary>
    private sealed record StringLengthRequest : ICommand<string>
    {
        [StringLength(20, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 20 characters.")]
        public string Username { get; init; } = string.Empty;
    }

    /// <summary>
    /// Request with [Range] attribute.
    /// </summary>
    private sealed record RangeRequest : ICommand<string>
    {
        [Range(1, 150, ErrorMessage = "Age must be between 1 and 150.")]
        public int Age { get; init; }
    }

    /// <summary>
    /// Request with [EmailAddress] attribute.
    /// </summary>
    private sealed record EmailRequest : ICommand<string>
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Email must be a valid email address.")]
        public string Email { get; init; } = string.Empty;
    }

    /// <summary>
    /// Request with multiple validation attributes.
    /// </summary>
    private sealed record MultipleAttributesRequest : ICommand<string>
    {
        [Required(ErrorMessage = "Name is required.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 50 characters.")]
        public string Name { get; init; } = string.Empty;

        [Range(1, 150, ErrorMessage = "Age must be between 1 and 150.")]
        public int Age { get; init; }

        [EmailAddress(ErrorMessage = "Email must be valid.")]
        public string? Email { get; init; }
    }

    /// <summary>
    /// Request with nested object validation.
    /// </summary>
    private sealed record NestedObjectRequest : ICommand<string>
    {
        [Required(ErrorMessage = "Address is required.")]
        public AddressDto? Address { get; init; }
    }

    private sealed record AddressDto
    {
        [Required(ErrorMessage = "Street is required.")]
        public string Street { get; init; } = string.Empty;

        [Required(ErrorMessage = "City is required.")]
        public string City { get; init; } = string.Empty;
    }

    /// <summary>
    /// Request with no validation attributes.
    /// </summary>
    private sealed record NoAttributesRequest : ICommand<string>
    {
        public string Value { get; init; } = string.Empty;
    }

    /// <summary>
    /// Request with [RegularExpression] attribute.
    /// </summary>
    private sealed record RegexRequest : ICommand<string>
    {
        [RegularExpression(@"^\d{5}$", ErrorMessage = "Zip code must be exactly 5 digits.")]
        public string ZipCode { get; init; } = string.Empty;
    }

    /// <summary>
    /// Request with [Compare] attribute.
    /// </summary>
    private sealed record CompareRequest : ICommand<string>
    {
        [Required(ErrorMessage = "Password is required.")]
        public string Password { get; init; } = string.Empty;

        [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; init; } = string.Empty;
    }

    #endregion

    #region Successful Validation Tests

    [Fact]
    public async Task ValidateAsync_WithValidRequiredField_ShouldReturnSuccess()
    {
        // Arrange
        var provider = new DataAnnotationsValidationProvider();
        var request = new RequiredFieldRequest { Name = "John" };
        var context = new TestRequestContext();

        // Act
        var result = await provider.ValidateAsync(request, context, CancellationToken.None);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public async Task ValidateAsync_WithValidStringLength_ShouldReturnSuccess()
    {
        // Arrange
        var provider = new DataAnnotationsValidationProvider();
        var request = new StringLengthRequest { Username = "validuser" };
        var context = new TestRequestContext();

        // Act
        var result = await provider.ValidateAsync(request, context, CancellationToken.None);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public async Task ValidateAsync_WithValidRange_ShouldReturnSuccess()
    {
        // Arrange
        var provider = new DataAnnotationsValidationProvider();
        var request = new RangeRequest { Age = 25 };
        var context = new TestRequestContext();

        // Act
        var result = await provider.ValidateAsync(request, context, CancellationToken.None);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public async Task ValidateAsync_WithValidEmail_ShouldReturnSuccess()
    {
        // Arrange
        var provider = new DataAnnotationsValidationProvider();
        var request = new EmailRequest { Email = "user@example.com" };
        var context = new TestRequestContext();

        // Act
        var result = await provider.ValidateAsync(request, context, CancellationToken.None);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public async Task ValidateAsync_WithAllValidFields_ShouldReturnSuccess()
    {
        // Arrange
        var provider = new DataAnnotationsValidationProvider();
        var request = new MultipleAttributesRequest
        {
            Name = "John Doe",
            Age = 30,
            Email = "john@example.com"
        };
        var context = new TestRequestContext();

        // Act
        var result = await provider.ValidateAsync(request, context, CancellationToken.None);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public async Task ValidateAsync_WithValidRegex_ShouldReturnSuccess()
    {
        // Arrange
        var provider = new DataAnnotationsValidationProvider();
        var request = new RegexRequest { ZipCode = "12345" };
        var context = new TestRequestContext();

        // Act
        var result = await provider.ValidateAsync(request, context, CancellationToken.None);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public async Task ValidateAsync_WithMatchingPasswords_ShouldReturnSuccess()
    {
        // Arrange
        var provider = new DataAnnotationsValidationProvider();
        var request = new CompareRequest
        {
            Password = "secret123",
            ConfirmPassword = "secret123"
        };
        var context = new TestRequestContext();

        // Act
        var result = await provider.ValidateAsync(request, context, CancellationToken.None);

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
        var provider = new DataAnnotationsValidationProvider();
        var request = new RequiredFieldRequest { Name = "" };
        var context = new TestRequestContext();

        // Act
        var result = await provider.ValidateAsync(request, context, CancellationToken.None);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.Length.ShouldBe(1);
        result.Errors[0].PropertyName.ShouldBe("Name");
        result.Errors[0].ErrorMessage.ShouldBe("Name is required.");
    }

    [Fact]
    public async Task ValidateAsync_WithTooShortStringLength_ShouldReturnFailure()
    {
        // Arrange
        var provider = new DataAnnotationsValidationProvider();
        var request = new StringLengthRequest { Username = "ab" };
        var context = new TestRequestContext();

        // Act
        var result = await provider.ValidateAsync(request, context, CancellationToken.None);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.Length.ShouldBe(1);
        result.Errors[0].PropertyName.ShouldBe("Username");
        result.Errors[0].ErrorMessage.ShouldBe("Username must be between 3 and 20 characters.");
    }

    [Fact]
    public async Task ValidateAsync_WithTooLongStringLength_ShouldReturnFailure()
    {
        // Arrange
        var provider = new DataAnnotationsValidationProvider();
        var request = new StringLengthRequest { Username = "thisusernameiswaytoolong" };
        var context = new TestRequestContext();

        // Act
        var result = await provider.ValidateAsync(request, context, CancellationToken.None);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.Length.ShouldBe(1);
        result.Errors[0].PropertyName.ShouldBe("Username");
        result.Errors[0].ErrorMessage.ShouldBe("Username must be between 3 and 20 characters.");
    }

    [Fact]
    public async Task ValidateAsync_WithOutOfRangeValue_ShouldReturnFailure()
    {
        // Arrange
        var provider = new DataAnnotationsValidationProvider();
        var request = new RangeRequest { Age = 0 };
        var context = new TestRequestContext();

        // Act
        var result = await provider.ValidateAsync(request, context, CancellationToken.None);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.Length.ShouldBe(1);
        result.Errors[0].PropertyName.ShouldBe("Age");
        result.Errors[0].ErrorMessage.ShouldBe("Age must be between 1 and 150.");
    }

    [Fact]
    public async Task ValidateAsync_WithInvalidEmail_ShouldReturnFailure()
    {
        // Arrange
        var provider = new DataAnnotationsValidationProvider();
        var request = new EmailRequest { Email = "not-an-email" };
        var context = new TestRequestContext();

        // Act
        var result = await provider.ValidateAsync(request, context, CancellationToken.None);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage == "Email must be a valid email address.");
    }

    [Fact]
    public async Task ValidateAsync_WithMultipleInvalidFields_ShouldReturnFirstErrorPerField()
    {
        // Arrange
        var provider = new DataAnnotationsValidationProvider();
        var request = new MultipleAttributesRequest
        {
            Name = "",
            Age = 0,
            Email = "invalid"
        };
        var context = new TestRequestContext();

        // Act
        var result = await provider.ValidateAsync(request, context, CancellationToken.None);

        // Assert
        // DataAnnotations short-circuits validation within each field: once a validator fails,
        // subsequent validators for that field don't run. Expected errors:
        // - Name: Required fails, StringLength skipped
        // - Age: Range fails
        // - Email: EmailAddress fails
        result.IsValid.ShouldBeFalse();
        result.Errors.Length.ShouldBe(3);
        result.Errors.ShouldContain(e => e.ErrorMessage == "Name is required.");
        result.Errors.ShouldContain(e => e.ErrorMessage == "Age must be between 1 and 150.");
        result.Errors.ShouldContain(e => e.ErrorMessage == "Email must be valid.");
    }

    [Fact]
    public async Task ValidateAsync_WithInvalidRegex_ShouldReturnFailure()
    {
        // Arrange
        var provider = new DataAnnotationsValidationProvider();
        var request = new RegexRequest { ZipCode = "1234" };
        var context = new TestRequestContext();

        // Act
        var result = await provider.ValidateAsync(request, context, CancellationToken.None);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.Length.ShouldBe(1);
        result.Errors[0].PropertyName.ShouldBe("ZipCode");
        result.Errors[0].ErrorMessage.ShouldBe("Zip code must be exactly 5 digits.");
    }

    [Fact]
    public async Task ValidateAsync_WithMismatchedPasswords_ShouldReturnFailure()
    {
        // Arrange
        var provider = new DataAnnotationsValidationProvider();
        var request = new CompareRequest
        {
            Password = "secret123",
            ConfirmPassword = "different456"
        };
        var context = new TestRequestContext();

        // Act
        var result = await provider.ValidateAsync(request, context, CancellationToken.None);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage == "Passwords do not match.");
    }

    #endregion

    #region Nested Object Validation Tests

    [Fact]
    public async Task ValidateAsync_WithNullNestedObject_ShouldReturnFailure()
    {
        // Arrange
        var provider = new DataAnnotationsValidationProvider();
        var request = new NestedObjectRequest { Address = null };
        var context = new TestRequestContext();

        // Act
        var result = await provider.ValidateAsync(request, context, CancellationToken.None);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage == "Address is required.");
    }

    #endregion

    #region No Attributes Tests

    [Fact]
    public async Task ValidateAsync_WithNoAttributes_ShouldReturnSuccess()
    {
        // Arrange
        var provider = new DataAnnotationsValidationProvider();
        var request = new NoAttributesRequest { Value = "" };
        var context = new TestRequestContext();

        // Act
        var result = await provider.ValidateAsync(request, context, CancellationToken.None);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    #endregion

    #region Guard Clause Tests

    [Fact]
    public async Task ValidateAsync_WithNullRequest_ShouldThrowArgumentNullException()
    {
        // Arrange
        var provider = new DataAnnotationsValidationProvider();
        var context = new TestRequestContext();

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await provider.ValidateAsync<RequiredFieldRequest>(null!, context, CancellationToken.None));
    }

    [Fact]
    public async Task ValidateAsync_WithNullContext_ShouldThrowArgumentNullException()
    {
        // Arrange
        var provider = new DataAnnotationsValidationProvider();
        var request = new RequiredFieldRequest { Name = "John" };

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await provider.ValidateAsync(request, null!, CancellationToken.None));
    }

    #endregion
}
