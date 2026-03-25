using Encina.Compliance.LawfulBasis.Errors;
using FluentAssertions;
using LanguageExt;

namespace Encina.UnitTests.Compliance.LawfulBasisModule;

public class LawfulBasisErrorsTests
{
    [Fact]
    public void RegistrationNotFound_ShouldCreateErrorWithCorrectCode()
    {
        var id = Guid.NewGuid();

        var error = LawfulBasisErrors.RegistrationNotFound(id);

        error.GetCode().Match(c => c, () => "").Should().Be(LawfulBasisErrors.RegistrationNotFoundCode);
        error.Message.Should().Contain(id.ToString());
    }

    [Fact]
    public void RegistrationAlreadyRevoked_ShouldCreateErrorWithCorrectCode()
    {
        var id = Guid.NewGuid();

        var error = LawfulBasisErrors.RegistrationAlreadyRevoked(id);

        error.GetCode().Match(c => c, () => "").Should().Be(LawfulBasisErrors.RegistrationAlreadyRevokedCode);
        error.Message.Should().Contain(id.ToString());
    }

    [Fact]
    public void LIANotFound_ShouldCreateErrorWithCorrectCode()
    {
        var id = Guid.NewGuid();

        var error = LawfulBasisErrors.LIANotFound(id);

        error.GetCode().Match(c => c, () => "").Should().Be(LawfulBasisErrors.LIANotFoundCode);
        error.Message.Should().Contain(id.ToString());
    }

    [Fact]
    public void LIANotFoundByReference_ShouldCreateErrorWithCorrectCode()
    {
        var reference = "LIA-2024-001";

        var error = LawfulBasisErrors.LIANotFoundByReference(reference);

        error.GetCode().Match(c => c, () => "").Should().Be(LawfulBasisErrors.LIANotFoundByReferenceCode);
        error.Message.Should().Contain(reference);
    }

    [Fact]
    public void LIAAlreadyDecided_ShouldCreateErrorWithCorrectCode()
    {
        var id = Guid.NewGuid();

        var error = LawfulBasisErrors.LIAAlreadyDecided(id);

        error.GetCode().Match(c => c, () => "").Should().Be(LawfulBasisErrors.LIAAlreadyDecidedCode);
        error.Message.Should().Contain(id.ToString());
    }

    [Fact]
    public void InvalidStateTransition_ShouldCreateErrorWithCorrectCode()
    {
        var error = LawfulBasisErrors.InvalidStateTransition("ApproveAsync", "Already approved");

        error.GetCode().Match(c => c, () => "").Should().Be(LawfulBasisErrors.InvalidStateTransitionCode);
        error.Message.Should().Contain("ApproveAsync");
        error.Message.Should().Contain("Already approved");
    }

    [Fact]
    public void StoreError_ShouldCreateErrorWithCorrectCode()
    {
        var exception = new InvalidOperationException("Connection failed");

        var error = LawfulBasisErrors.StoreError("RegisterAsync", exception);

        error.GetCode().Match(c => c, () => "").Should().Be(LawfulBasisErrors.StoreErrorCode);
        error.Message.Should().Contain("RegisterAsync");
        error.Message.Should().Contain("Connection failed");
    }

    [Fact]
    public void ErrorCodes_ShouldFollowNamingConvention()
    {
        LawfulBasisErrors.RegistrationNotFoundCode.Should().StartWith("lawfulbasis.");
        LawfulBasisErrors.RegistrationAlreadyRevokedCode.Should().StartWith("lawfulbasis.");
        LawfulBasisErrors.LIANotFoundCode.Should().StartWith("lawfulbasis.");
        LawfulBasisErrors.LIANotFoundByReferenceCode.Should().StartWith("lawfulbasis.");
        LawfulBasisErrors.LIAAlreadyDecidedCode.Should().StartWith("lawfulbasis.");
        LawfulBasisErrors.InvalidStateTransitionCode.Should().StartWith("lawfulbasis.");
        LawfulBasisErrors.StoreErrorCode.Should().StartWith("lawfulbasis.");
    }
}
