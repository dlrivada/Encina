using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.Model;

using Shouldly;

namespace Encina.UnitTests.Compliance.DataResidency;

public class ResidencyPolicyBuilderTests
{
    [Fact]
    public void AddPolicy_BasicConfiguration_CreatesPolicyEntry()
    {
        var options = new DataResidencyOptions();

        options.AddPolicy("healthcare-data", builder =>
        {
            builder.AllowRegions(RegionRegistry.DE, RegionRegistry.FR);
        });

        options.ConfiguredPolicies.Count.ShouldBe(1);
        options.ConfiguredPolicies[0].DataCategory.ShouldBe("healthcare-data");
        options.ConfiguredPolicies[0].AllowedRegions.Count.ShouldBe(2);
    }

    [Fact]
    public void AddPolicy_AllowEU_AddsEUMemberStates()
    {
        var options = new DataResidencyOptions();

        options.AddPolicy("personal-data", builder =>
        {
            builder.AllowEU();
        });

        options.ConfiguredPolicies[0].AllowedRegions.ShouldNotBeEmpty();
    }

    [Fact]
    public void AddPolicy_AllowEEA_AddsEEACountries()
    {
        var options = new DataResidencyOptions();

        options.AddPolicy("personal-data", builder =>
        {
            builder.AllowEEA();
        });

        options.ConfiguredPolicies[0].AllowedRegions.ShouldNotBeEmpty();
    }

    [Fact]
    public void AddPolicy_AllowAdequate_AddsAdequacyCountries()
    {
        var options = new DataResidencyOptions();

        options.AddPolicy("personal-data", builder =>
        {
            builder.AllowAdequate();
        });

        options.ConfiguredPolicies[0].AllowedRegions.ShouldNotBeEmpty();
    }

    [Fact]
    public void AddPolicy_RequireAdequacyDecision_SetsFlag()
    {
        var options = new DataResidencyOptions();

        options.AddPolicy("sensitive-data", builder =>
        {
            builder.AllowEU();
            builder.RequireAdequacyDecision();
        });

        options.ConfiguredPolicies[0].RequireAdequacyDecision.ShouldBeTrue();
    }

    [Fact]
    public void AddPolicy_AllowTransferBasis_AddsBases()
    {
        var options = new DataResidencyOptions();

        options.AddPolicy("corporate-data", builder =>
        {
            builder.AllowRegions(RegionRegistry.DE);
            builder.AllowTransferBasis(
                TransferLegalBasis.StandardContractualClauses,
                TransferLegalBasis.BindingCorporateRules);
        });

        options.ConfiguredPolicies[0].AllowedTransferBases.Count.ShouldBe(2);
    }

    [Fact]
    public void AddPolicy_Chaining_WorksCorrectly()
    {
        var options = new DataResidencyOptions();

        options
            .AddPolicy("data-1", b => b.AllowEU())
            .AddPolicy("data-2", b => b.AllowRegions(RegionRegistry.US));

        options.ConfiguredPolicies.Count.ShouldBe(2);
    }

    [Fact]
    public void AddPolicy_DuplicateRegions_DeduplicatesInBuild()
    {
        var options = new DataResidencyOptions();

        options.AddPolicy("test", builder =>
        {
            builder.AllowRegions(RegionRegistry.DE, RegionRegistry.DE, RegionRegistry.FR);
        });

        options.ConfiguredPolicies[0].AllowedRegions.ShouldBeUnique();
    }

    [Fact]
    public void AddPolicy_NullDataCategory_ThrowsArgumentNullException()
    {
        var options = new DataResidencyOptions();

        var act = () => options.AddPolicy(null!, _ => { });

        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void AddPolicy_NullConfigure_ThrowsArgumentNullException()
    {
        var options = new DataResidencyOptions();

        var act = () => options.AddPolicy("test", null!);

        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void AllowRegions_NullRegions_ThrowsArgumentNullException()
    {
        var options = new DataResidencyOptions();

        var act = () => options.AddPolicy("test", builder =>
        {
            builder.AllowRegions(null!);
        });

        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void AllowTransferBasis_NullBases_ThrowsArgumentNullException()
    {
        var options = new DataResidencyOptions();

        var act = () => options.AddPolicy("test", builder =>
        {
            builder.AllowTransferBasis(null!);
        });

        Should.Throw<ArgumentNullException>(act);
    }
}
