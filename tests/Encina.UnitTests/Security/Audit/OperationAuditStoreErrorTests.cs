using Encina.Security.Audit;
using Shouldly;

namespace Encina.UnitTests.Security.Audit;

/// <summary>
/// Unit tests for <see cref="OperationAuditErrors.StoreError"/> (#1322).
/// </summary>
[Trait("Category", "Unit")]
public sealed class OperationAuditStoreErrorTests
{
    [Fact]
    public void StoreErrorCode_IsStable() =>
        OperationAuditErrors.StoreErrorCode.ShouldBe("audit.store_error");

    [Fact]
    public void StoreError_CarriesCodeOperationAndExceptionTypeButNotExceptionMessage()
    {
        // Arrange
        var exception = new InvalidOperationException("SECRET-42");

        // Act
        var error = OperationAuditErrors.StoreError("Record", exception);

        // Assert
        error.GetCode().IfNone(string.Empty).ShouldBe(OperationAuditErrors.StoreErrorCode);
        error.Message.ShouldContain("Record");
        error.Message.ShouldContain(nameof(InvalidOperationException));
        error.Message.ShouldNotContain("SECRET-42");
        error.GetDetails()["operation"].ShouldBe("Record");
    }

    [Fact]
    public void StoreError_AttachesTheException()
    {
        // Arrange
        var exception = new InvalidOperationException("SECRET-42");

        // Act
        var error = OperationAuditErrors.StoreError("Query", exception);

        // Assert
        error.Exception.IsSome.ShouldBeTrue();
        error.Exception.IfSome(ex => ex.ShouldBeSameAs(exception));
    }
}
