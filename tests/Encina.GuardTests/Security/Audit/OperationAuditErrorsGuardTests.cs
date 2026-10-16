using Encina.Security.Audit;
using Shouldly;

namespace Encina.GuardTests.Security.Audit;

/// <summary>
/// Guard clause tests for <see cref="OperationAuditErrors"/>.
/// </summary>
public class OperationAuditErrorsGuardTests
{
    [Fact]
    public void MaskingFailed_NullRequestType_ThrowsArgumentNullException()
    {
        Action act = () => OperationAuditErrors.MaskingFailed(null!, "cause");

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("requestType");
    }

    [Fact]
    public void MaskingFailed_NullCauseCode_ThrowsArgumentNullException()
    {
        Action act = () => OperationAuditErrors.MaskingFailed("request", null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("causeCode");
    }

    [Fact]
    public void WriteFailed_NullRequestType_ThrowsArgumentNullException()
    {
        Action act = () => OperationAuditErrors.WriteFailed(null!, "cause");

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("requestType");
    }

    [Fact]
    public void WriteFailed_NullCauseCode_ThrowsArgumentNullException()
    {
        Action act = () => OperationAuditErrors.WriteFailed("request", null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("causeCode");
    }
}
