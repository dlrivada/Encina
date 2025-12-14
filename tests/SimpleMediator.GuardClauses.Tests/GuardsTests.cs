using LanguageExt;
using Shouldly;
using static LanguageExt.Prelude;

namespace SimpleMediator.GuardClauses.Tests;

public sealed class GuardsTests
{
    #region TryValidateNotNull Tests

    [Fact]
    public void TryValidateNotNull_WithNonNullValue_ShouldReturnTrue()
    {
        // Arrange
        var value = "test";

        // Act
        var result = Guards.TryValidateNotNull(value, nameof(value), out var error);

        // Assert
        result.ShouldBeTrue();
        error.ShouldBe(default(MediatorError));
    }

    [Fact]
    public void TryValidateNotNull_WithNullValue_ShouldReturnFalse()
    {
        // Arrange
        string? value = null;

        // Act
        var result = Guards.TryValidateNotNull(value, nameof(value), out var error);

        // Assert
        result.ShouldBeFalse();
        error.ShouldNotBe(default(MediatorError));
        error.Message.ShouldContain("value");
        error.Message.ShouldContain("cannot be null");
    }

    [Fact]
    public void TryValidateNotNull_WithCustomMessage_ShouldUseCustomMessage()
    {
        // Arrange
        string? value = null;
        var customMessage = "Custom error message";

        // Act
        var result = Guards.TryValidateNotNull(value, nameof(value), out var error, customMessage);

        // Assert
        result.ShouldBeFalse();
        error.Message.ShouldBe(customMessage);
    }

    [Fact]
    public void TryValidateNotNull_WithNullObject_ShouldReturnError()
    {
        // Arrange
        object? value = null;

        // Act
        var result = Guards.TryValidateNotNull(value, nameof(value), out var error);

        // Assert
        result.ShouldBeFalse();
        error.Message.ShouldContain("cannot be null");
    }

    #endregion

    #region TryValidateNotEmpty (String) Tests

    [Fact]
    public void TryValidateNotEmpty_String_WithNonEmptyString_ShouldReturnTrue()
    {
        // Arrange
        var value = "test";

        // Act
        var result = Guards.TryValidateNotEmpty(value, nameof(value), out var error);

        // Assert
        result.ShouldBeTrue();
        error.ShouldBe(default(MediatorError));
    }

    [Fact]
    public void TryValidateNotEmpty_String_WithNullString_ShouldReturnFalse()
    {
        // Arrange
        string? value = null;

        // Act
        var result = Guards.TryValidateNotEmpty(value, nameof(value), out var error);

        // Assert
        result.ShouldBeFalse();
        error.Message.ShouldContain("cannot be null or empty");
    }

    [Fact]
    public void TryValidateNotEmpty_String_WithEmptyString_ShouldReturnFalse()
    {
        // Arrange
        var value = "";

        // Act
        var result = Guards.TryValidateNotEmpty(value, nameof(value), out var error);

        // Assert
        result.ShouldBeFalse();
        error.Message.ShouldContain("cannot be null or empty");
    }

    #endregion

    #region TryValidateNotWhiteSpace Tests

