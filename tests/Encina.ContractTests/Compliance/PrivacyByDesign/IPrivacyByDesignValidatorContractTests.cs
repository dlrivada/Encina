#pragma warning disable CA1859 // Contract tests intentionally use interface types

using Encina.Compliance.PrivacyByDesign;
using Encina.Compliance.PrivacyByDesign.Model;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.ContractTests.Compliance.PrivacyByDesign;

/// <summary>
/// Contract tests for <see cref="IPrivacyByDesignValidator"/> verifying the
/// <see cref="DefaultPrivacyByDesignValidator"/> implementation behaves correctly
/// through the interface contract.
/// </summary>
[Trait("Category", "Contract")]
public class IPrivacyByDesignValidatorContractTests
{
    private readonly IPrivacyByDesignValidator _validator;

    public IPrivacyByDesignValidatorContractTests()
    {
        var analyzer = new DefaultDataMinimizationAnalyzer(
            TimeProvider.System,
            NullLogger<DefaultDataMinimizationAnalyzer>.Instance);

        var registry = new InMemoryPurposeRegistry(
            NullLogger<InMemoryPurposeRegistry>.Instance);

        _validator = new DefaultPrivacyByDesignValidator(
            analyzer,
            registry,
            TimeProvider.System,
            NullLogger<DefaultPrivacyByDesignValidator>.Instance);
    }

    #region Test Types

    [EnforceDataMinimization]
    public class ContractTestRequest
    {
        public string Name { get; set; } = "";

        [NotStrictlyNecessary(Reason = "Analytics")]
        public string? ReferralSource { get; set; }
    }

    [EnforceDataMinimization]
    public class ContractTestRequestWithDefaults
    {
        public string Name { get; set; } = "";

        [PrivacyDefault(false)]
        public bool ShareWithThirdParties { get; set; }
    }

    #endregion

    #region ValidateAsync Contract

    [Fact]
    public async Task Contract_ValidateAsync_ReturnsRight_WithCompliantResult()
    {
        IPrivacyByDesignValidator validator = _validator;
        var request = new ContractTestRequest { Name = "Test" };

        var result = await validator.ValidateAsync(request);
        result.IsRight.ShouldBeTrue("ValidateAsync should return Right");

        var validation = result.Match(v => v, _ => null!);
        validation.ShouldNotBeNull();
        validation.IsCompliant.ShouldBeTrue("Request without unnecessary field values should be compliant");
        validation.Violations.Count.ShouldBe(0);
        validation.MinimizationReport.ShouldNotBeNull();
    }

    [Fact]
    public async Task Contract_ValidateAsync_ReturnsRight_WithViolations()
    {
        IPrivacyByDesignValidator validator = _validator;
        var request = new ContractTestRequest
        {
            Name = "Test",
            ReferralSource = "Google" // Unnecessary field has a value
        };

        var result = await validator.ValidateAsync(request);
        result.IsRight.ShouldBeTrue("ValidateAsync should return Right even with violations");

        var validation = result.Match(v => v, _ => null!);
        validation.ShouldNotBeNull();
        validation.IsCompliant.ShouldBeFalse("Request with unnecessary field value should not be compliant");
        validation.Violations.Count.ShouldBeGreaterThan(0);
        validation.Violations.ShouldContain(v => v.FieldName == "ReferralSource");
        validation.Violations.ShouldContain(v => v.ViolationType == PrivacyViolationType.DataMinimization);
    }

    #endregion

    #region AnalyzeMinimizationAsync Contract

    [Fact]
    public async Task Contract_AnalyzeMinimizationAsync_ReturnsRight_WithReport()
    {
        IPrivacyByDesignValidator validator = _validator;
        var request = new ContractTestRequest { Name = "Test" };

        var result = await validator.AnalyzeMinimizationAsync(request);
        result.IsRight.ShouldBeTrue("AnalyzeMinimizationAsync should return Right");

        var report = result.Match(r => r, _ => null!);
        report.ShouldNotBeNull();
        report.NecessaryFields.Count.ShouldBeGreaterThan(0);
        report.UnnecessaryFields.Count.ShouldBeGreaterThan(0);
        report.MinimizationScore.ShouldBeGreaterThan(0.0);
        report.MinimizationScore.ShouldBeLessThanOrEqualTo(1.0);
    }

    #endregion

    #region ValidateDefaultsAsync Contract

    [Fact]
    public async Task Contract_ValidateDefaultsAsync_ReturnsRight_WithDefaults()
    {
        IPrivacyByDesignValidator validator = _validator;
        var request = new ContractTestRequestWithDefaults
        {
            Name = "Test",
            ShareWithThirdParties = true // Deviates from default (false)
        };

        var result = await validator.ValidateDefaultsAsync(request);
        result.IsRight.ShouldBeTrue("ValidateDefaultsAsync should return Right");

        var defaults = result.Match(d => d, _ => []);
        defaults.Count.ShouldBeGreaterThan(0);
        defaults.ShouldContain(d => d.FieldName == "ShareWithThirdParties");
        defaults.ShouldContain(d => !d.MatchesDefault);
    }

    #endregion
}
