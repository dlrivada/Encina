using Encina.Compliance.LawfulBasis.Errors;
using Shouldly;
using LanguageExt;

namespace Encina.UnitTests.Compliance.LawfulBasisModule;

public class LawfulBasisErrorsTests
{
    [Fact]
    public void RegistrationNotFound_ShouldCreateErrorWithCorrectCode()
    {
        var id = Guid.NewGuid();

        var error = LawfulBasisErrors.RegistrationNotFound(id);

        error.GetCode().Match(c => c, () => "").ShouldBe(LawfulBasisErrors.RegistrationNotFoundCode);
        error.Message.ShouldContain(id.ToString());
    }

    [Fact]
    public void RegistrationAlreadyRevoked_ShouldCreateErrorWithCorrectCode()
    {
        var id = Guid.NewGuid();

        var error = LawfulBasisErrors.RegistrationAlreadyRevoked(id);

        error.GetCode().Match(c => c, () => "").ShouldBe(LawfulBasisErrors.RegistrationAlreadyRevokedCode);
        error.Message.ShouldContain(id.ToString());
    }

    [Fact]
    public void LIANotFound_ShouldCreateErrorWithCorrectCode()
    {
        var id = Guid.NewGuid();

        var error = LawfulBasisErrors.LIANotFound(id);

        error.GetCode().Match(c => c, () => "").ShouldBe(LawfulBasisErrors.LIANotFoundCode);
        error.Message.ShouldContain(id.ToString());
    }

    [Fact]
    public void LIANotFoundByReference_ShouldCreateErrorWithCorrectCode()
    {
        var reference = "LIA-2024-001";

        var error = LawfulBasisErrors.LIANotFoundByReference(reference);

        error.GetCode().Match(c => c, () => "").ShouldBe(LawfulBasisErrors.LIANotFoundByReferenceCode);
        error.Message.ShouldContain(reference);
    }

    [Fact]
    public void LIAAlreadyDecided_ShouldCreateErrorWithCorrectCode()
    {
        var id = Guid.NewGuid();

        var error = LawfulBasisErrors.LIAAlreadyDecided(id);

        error.GetCode().Match(c => c, () => "").ShouldBe(LawfulBasisErrors.LIAAlreadyDecidedCode);
        error.Message.ShouldContain(id.ToString());
    }

    [Fact]
    public void InvalidStateTransition_ShouldCreateErrorWithCorrectCode()
    {
        var error = LawfulBasisErrors.InvalidStateTransition("ApproveAsync", "Already approved");

        error.GetCode().Match(c => c, () => "").ShouldBe(LawfulBasisErrors.InvalidStateTransitionCode);
        error.Message.ShouldContain("ApproveAsync");
        error.Message.ShouldContain("Already approved");
    }

    [Fact]
    public void StoreError_ShouldCreateErrorWithCorrectCode()
    {
        var exception = new InvalidOperationException("Connection failed");

        var error = LawfulBasisErrors.StoreError("RegisterAsync", exception);

        error.GetCode().Match(c => c, () => "").ShouldBe(LawfulBasisErrors.StoreErrorCode);
        error.Message.ShouldContain("RegisterAsync");
        error.Message.ShouldContain("Connection failed");
    }

    [Fact]
    public void ErrorCodes_ShouldFollowNamingConvention()
    {
        LawfulBasisErrors.RegistrationNotFoundCode.ShouldStartWith("lawfulbasis.");
        LawfulBasisErrors.RegistrationAlreadyRevokedCode.ShouldStartWith("lawfulbasis.");
        LawfulBasisErrors.LIANotFoundCode.ShouldStartWith("lawfulbasis.");
        LawfulBasisErrors.LIANotFoundByReferenceCode.ShouldStartWith("lawfulbasis.");
        LawfulBasisErrors.LIAAlreadyDecidedCode.ShouldStartWith("lawfulbasis.");
        LawfulBasisErrors.InvalidStateTransitionCode.ShouldStartWith("lawfulbasis.");
        LawfulBasisErrors.StoreErrorCode.ShouldStartWith("lawfulbasis.");
    }
}
