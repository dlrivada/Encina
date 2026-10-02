using Encina.Security.PII;

namespace Encina.GuardTests.Security.PII;

public sealed class PIIOptionsValidatorGuardTests
{
    [Fact]
    public void Validate_NullOptions_ThrowsArgumentNullException()
    {
        var sut = new PIIOptionsValidator();

        Should.Throw<ArgumentNullException>(() => sut.Validate(null, null!))
            .ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_NullLogger_FallsBackToNullLogger()
    {
        var sut = new PIIOptionsValidator(null);

        sut.Validate(null, new PIIOptions { AllowUnkeyedHash = true }).Succeeded.ShouldBeTrue();
    }
}
