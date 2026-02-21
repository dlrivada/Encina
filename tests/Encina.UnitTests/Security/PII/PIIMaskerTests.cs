using Encina.Security.PII;
using Encina.Security.PII.Abstractions;
using Encina.Security.PII.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Encina.UnitTests.Security.PII;

public sealed class PIIMaskerTests
{
    #region Test DTOs

    private sealed class TestDto
    {
        [PII(PIIType.Email)]
        public string Email { get; set; } = "john@example.com";

        [PII(PIIType.Phone)]
        public string Phone { get; set; } = "555-123-4567";

        [PII(PIIType.Name)]
        public string Name { get; set; } = "John Doe";

        public string NonPii { get; set; } = "not-sensitive";
    }

    private sealed class NoPiiDto
    {
        public string Title { get; set; } = "Hello";
        public int Count { get; set; } = 42;
    }

    private sealed class SensitiveFieldDto
    {
        public string Password { get; set; } = "supersecret";
        public string ApiKey { get; set; } = "sk-1234567890";
        public string NormalField { get; set; } = "visible";
    }

    private readonly record struct ValueTypeRequest(string Name, int Id);

    #endregion

    #region Helpers

    private static PIIMasker CreateSut(PIIOptions? options = null)
    {
        var opts = options ?? new PIIOptions();
        var services = new ServiceCollection();
        services.Configure<PIIOptions>(_ => { });
        var sp = services.BuildServiceProvider();
        return new PIIMasker(
            Options.Create(opts),
            Substitute.For<ILogger<PIIMasker>>(),
            sp);
    }

    #endregion

    #region Mask(string, PIIType)

    [Fact]
    public void Mask_Email_ReturnsMaskedEmail()
    {
        var sut = CreateSut();

        var result = sut.Mask("john@example.com", PIIType.Email);

        result.ShouldNotBe("john@example.com");
        result.ShouldContain("@example.com");
    }

    [Fact]
    public void Mask_Phone_ReturnsMaskedPhone()
    {
        var sut = CreateSut();

        var result = sut.Mask("555-123-4567", PIIType.Phone);

        result.ShouldNotBe("555-123-4567");
        // Phone masking reveals last 4 digits
        result.ShouldEndWith("4567");
    }

    [Fact]
    public void Mask_Name_ReturnsMaskedName()
    {
        var sut = CreateSut();

        var result = sut.Mask("John Doe", PIIType.Name);

        result.ShouldNotBe("John Doe");
        // Name masking preserves first character
        result.ShouldStartWith("J");
    }

    [Fact]
    public void Mask_EmptyString_ReturnsEmpty()
    {
        var sut = CreateSut();

        var result = sut.Mask(string.Empty, PIIType.Email);

        result.ShouldBe(string.Empty);
    }

    [Fact]
    public void Mask_NullValue_ReturnsNull()
    {
        var sut = CreateSut();

        var result = sut.Mask(null!, PIIType.Email);

        result.ShouldBeNull();
    }

    [Fact]
    public void Mask_CreditCard_ReturnsMaskedCreditCard()
    {
        var sut = CreateSut();

        var result = sut.Mask("4111-1111-1111-1111", PIIType.CreditCard);

        result.ShouldNotBe("4111-1111-1111-1111");
        result.ShouldEndWith("1111");
    }

    [Fact]
    public void Mask_SSN_ReturnsMaskedSSN()
    {
        var sut = CreateSut();

        var result = sut.Mask("123-45-6789", PIIType.SSN);

        result.ShouldNotBe("123-45-6789");
        result.ShouldEndWith("6789");
    }

    [Theory]
    [InlineData(PIIType.Address)]
    [InlineData(PIIType.DateOfBirth)]
    [InlineData(PIIType.IPAddress)]
    public void Mask_OtherPIITypes_ReturnsDifferentValue(PIIType type)
    {
        var sut = CreateSut();
        var original = "some-test-value-1234";

        var result = sut.Mask(original, type);

        result.ShouldNotBe(original);
    }

    #endregion

    #region Mask(string, string pattern)

    [Fact]
    public void Mask_WithPattern_ReturnsPatternMasked()
    {
        var sut = CreateSut();

        var result = sut.Mask("License: ABC-12345", @"\d+");

        result.ShouldBe("License: ABC-*****");
    }

    [Fact]
    public void Mask_WithPattern_EmptyString_ReturnsEmpty()
    {
        var sut = CreateSut();

        var result = sut.Mask(string.Empty, @"\d+");

        result.ShouldBe(string.Empty);
    }

    [Fact]
    public void Mask_InvalidRegex_ReturnsOriginal()
    {
        var sut = CreateSut();

        var result = sut.Mask("test-value", "[invalid(regex");

        result.ShouldBe("test-value");
    }

    [Fact]
    public void Mask_WithPattern_NullPattern_ThrowsArgumentNull()
    {
        var sut = CreateSut();

        Should.Throw<ArgumentNullException>(() => sut.Mask("value", (string)null!));
    }

    [Fact]
    public void Mask_WithPattern_NoMatch_ReturnsOriginal()
    {
        var sut = CreateSut();

        var result = sut.Mask("no-digits-here", @"\d+");

        result.ShouldBe("no-digits-here");
    }

    #endregion

    #region MaskObject<T>

