using System.Reflection;
using Encina.Compliance.LawfulBasis;
using GdprLawfulBasis = global::Encina.Compliance.GDPR.LawfulBasis;

namespace Encina.UnitTests.Compliance.LawfulBasisModule;

public class LawfulBasisOptionsTests
{
    [Fact]
    public void Defaults_EnforcementMode_IsBlock()
    {
        var options = new LawfulBasisOptions();
        options.EnforcementMode.ShouldBe(LawfulBasisEnforcementMode.Block);
    }

    [Fact]
    public void Defaults_RequireDeclaredBasis_IsTrue()
    {
        var options = new LawfulBasisOptions();
        options.RequireDeclaredBasis.ShouldBeTrue();
    }

    [Fact]
    public void Defaults_ValidateConsentForConsentBasis_IsTrue()
    {
        var options = new LawfulBasisOptions();
        options.ValidateConsentForConsentBasis.ShouldBeTrue();
    }

    [Fact]
    public void Defaults_ValidateLIAForLegitimateInterests_IsTrue()
    {
        var options = new LawfulBasisOptions();
        options.ValidateLIAForLegitimateInterests.ShouldBeTrue();
    }

    [Fact]
    public void Defaults_AutoRegisterFromAttributes_IsTrue()
    {
        var options = new LawfulBasisOptions();
        options.AutoRegisterFromAttributes.ShouldBeTrue();
    }

    [Fact]
    public void Defaults_AddHealthCheck_IsFalse()
    {
        var options = new LawfulBasisOptions();
        options.AddHealthCheck.ShouldBeFalse();
    }

    [Fact]
    public void Defaults_AssembliesToScan_IsEmpty()
    {
        var options = new LawfulBasisOptions();
        options.AssembliesToScan.ShouldBeEmpty();
    }

    [Fact]
    public void Defaults_DefaultBases_IsEmpty()
    {
        var options = new LawfulBasisOptions();
        options.DefaultBases.ShouldBeEmpty();
    }

    [Fact]
    public void DefaultBasis_RegistersTypeAndBasis()
    {
        var options = new LawfulBasisOptions();
        options.DefaultBasis<string>(GdprLawfulBasis.Contract);

        options.DefaultBases.ShouldContainKey(typeof(string));
        options.DefaultBases[typeof(string)].ShouldBe(GdprLawfulBasis.Contract);
    }

    [Fact]
    public void DefaultBasis_ReturnsSameInstance_ForChaining()
    {
        var options = new LawfulBasisOptions();
        var result = options.DefaultBasis<string>(GdprLawfulBasis.Consent);
        result.ShouldBeSameAs(options);
    }

    [Fact]
    public void ScanAssembly_AddsToSet()
    {
        var options = new LawfulBasisOptions();
        var asm = typeof(LawfulBasisOptions).Assembly;
        options.ScanAssembly(asm);

        options.AssembliesToScan.ShouldContain(asm);
    }

    [Fact]
    public void ScanAssembly_NullAssembly_ThrowsArgumentNullException()
    {
        var options = new LawfulBasisOptions();
        Should.Throw<ArgumentNullException>(() => options.ScanAssembly(null!));
    }

    [Fact]
    public void ScanAssembly_ReturnsSameInstance_ForChaining()
    {
        var options = new LawfulBasisOptions();
        var result = options.ScanAssembly(typeof(LawfulBasisOptions).Assembly);
        result.ShouldBeSameAs(options);
    }

    [Fact]
    public void ScanAssemblyContaining_AddsAssemblyOfType()
    {
        var options = new LawfulBasisOptions();
        options.ScanAssemblyContaining<LawfulBasisOptions>();

        options.AssembliesToScan.ShouldContain(typeof(LawfulBasisOptions).Assembly);
    }

    [Fact]
    public void ScanAssemblyContaining_ReturnsSameInstance_ForChaining()
    {
        var options = new LawfulBasisOptions();
        var result = options.ScanAssemblyContaining<LawfulBasisOptions>();
        result.ShouldBeSameAs(options);
    }

    [Fact]
    public void ScanAssembly_DuplicateAssembly_DoesNotDuplicate()
    {
        var options = new LawfulBasisOptions();
        var asm = typeof(LawfulBasisOptions).Assembly;
        options.ScanAssembly(asm);
        options.ScanAssembly(asm);

        options.AssembliesToScan.Count.ShouldBe(1);
    }
}
