using Encina.MiniValidator;
using System.ComponentModel.DataAnnotations;
using Encina.TestInfrastructure.PropertyTests;
using Encina.Validation;
using Shouldly;

namespace Encina.UnitTests.MiniValidator;

/// <summary>
/// Tests for <see cref="MiniValidationProvider"/> implementation.
/// </summary>
public sealed class MiniValidationProviderTests
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
    /// Request with multiple validation attributes on the same property.
    /// </summary>
    private sealed record MultipleAttributesSamePropertyRequest : ICommand<string>
    {
        [Required(ErrorMessage = "Password is required.")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
        [RegularExpression(@".*\d.*", ErrorMessage = "Password must contain at least one digit.")]
        public string Password { get; init; } = string.Empty;
    }

    /// <summary>
    /// Request with multiple fields having validation.
    /// </summary>
    private sealed record MultipleFieldsRequest : ICommand<string>
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
    /// Request with no validation attributes.
    /// </summary>
    private sealed record NoAttributesRequest : ICommand<string>
    {
        public string Value { get; init; } = string.Empty;
    }

    /// <summary>
    /// Request with nested object validation.
    /// </summary>
    private sealed record NestedObjectRequest : ICommand<string>
    {
        [Required(ErrorMessage = "Contact is required.")]
        public ContactInfo? Contact { get; init; }
    }

    private sealed record ContactInfo
    {
        [Required(ErrorMessage = "Phone is required.")]
        public string Phone { get; init; } = string.Empty;

        [EmailAddress(ErrorMessage = "Email must be valid.")]
        public string? Email { get; init; }
    }

    /// <summary>
    /// Request with collection validation.
    /// </summary>
    private sealed record CollectionRequest : ICommand<string>
    {
        [Required(ErrorMessage = "Items are required.")]
        [MinLength(1, ErrorMessage = "At least one item is required.")]
        public List<string>? Items { get; init; }
    }

    /// <summary>
    /// Request with [RegularExpression] attribute.
    /// </summary>
    private sealed record RegexRequest : ICommand<string>
    {
        [RegularExpression(@"^\d{5}(-\d{4})?$", ErrorMessage = "Zip code must be in format 12345 or 12345-6789.")]
        public string ZipCode { get; init; } = string.Empty;
    }

    #endregion

    #region Successful Validation Tests

    [Fact]
    public async Task ValidateAsync_WithValidRequiredField_ShouldReturnSuccess()
    {
        // Arrange
        var provider = new MiniValidationProvider();
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
        var provider = new MiniValidationProvider();
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
        var provider = new MiniValidationProvider();
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
        var provider = new MiniValidationProvider();
        var request = new EmailRequest { Email = "user@example.com" };
        var context = new TestRequestContext();

        // Act
        var result = await provider.ValidateAsync(request, context, CancellationToken.None);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public async Task ValidateAsync_WithValidPassword_ShouldReturnSuccess()
    {
        // Arrange
        var provider = new MiniValidationProvider();
        var request = new MultipleAttributesSamePropertyRequest { Password = "password123" };
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
        var provider = new MiniValidationProvider();
        var request = new MultipleFieldsRequest
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
        var provider = new MiniValidationProvider();
        var request = new RegexRequest { ZipCode = "12345" };
        var context = new TestRequestContext();

        // Act
        var result = await provider.ValidateAsync(request, context, CancellationToken.None);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public async Task ValidateAsync_WithValidRegexExtended_ShouldReturnSuccess()
    {
        // Arrange
        var provider = new MiniValidationProvider();
        var request = new RegexRequest { ZipCode = "12345-6789" };
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
        var provider = new MiniValidationProvider();
        var request = new RequiredFieldRequest { Name = "" };
        var context = new TestRequestContext();

        // Act
        var result = await provider.ValidateAsync(request, context, CancellationToken.None);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Name" && e.ErrorMessage == "Name is required.");
    }

    [Fact]
    public async Task ValidateAsync_WithTooShortStringLength_ShouldReturnFailure()
    {
        // Arrange
        var provider = new MiniValidationProvider();
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
        var provider = new MiniValidationProvider();
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
        var provider = new MiniValidationProvider();
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
        var provider = new MiniValidationProvider();
        var request = new EmailRequest { Email = "not-an-email" };
        var context = new TestRequestContext();

        // Act
        var result = await provider.ValidateAsync(request, context, CancellationToken.None);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage == "Email must be a valid email address.");
    }

    [Fact]
    public async Task ValidateAsync_WithInvalidRegex_ShouldReturnFailure()
    {
        // Arrange
        var provider = new MiniValidationProvider();
        var request = new RegexRequest { ZipCode = "1234" };
        var context = new TestRequestContext();

        // Act
        var result = await provider.ValidateAsync(request, context, CancellationToken.None);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.Length.ShouldBe(1);
        result.Errors[0].PropertyName.ShouldBe("ZipCode");
        result.Errors[0].ErrorMessage.ShouldBe("Zip code must be in format 12345 or 12345-6789.");
    }

    #endregion

    #region Multiple Errors Same Property Tests

    [Fact]
    public async Task ValidateAsync_WithEmptyPassword_ShouldReturnMultipleErrors()
    {
        // Arrange
        var provider = new MiniValidationProvider();
        var request = new MultipleAttributesSamePropertyRequest { Password = "" };
        var context = new TestRequestContext();

        // Act
        var result = await provider.ValidateAsync(request, context, CancellationToken.None);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Password");
    }

    [Fact]
    public async Task ValidateAsync_WithShortPasswordNoDigit_ShouldReturnMultipleErrors()
    {
        // Arrange
        var provider = new MiniValidationProvider();
        var request = new MultipleAttributesSamePropertyRequest { Password = "short" };
        var context = new TestRequestContext();

        // Act
        var result = await provider.ValidateAsync(request, context, CancellationToken.None);

        // Assert
        result.IsValid.ShouldBeFalse();

        // MiniValidator should return multiple errors for the same property
        var passwordErrors = result.Errors
            .Where(e => e.PropertyName == "Password" && e.ErrorMessage is not null)
            .ToList();

        passwordErrors.Count.ShouldBe(2);
        passwordErrors.ShouldContain(e => e.ErrorMessage!.Contains("8 characters"));
        passwordErrors.ShouldContain(e => e.ErrorMessage!.Contains("digit"));
    }

    #endregion

    #region Multiple Fields Errors Tests

    [Fact]
    public async Task ValidateAsync_WithMultipleInvalidFields_ShouldReturnAllErrors()
    {
        // Arrange
        var provider = new MiniValidationProvider();
        var request = new MultipleFieldsRequest
        {
            Name = "",
            Age = 0,
            Email = "invalid"
        };
        var context = new TestRequestContext();

        // Act
        var result = await provider.ValidateAsync(request, context, CancellationToken.None);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.Length.ShouldBe(3);
        result.Errors.ShouldContain(e => e.PropertyName == "Name");
        result.Errors.ShouldContain(e => e.PropertyName == "Age");
        result.Errors.ShouldContain(e => e.PropertyName == "Email");
    }

    #endregion

    #region Nested Object Validation Tests

    [Fact]
    public async Task ValidateAsync_WithNullNestedObject_ShouldReturnFailure()
    {
        // Arrange
        var provider = new MiniValidationProvider();
        var request = new NestedObjectRequest { Contact = null };
        var context = new TestRequestContext();

        // Act
        var result = await provider.ValidateAsync(request, context, CancellationToken.None);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage == "Contact is required.");
    }

    [Fact]
    public async Task ValidateAsync_WithValidNestedObject_ShouldReturnSuccess()
    {
        // Arrange
        var provider = new MiniValidationProvider();
        var request = new NestedObjectRequest
        {
            Contact = new ContactInfo
            {
                Phone = "555-1234",
                Email = "contact@example.com"
            }
        };
        var context = new TestRequestContext();

        // Act
        var result = await provider.ValidateAsync(request, context, CancellationToken.None);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    #endregion

    #region Collection Validation Tests

    [Fact]
    public async Task ValidateAsync_WithNullCollection_ShouldReturnFailure()
    {
        // Arrange
        var provider = new MiniValidationProvider();
        var request = new CollectionRequest { Items = null };
        var context = new TestRequestContext();

        // Act
        var result = await provider.ValidateAsync(request, context, CancellationToken.None);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage == "Items are required.");
    }

    [Fact]
    public async Task ValidateAsync_WithEmptyCollection_ShouldReturnFailure()
    {
        // Arrange
        var provider = new MiniValidationProvider();
        var request = new CollectionRequest { Items = [] };
        var context = new TestRequestContext();

        // Act
        var result = await provider.ValidateAsync(request, context, CancellationToken.None);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage == "At least one item is required.");
    }

    [Fact]
    public async Task ValidateAsync_WithValidCollection_ShouldReturnSuccess()
    {
        // Arrange
        var provider = new MiniValidationProvider();
        var request = new CollectionRequest { Items = ["item1", "item2"] };
        var context = new TestRequestContext();

        // Act
        var result = await provider.ValidateAsync(request, context, CancellationToken.None);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    #endregion

    #region No Attributes Tests

    [Fact]
    public async Task ValidateAsync_WithNoAttributes_ShouldReturnSuccess()
    {
        // Arrange
        var provider = new MiniValidationProvider();
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
        var provider = new MiniValidationProvider();
        var context = new TestRequestContext();

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await provider.ValidateAsync<RequiredFieldRequest>(null!, context, CancellationToken.None));
    }

    [Fact]
    public async Task ValidateAsync_WithNullContext_ShouldThrowArgumentNullException()
    {
        // Arrange
        var provider = new MiniValidationProvider();
        var request = new RequiredFieldRequest { Name = "John" };

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await provider.ValidateAsync(request, null!, CancellationToken.None));
    }

    #endregion
}
