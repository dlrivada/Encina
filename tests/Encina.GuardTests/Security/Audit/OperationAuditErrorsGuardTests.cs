using Encina.Security.Audit;
using Shouldly;

namespace Encina.GuardTests.Security.Audit;

/// <summary>Guard clauses of <see cref="OperationAuditErrors"/>.</summary>
[Trait("Category", "Guard")]
public sealed class OperationAuditErrorsGuardTests
{
    [Fact]
    public void WriteFailed_NullRequestType_ThrowsArgumentNullException() =>
        Should.Throw<ArgumentNullException>(() => OperationAuditErrors.WriteFailed(null!, "cause"))
            .ParamName.ShouldBe("requestType");

    [Fact]
    public void WriteFailed_NullCauseCode_ThrowsArgumentNullException() =>
        Should.Throw<ArgumentNullException>(() => OperationAuditErrors.WriteFailed("Request", null!))
            .ParamName.ShouldBe("causeCode");
}