    [Fact]
    public void TryValidateNotWhiteSpace_WithValidString_ShouldReturnTrue()
    {
        // Arrange
        var value = "test";

        // Act
        var result = Guards.TryValidateNotWhiteSpace(value, nameof(value), out var error);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void TryValidateNotWhiteSpace_WithWhiteSpaceString_ShouldReturnFalse()
    {
        // Arrange
        var value = "   ";

        // Act
        var result = Guards.TryValidateNotWhiteSpace(value, nameof(value), out var error);

        // Assert
        result.ShouldBeFalse();
        error.Message.ShouldContain("cannot be null, empty, or whitespace");
    }

    [Fact]
    public void TryValidateNotWhiteSpace_WithNullString_ShouldReturnFalse()
    {
        // Arrange
        string? value = null;

        // Act
        var result = Guards.TryValidateNotWhiteSpace(value, nameof(value), out var error);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region TryValidateNotEmpty (Collection) Tests

    [Fact]
    public void TryValidateNotEmpty_Collection_WithNonEmptyCollection_ShouldReturnTrue()
    {
        // Arrange
        var value = new[] { 1, 2, 3 };

        // Act
        var result = Guards.TryValidateNotEmpty(value, nameof(value), out var error);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void TryValidateNotEmpty_Collection_WithNullCollection_ShouldReturnFalse()
    {
        // Arrange
        int[]? value = null;

        // Act
        var result = Guards.TryValidateNotEmpty(value, nameof(value), out var error);

        // Assert
        result.ShouldBeFalse();
        error.Message.ShouldContain("cannot be null or empty");
    }

    [Fact]
    public void TryValidateNotEmpty_Collection_WithEmptyCollection_ShouldReturnFalse()
    {
        // Arrange
        var value = System.Array.Empty<int>();

        // Act
        var result = Guards.TryValidateNotEmpty(value, nameof(value), out var error);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void TryValidateNotEmpty_Collection_WithList_ShouldWork()
    {
        // Arrange
        var value = new List<string> { "item1", "item2" };

        // Act
        var result = Guards.TryValidateNotEmpty(value, nameof(value), out var error);

        // Assert
        result.ShouldBeTrue();
    }

    #endregion

    #region TryValidatePositive Tests

    [Fact]
    public void TryValidatePositive_WithPositiveInteger_ShouldReturnTrue()
    {
        // Arrange
        var value = 42;

        // Act
        var result = Guards.TryValidatePositive(value, nameof(value), out var error);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void TryValidatePositive_WithZero_ShouldReturnFalse()
    {
        // Arrange
        var value = 0;

        // Act
        var result = Guards.TryValidatePositive(value, nameof(value), out var error);

        // Assert
        result.ShouldBeFalse();
        error.Message.ShouldContain("must be positive");
    }

    [Fact]
    public void TryValidatePositive_WithNegativeInteger_ShouldReturnFalse()
    {
        // Arrange
        var value = -10;

        // Act
        var result = Guards.TryValidatePositive(value, nameof(value), out var error);

        // Assert
        result.ShouldBeFalse();
        error.Message.ShouldContain("must be positive");
    }

    [Fact]
    public void TryValidatePositive_WithPositiveDecimal_ShouldReturnTrue()
    {
        // Arrange
        var value = 3.14m;

        // Act
        var result = Guards.TryValidatePositive(value, nameof(value), out var error);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void TryValidatePositive_WithPositiveDouble_ShouldReturnTrue()
    {
        // Arrange
        var value = 2.71828;

        // Act
        var result = Guards.TryValidatePositive(value, nameof(value), out var error);

        // Assert
        result.ShouldBeTrue();
    }

    #endregion

    #region TryValidateNegative Tests

    [Fact]
    public void TryValidateNegative_WithNegativeInteger_ShouldReturnTrue()
    {
        // Arrange
        var value = -42;

        // Act
        var result = Guards.TryValidateNegative(value, nameof(value), out var error);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void TryValidateNegative_WithZero_ShouldReturnFalse()
    {
        // Arrange
        var value = 0;

        // Act
        var result = Guards.TryValidateNegative(value, nameof(value), out var error);

        // Assert
        result.ShouldBeFalse();
        error.Message.ShouldContain("must be negative");
    }

    [Fact]
    public void TryValidateNegative_WithPositiveInteger_ShouldReturnFalse()
    {
        // Arrange
        var value = 10;

        // Act
        var result = Guards.TryValidateNegative(value, nameof(value), out var error);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region TryValidateInRange Tests

    [Fact]
    public void TryValidateInRange_WithValueInRange_ShouldReturnTrue()
    {
        // Arrange
        var value = 50;

        // Act
        var result = Guards.TryValidateInRange(value, nameof(value), 1, 100, out var error);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void TryValidateInRange_WithValueAtMin_ShouldReturnTrue()
    {
        // Arrange
        var value = 1;

        // Act
        var result = Guards.TryValidateInRange(value, nameof(value), 1, 100, out var error);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void TryValidateInRange_WithValueAtMax_ShouldReturnTrue()
    {
        // Arrange
        var value = 100;

        // Act
        var result = Guards.TryValidateInRange(value, nameof(value), 1, 100, out var error);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void TryValidateInRange_WithValueBelowMin_ShouldReturnFalse()
    {
        // Arrange
        var value = 0;

        // Act
        var result = Guards.TryValidateInRange(value, nameof(value), 1, 100, out var error);

        // Assert
        result.ShouldBeFalse();
        error.Message.ShouldContain("must be between 1 and 100");
    }

    [Fact]
    public void TryValidateInRange_WithValueAboveMax_ShouldReturnFalse()
    {
        // Arrange
        var value = 101;

        // Act
        var result = Guards.TryValidateInRange(value, nameof(value), 1, 100, out var error);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void TryValidateInRange_WithDecimalRange_ShouldWork()
    {
        // Arrange
        var value = 5.5m;

        // Act
        var result = Guards.TryValidateInRange(value, nameof(value), 0.0m, 10.0m, out var error);

        // Assert
        result.ShouldBeTrue();
    }

    #endregion

    #region TryValidateEmail Tests

    [Fact]
    public void TryValidateEmail_WithValidEmail_ShouldReturnTrue()
    {
        // Arrange
        var value = "user@example.com";

        // Act
        var result = Guards.TryValidateEmail(value, nameof(value), out var error);

        // Assert
        result.ShouldBeTrue();
    }

    [Theory]
    [InlineData("test@test.com")]
    [InlineData("user.name@example.co.uk")]
    [InlineData("user+tag@example.com")]
    [InlineData("123@example.com")]
    public void TryValidateEmail_WithValidEmails_ShouldReturnTrue(string email)
    {
        // Act
        var result = Guards.TryValidateEmail(email, nameof(email), out var error);

        // Assert
        result.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("invalid")]
    [InlineData("@example.com")]
    [InlineData("user@")]
    [InlineData("user @example.com")]
    [InlineData("user@example")]
    public void TryValidateEmail_WithInvalidEmails_ShouldReturnFalse(string? email)
    {
        // Act
        var result = Guards.TryValidateEmail(email, nameof(email), out var error);

        // Assert
        result.ShouldBeFalse();
        error.Message.ShouldContain("must be a valid email address");
    }

    [Fact]
    public void TryValidateEmail_WithNull_ShouldReturnFalse()
    {
        // Arrange
        string? value = null;

        // Act
        var result = Guards.TryValidateEmail(value, nameof(value), out var error);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region TryValidateUrl Tests

    [Fact]
    public void TryValidateUrl_WithValidHttpUrl_ShouldReturnTrue()
    {
        // Arrange
        var value = "http://example.com";

        // Act
        var result = Guards.TryValidateUrl(value, nameof(value), out var error);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void TryValidateUrl_WithValidHttpsUrl_ShouldReturnTrue()
    {
        // Arrange
        var value = "https://example.com";

        // Act
        var result = Guards.TryValidateUrl(value, nameof(value), out var error);

        // Assert
        result.ShouldBeTrue();
    }

    [Theory]
    [InlineData("https://www.example.com")]
    [InlineData("https://example.com/path/to/resource")]
    [InlineData("https://example.com:8080/path?query=value")]
    [InlineData("http://subdomain.example.com")]
    public void TryValidateUrl_WithValidUrls_ShouldReturnTrue(string url)
    {
        // Act
        var result = Guards.TryValidateUrl(url, nameof(url), out var error);

        // Assert
        result.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-url")]
    [InlineData("ftp://example.com")]
    [InlineData("example.com")]
    [InlineData("//example.com")]
    public void TryValidateUrl_WithInvalidUrls_ShouldReturnFalse(string? url)
    {
        // Act
        var result = Guards.TryValidateUrl(url, nameof(url), out var error);

        // Assert
        result.ShouldBeFalse();
        error.Message.ShouldContain("must be a valid HTTP or HTTPS URL");
    }

    #endregion

    #region TryValidateNotEmpty (Guid) Tests

    [Fact]
    public void TryValidateNotEmpty_Guid_WithNonEmptyGuid_ShouldReturnTrue()
    {
        // Arrange
        var value = Guid.NewGuid();

        // Act
        var result = Guards.TryValidateNotEmpty(value, nameof(value), out var error);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void TryValidateNotEmpty_Guid_WithEmptyGuid_ShouldReturnFalse()
    {
        // Arrange
        var value = Guid.Empty;

        // Act
        var result = Guards.TryValidateNotEmpty(value, nameof(value), out var error);

        // Assert
        result.ShouldBeFalse();
        error.Message.ShouldContain("cannot be an empty GUID");
    }

    #endregion

    #region TryValidate (Custom Condition) Tests

    [Fact]
    public void TryValidate_WithTrueCondition_ShouldReturnTrue()
    {
        // Arrange
        var condition = true;

        // Act
        var result = Guards.TryValidate(condition, "customCondition", out var error);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void TryValidate_WithFalseCondition_ShouldReturnFalse()
    {
        // Arrange
        var condition = false;

        // Act
        var result = Guards.TryValidate(condition, "customCondition", out var error);

        // Assert
        result.ShouldBeFalse();
        error.Message.ShouldContain("Validation failed");
    }

    [Fact]
    public void TryValidate_WithCustomMessage_ShouldUseCustomMessage()
    {
        // Arrange
        var condition = false;
        var customMessage = "Order must be in Pending status";

        // Act
        var result = Guards.TryValidate(condition, "order.Status", out var error, customMessage);

        // Assert
        result.ShouldBeFalse();
        error.Message.ShouldBe(customMessage);
    }

    [Fact]
    public void TryValidate_WithComplexCondition_ShouldWork()
    {
        // Arrange
        var age = 25;
        var isAdult = age >= 18;

        // Act
        var result = Guards.TryValidate(isAdult, nameof(age), out var error, "User must be 18 or older");

        // Assert
        result.ShouldBeTrue();
    }

    #endregion

    #region TryValidatePattern Tests

    [Fact]
    public void TryValidatePattern_WithMatchingPattern_ShouldReturnTrue()
    {
        // Arrange
        var value = "555-123-4567";
        var pattern = @"^\d{3}-\d{3}-\d{4}$";

        // Act
        var result = Guards.TryValidatePattern(value, nameof(value), pattern, out var error);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void TryValidatePattern_WithNonMatchingPattern_ShouldReturnFalse()
    {
        // Arrange
        var value = "123-45-6789";
        var pattern = @"^\d{3}-\d{3}-\d{4}$";

        // Act
        var result = Guards.TryValidatePattern(value, nameof(value), pattern, out var error);

        // Assert
        result.ShouldBeFalse();
        error.Message.ShouldContain("does not match the required pattern");
    }

    [Fact]
    public void TryValidatePattern_WithNullValue_ShouldReturnFalse()
    {
        // Arrange
        string? value = null;
        var pattern = @"^\d{3}-\d{3}-\d{4}$";

        // Act
        var result = Guards.TryValidatePattern(value, nameof(value), pattern, out var error);

        // Assert
        result.ShouldBeFalse();
    }

    [Theory]
    [InlineData("ABC123")]
    [InlineData("XYZ789")]
    [InlineData("DEF456")]
    public void TryValidatePattern_WithAlphanumericPattern_ShouldWork(string value)
    {
        // Arrange
        var pattern = @"^[A-Z]{3}\d{3}$";

        // Act
        var result = Guards.TryValidatePattern(value, nameof(value), pattern, out var error);

        // Assert
        result.ShouldBeTrue();
    }

    #endregion

    #region Real-World Scenario Tests

    [Fact]
    public void Guards_InDomainModel_ShouldProtectInvariants()
    {
        // Arrange & Act & Assert
        Should.Throw<InvalidOperationException>(() => new User("", "password"))
            .Message.ShouldContain("cannot be null or empty");

        Should.Throw<InvalidOperationException>(() => new User("john@example.com", ""))
            .Message.ShouldContain("cannot be null or empty");

        Should.NotThrow(() => new User("john@example.com", "SecurePassword123"));
    }

    [Fact]
    public void Guards_InHandler_ShouldValidateState()
    {
        // Arrange
        Order? nullOrder = null;
        var validOrder = new Order { Id = Guid.NewGuid(), Status = OrderStatus.Pending };

        // Act - Null order
        var result1 = TestHandler.Handle(nullOrder);
        result1.IsLeft.ShouldBeTrue();

        // Act - Valid order
        var result2 = TestHandler.Handle(validOrder);
        result2.IsRight.ShouldBeTrue();
    }

    [Fact]
    public void Guards_MultipleValidations_ShouldShortCircuit()
    {
        // Arrange
        string? email = null;
        var age = 15;

        // Act - First guard fails, should not proceed
        if (!Guards.TryValidateNotNull(email, nameof(email), out var error1))
        {
            error1.Message.ShouldContain("cannot be null");
            return; // Short-circuit - don't validate age
        }

        // This should never execute
        if (!Guards.TryValidateInRange(age, nameof(age), 18, 120, out var error2))
        {
            throw new InvalidOperationException("Should not reach here");
        }
    }

    #endregion

    #region Helper Classes for Tests

    private sealed class User
    {
        public string Email { get; }
        public string Password { get; }

        public User(string email, string password)
        {
            if (!Guards.TryValidateNotEmpty(email, nameof(email), out var emailError))
                throw new InvalidOperationException(emailError.Message);

            if (!Guards.TryValidateNotEmpty(password, nameof(password), out var pwdError))
                throw new InvalidOperationException(pwdError.Message);

            Email = email;
            Password = password;
        }
    }

    private enum OrderStatus
    {
        Pending,
        Processing,
        Completed,
        Cancelled
    }

    private sealed class Order
    {
        public Guid Id { get; set; }
        public OrderStatus Status { get; set; }
    }

    private sealed class TestHandler
    {
        public static Either<MediatorError, Guid> Handle(Order? order)
        {
            if (!Guards.TryValidateNotNull(order, nameof(order), out var error))
                return Left<MediatorError, Guid>(error);

            return Right<MediatorError, Guid>(order!.Id);
        }
    }

    #endregion
}
