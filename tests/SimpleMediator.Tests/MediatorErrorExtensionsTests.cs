using System;
using Shouldly;
using SimpleMediator;

namespace SimpleMediator.Tests;

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
        var error = Error.New("boom", new InvalidOperationException("oops"));

        error.GetMediatorCode().ShouldBe(nameof(InvalidOperationException));
    }

    [Fact]
    public void GetMediatorCode_DefaultsToUnknown_WhenMessageIsMissing()
    {
        var error = default(Error);

        error.GetMediatorCode().ShouldBe("mediator.unknown");
    }

    [Fact]
    public void GetMediatorCode_UsesMessage_WhenNoMetadataAndMessagePresent()
    {
        var error = Error.New("custom-code");

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
        var error = Error.New("boom", new InvalidOperationException("oops"));

        error.GetMediatorDetails().ShouldBeNull();
    }
}