    [Fact]
    public void MaskObject_WithPIIAttributes_MasksDecoratedProperties()
    {
        var sut = CreateSut();
        var dto = new TestDto();

        var result = sut.MaskObject(dto);

        result.Email.ShouldNotBe("john@example.com");
        result.Phone.ShouldNotBe("555-123-4567");
        result.Name.ShouldNotBe("John Doe");
    }

    [Fact]
    public void MaskObject_PreservesNonPIIProperties()
    {
        var sut = CreateSut();
        var dto = new TestDto();

        var result = sut.MaskObject(dto);

        result.NonPii.ShouldBe("not-sensitive");
    }

    [Fact]
    public void MaskObject_NullObject_ThrowsArgumentNull()
    {
        var sut = CreateSut();

        Should.Throw<ArgumentNullException>(() => sut.MaskObject<TestDto>(null!));
    }

    [Fact]
    public void MaskObject_NoAttributes_ReturnsUnchanged()
    {
        var sut = CreateSut();
        var dto = new NoPiiDto { Title = "Hello", Count = 42 };

        var result = sut.MaskObject(dto);

        result.Title.ShouldBe("Hello");
        result.Count.ShouldBe(42);
    }

    [Fact]
    public void MaskObject_DoesNotMutateOriginalObject()
    {
        var sut = CreateSut();
        var dto = new TestDto();
        var originalEmail = dto.Email;

        _ = sut.MaskObject(dto);

        dto.Email.ShouldBe(originalEmail);
    }

    [Fact]
    public void MaskObject_SensitiveFieldPatterns_RedactsMatchingFields()
    {
        var options = new PIIOptions();
        // "password" and "apikey" are default sensitive patterns
        var sut = CreateSut(options);
        var dto = new SensitiveFieldDto();

        var result = sut.MaskObject(dto);

        result.Password.ShouldBe("[REDACTED]");
        result.ApiKey.ShouldBe("[REDACTED]");
        result.NormalField.ShouldBe("visible");
    }

    #endregion

    #region MaskForAudit<T> (generic)

    [Fact]
    public void MaskForAudit_WhenEnabled_MasksProperties()
    {
        var options = new PIIOptions { MaskInAuditTrails = true };
        var sut = CreateSut(options);
        var dto = new TestDto();

        var result = sut.MaskForAudit(dto);

        result.Email.ShouldNotBe("john@example.com");
        result.Phone.ShouldNotBe("555-123-4567");
        result.Name.ShouldNotBe("John Doe");
    }

    [Fact]
    public void MaskForAudit_WhenDisabled_ReturnsOriginal()
    {
        var options = new PIIOptions { MaskInAuditTrails = false };
        var sut = CreateSut(options);
        var dto = new TestDto();

        var result = sut.MaskForAudit(dto);

        result.ShouldBeSameAs(dto);
    }

    [Fact]
    public void MaskForAudit_NullRequest_ThrowsArgumentNull()
    {
        var sut = CreateSut();

        Should.Throw<ArgumentNullException>(() => sut.MaskForAudit<TestDto>(null!));
    }

    [Fact]
    public void MaskForAudit_ValueType_ReturnsAsIs()
    {
        var options = new PIIOptions { MaskInAuditTrails = true };
        var sut = CreateSut(options);
        var request = new ValueTypeRequest("Test", 1);

        var result = sut.MaskForAudit(request);

        result.Name.ShouldBe("Test");
        result.Id.ShouldBe(1);
    }

    #endregion

    #region MaskForAudit(object) (non-generic)

    [Fact]
    public void MaskForAudit_NonGeneric_MasksProperties()
    {
        var options = new PIIOptions { MaskInAuditTrails = true };
        var sut = CreateSut(options);
        object dto = new TestDto();

        var result = sut.MaskForAudit(dto);

        var typed = result.ShouldBeOfType<TestDto>();
        typed.Email.ShouldNotBe("john@example.com");
        typed.Phone.ShouldNotBe("555-123-4567");
    }

    [Fact]
    public void MaskForAudit_NonGeneric_WhenDisabled_ReturnsOriginal()
    {
        var options = new PIIOptions { MaskInAuditTrails = false };
        var sut = CreateSut(options);
        object dto = new TestDto();

        var result = sut.MaskForAudit(dto);

        result.ShouldBeSameAs(dto);
    }

    [Fact]
    public void MaskForAudit_NonGeneric_NullRequest_ThrowsArgumentNull()
    {
        var sut = CreateSut();

        Should.Throw<ArgumentNullException>(() => sut.MaskForAudit((object)null!));
    }

    #endregion

    #region Constructor Guards

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNull()
    {
        Should.Throw<ArgumentNullException>(() =>
            new PIIMasker(null!, Substitute.For<ILogger<PIIMasker>>(), new ServiceCollection().BuildServiceProvider()));
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNull()
    {
        Should.Throw<ArgumentNullException>(() =>
            new PIIMasker(Options.Create(new PIIOptions()), null!, new ServiceCollection().BuildServiceProvider()));
    }

    [Fact]
    public void Constructor_NullServiceProvider_ThrowsArgumentNull()
    {
        Should.Throw<ArgumentNullException>(() =>
            new PIIMasker(Options.Create(new PIIOptions()), Substitute.For<ILogger<PIIMasker>>(), null!));
    }

    #endregion
}
