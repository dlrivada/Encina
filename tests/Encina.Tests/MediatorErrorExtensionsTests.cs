using Shouldly;

namespace Encina.Tests;

public sealed class MediatorErrorExtensionsTests
{
    [Fact]
    public void GetMediatorCode_ReturnsMediatorCode_ForMediatorExceptions()
    {
        var error = MediatorErrors.Create("mediator.test", "boom");

        error.GetMediatorCode().ShouldBe("mediator.test");
    }

    [Fact]
    public void GetMediatorCode_ReturnsExceptionTypeName_WhenMetadataIsNonMediator()
    {
        var error = MediatorError.New("boom", new InvalidOperationException("oops"));

        error.GetMediatorCode().ShouldBe(nameof(InvalidOperationException));
    }

    [Fact]
    public void GetMediatorCode_DefaultsToUnknown_WhenMessageIsMissing()
    {
        var error = default(MediatorError);

        error.GetMediatorCode().ShouldBe("mediator.unknown");
    }

    [Fact]
    public void GetMediatorCode_UsesMessage_WhenNoMetadataAndMessagePresent()
    {
        var error = MediatorError.New("custom-code");

        error.GetMediatorCode().ShouldBe("custom-code");
    }

    [Fact]
    public void GetMediatorDetails_ReturnsDetails_FromMediatorException()
    {
        var details = new { Value = 42 };
        var error = MediatorErrors.Create("mediator.details", "boom", details: details);

        error.GetMediatorDetails().ShouldBe(details);
    }

    [Fact]
    public void GetMediatorDetails_ReturnsNull_ForNonMediatorMetadata()
    {
        var error = MediatorError.New("boom", new InvalidOperationException("oops"));

        error.GetMediatorDetails().ShouldBeNull();
    }

    [Fact]
    public void GetMediatorMetadata_ReturnsMetadata_FromMediatorException()
    {
        var details = new Dictionary<string, object?>
        {
            ["handler"] = "TestHandler",
            ["request"] = "TestRequest",
            ["stage"] = "handler"
        };

        var error = MediatorErrors.Create("mediator.metadata", "boom", details: details);

        var metadata = error.GetMediatorMetadata();

        metadata.ShouldNotBeNull();
        metadata.ShouldContainKey("handler");
        metadata["handler"].ShouldBe("TestHandler");
        metadata["stage"].ShouldBe("handler");
    }

    [Fact]
    public void GetMediatorMetadata_ReturnsEmpty_ForNonMediatorMetadata()
    {
        var error = MediatorError.New("boom", new InvalidOperationException("oops"));

        var metadata = error.GetMediatorMetadata();

        metadata.ShouldNotBeNull();
        metadata.ShouldBeEmpty();
    }

    [Fact]
    public void MediatorError_New_WithNullException_ReturnsErrorWithoutException()
    {
        var error = MediatorError.New("test message", (Exception?)null);

        error.Message.ShouldBe("test message");
        error.Exception.IsNone.ShouldBeTrue();
    }

    [Fact]
    public void MediatorError_New_FromNullException_UsesDefaultMessage()
    {
        var error = MediatorError.New((Exception)null!);

        error.Message.ShouldBe("An error occurred");
        error.Exception.IsNone.ShouldBeTrue();
    }

    [Fact]
    public void MediatorError_New_FromNullExceptionWithMessage_UsesProvidedMessage()
    {
        var error = MediatorError.New((Exception)null!, "custom message");

        error.Message.ShouldBe("custom message");
        error.Exception.IsNone.ShouldBeTrue();
    }

    [Fact]
    public void MediatorError_New_WithEmptyMessage_UsesDefaultMessage()
    {
        var error = MediatorError.New("");

        error.Message.ShouldBe("An error occurred");
    }

    [Fact]
    public void MediatorError_New_WithWhitespaceMessage_UsesDefaultMessage()
    {
        var error = MediatorError.New("   ");

        error.Message.ShouldBe("An error occurred");
    }

    [Fact]
    public void MediatorError_ImplicitConversionFromString_CreatesError()
    {
        MediatorError error = "test error";

        error.Message.ShouldBe("test error");
        error.Exception.IsNone.ShouldBeTrue();
    }

    [Fact]
    public void MediatorError_ImplicitConversionFromException_CreatesError()
    {
        var exception = new InvalidOperationException("test exception");
        MediatorError error = exception;

        error.Message.ShouldBe("test exception");
        error.Exception.IsSome.ShouldBeTrue();
    }

    [Fact]
    public void MediatorError_New_WithExceptionWithNullMessage_UsesExceptionMessage()
    {
        var exception = new InvalidOperationException(); // Exception with default message
        var error = MediatorError.New(exception);

        error.Message.ShouldNotBeNullOrWhiteSpace();
        error.Exception.IsSome.ShouldBeTrue();
    }

    [Fact]
    public void MediatorError_New_WithMediatorException_NormalizesInnerException()
    {
        var innerException = new InvalidOperationException("inner");
        var mediatorException = new MediatorException("mediator.test", "wrapper", innerException, details: null);

        var error = MediatorError.New(mediatorException);

        error.Message.ShouldBe("wrapper");
        error.Exception.IsSome.ShouldBeTrue();
        error.Exception.Match(
            Some: ex => ex.ShouldBe(innerException),
            None: () => throw new InvalidOperationException("Expected exception to be present"));
    }

    [Fact]
    public void MediatorErrors_Create_WithNonDictionaryDetails_WrapsInMetadata()
    {
        var customDetail = new { Value = 42, Name = "Test" };
        var error = MediatorErrors.Create("test.code", "test message", details: customDetail);

        error.GetMediatorCode().ShouldBe("test.code");
        error.Message.ShouldBe("test message");

        var details = error.GetMediatorDetails();
        details.ShouldBe(customDetail);

        var metadata = error.GetMediatorMetadata();
        metadata.ShouldNotBeNull();
        metadata.ShouldContainKey("detail");
        metadata["detail"].ShouldBe(customDetail);
    }

    [Fact]
    public void GetMediatorMetadata_ReturnsEmptyDictionary_WhenMetadataIsNull()
    {
        // Create an error with dictionary details that could potentially be null
        var error = MediatorErrors.Create("test.null", "test", details: (IReadOnlyDictionary<string, object?>?)null);

        var metadata = error.GetMediatorMetadata();

        metadata.ShouldNotBeNull();
        metadata.ShouldBeEmpty();
    }
}
