using Shouldly;

namespace Encina.Tests;

public sealed class EncinaErrorExtensionsTests
{
    [Fact]
    public void GetEncinaCode_ReturnsEncinaCode_ForEncinaExceptions()
    {
        var error = EncinaErrors.Create("Encina.test", "boom");

        error.GetEncinaCode().ShouldBe("Encina.test");
    }

    [Fact]
    public void GetEncinaCode_ReturnsExceptionTypeName_WhenMetadataIsNonEncina()
    {
        var error = EncinaError.New("boom", new InvalidOperationException("oops"));

        error.GetEncinaCode().ShouldBe(nameof(InvalidOperationException));
    }

    [Fact]
    public void GetEncinaCode_DefaultsToUnknown_WhenMessageIsMissing()
    {
        var error = default(EncinaError);

        error.GetEncinaCode().ShouldBe("Encina.unknown");
    }

    [Fact]
    public void GetEncinaCode_UsesMessage_WhenNoMetadataAndMessagePresent()
    {
        var error = EncinaError.New("custom-code");

        error.GetEncinaCode().ShouldBe("custom-code");
    }

    [Fact]
    public void GetEncinaDetails_ReturnsDetails_FromEncinaException()
    {
        var details = new { Value = 42 };
        var error = EncinaErrors.Create("Encina.details", "boom", details: details);

        error.GetEncinaDetails().ShouldBe(details);
    }

    [Fact]
    public void GetEncinaDetails_ReturnsNull_ForNonEncinaMetadata()
    {
        var error = EncinaError.New("boom", new InvalidOperationException("oops"));

        error.GetEncinaDetails().ShouldBeNull();
    }

    [Fact]
    public void GetEncinaMetadata_ReturnsMetadata_FromEncinaException()
    {
        var details = new Dictionary<string, object?>
        {
            ["handler"] = "TestHandler",
            ["request"] = "TestRequest",
            ["stage"] = "handler"
        };

        var error = EncinaErrors.Create("Encina.metadata", "boom", details: details);

        var metadata = error.GetEncinaMetadata();

        metadata.ShouldNotBeNull();
        metadata.ShouldContainKey("handler");
        metadata["handler"].ShouldBe("TestHandler");
        metadata["stage"].ShouldBe("handler");
    }

    [Fact]
    public void GetEncinaMetadata_ReturnsEmpty_ForNonEncinaMetadata()
    {
        var error = EncinaError.New("boom", new InvalidOperationException("oops"));

        var metadata = error.GetEncinaMetadata();

        metadata.ShouldNotBeNull();
        metadata.ShouldBeEmpty();
    }

    [Fact]
    public void EncinaError_New_WithNullException_ReturnsErrorWithoutException()
    {
        var error = EncinaError.New("test message", (Exception?)null);

        error.Message.ShouldBe("test message");
        error.Exception.IsNone.ShouldBeTrue();
    }

    [Fact]
    public void EncinaError_New_FromNullException_UsesDefaultMessage()
    {
        var error = EncinaError.New((Exception)null!);

        error.Message.ShouldBe("An error occurred");
        error.Exception.IsNone.ShouldBeTrue();
    }

    [Fact]
    public void EncinaError_New_FromNullExceptionWithMessage_UsesProvidedMessage()
    {
        var error = EncinaError.New((Exception)null!, "custom message");

        error.Message.ShouldBe("custom message");
        error.Exception.IsNone.ShouldBeTrue();
    }

    [Fact]
    public void EncinaError_New_WithEmptyMessage_UsesDefaultMessage()
    {
        var error = EncinaError.New("");

        error.Message.ShouldBe("An error occurred");
    }

    [Fact]
    public void EncinaError_New_WithWhitespaceMessage_UsesDefaultMessage()
    {
        var error = EncinaError.New("   ");

        error.Message.ShouldBe("An error occurred");
    }

    [Fact]
    public void EncinaError_ImplicitConversionFromString_CreatesError()
    {
        EncinaError error = "test error";

        error.Message.ShouldBe("test error");
        error.Exception.IsNone.ShouldBeTrue();
    }

    [Fact]
    public void EncinaError_ImplicitConversionFromException_CreatesError()
    {
        var exception = new InvalidOperationException("test exception");
        EncinaError error = exception;

        error.Message.ShouldBe("test exception");
        error.Exception.IsSome.ShouldBeTrue();
    }

    [Fact]
    public void EncinaError_New_WithExceptionWithNullMessage_UsesExceptionMessage()
    {
        var exception = new InvalidOperationException(); // Exception with default message
        var error = EncinaError.New(exception);

        error.Message.ShouldNotBeNullOrWhiteSpace();
        error.Exception.IsSome.ShouldBeTrue();
    }

    [Fact]
    public void EncinaError_New_WithEncinaException_NormalizesInnerException()
    {
        var innerException = new InvalidOperationException("inner");
        var EncinaException = new EncinaException("Encina.test", "wrapper", innerException, details: null);

        var error = EncinaError.New(EncinaException);

        error.Message.ShouldBe("wrapper");
        error.Exception.IsSome.ShouldBeTrue();
        error.Exception.Match(
            Some: ex => ex.ShouldBe(innerException),
            None: () => throw new InvalidOperationException("Expected exception to be present"));
    }

    [Fact]
    public void EncinaErrors_Create_WithNonDictionaryDetails_WrapsInMetadata()
    {
        var customDetail = new { Value = 42, Name = "Test" };
        var error = EncinaErrors.Create("test.code", "test message", details: customDetail);

        error.GetEncinaCode().ShouldBe("test.code");
        error.Message.ShouldBe("test message");

        var details = error.GetEncinaDetails();
        details.ShouldBe(customDetail);

        var metadata = error.GetEncinaMetadata();
        metadata.ShouldNotBeNull();
        metadata.ShouldContainKey("detail");
        metadata["detail"].ShouldBe(customDetail);
    }

    [Fact]
    public void GetEncinaMetadata_ReturnsEmptyDictionary_WhenMetadataIsNull()
    {
        // Create an error with dictionary details that could potentially be null
        var error = EncinaErrors.Create("test.null", "test", details: (IReadOnlyDictionary<string, object?>?)null);

        var metadata = error.GetEncinaMetadata();

        metadata.ShouldNotBeNull();
        metadata.ShouldBeEmpty();
    }
}
