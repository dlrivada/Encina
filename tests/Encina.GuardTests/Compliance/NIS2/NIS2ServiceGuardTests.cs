using Encina.Compliance.NIS2;
using Encina.Compliance.NIS2.Abstractions;
using Encina.Compliance.NIS2.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;

namespace Encina.GuardTests.Compliance.NIS2;

#region DefaultMFAEnforcer

/// <summary>
/// Guard clause tests for <see cref="DefaultMFAEnforcer"/>.
/// The default MFA enforcer is a pass-through — these tests verify it can be
/// instantiated and called without error.
/// </summary>
public sealed class DefaultMFAEnforcerGuardTests
{
    [Fact]
    public void DefaultMFAEnforcer_CanBeResolvedFromDI()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaNIS2();

        var provider = services.BuildServiceProvider();
        var enforcer = provider.GetRequiredService<IMFAEnforcer>();

        enforcer.ShouldNotBeNull();
    }

    [Fact]
    public async Task IsMFAEnabledAsync_AlwaysReturnsTrue()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaNIS2();

        var provider = services.BuildServiceProvider();
        var enforcer = provider.GetRequiredService<IMFAEnforcer>();

        var result = await enforcer.IsMFAEnabledAsync("test-user");

        result.IsRight.ShouldBeTrue();
        result.IfRight(v => v.ShouldBeTrue());
    }
}

#endregion

#region NIS2Options.AddSupplier

/// <summary>
/// Guard clause tests for <see cref="NIS2Options.AddSupplier"/>.
/// </summary>
public sealed class NIS2OptionsAddSupplierGuardTests
{
    [Fact]
    public void AddSupplier_NullSupplierId_ThrowsArgumentNullException()
    {
        var options = new NIS2Options();

        var act = () => options.AddSupplier(null!, _ => { });

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("supplierId");
    }

    [Fact]
    public void AddSupplier_NullConfigure_ThrowsArgumentNullException()
    {
        var options = new NIS2Options();

        var act = () => options.AddSupplier("test", null!);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("configure");
    }

    [Fact]
    public void AddSupplier_ValidInputs_ShouldReturnSameOptionsForChaining()
    {
        var options = new NIS2Options();

        var result = options.AddSupplier("test", s =>
        {
            s.Name = "Test";
            s.RiskLevel = SupplierRiskLevel.Low;
        });

        result.ShouldBeSameAs(options);
    }
}

#endregion

#region NIS2Errors

/// <summary>
/// Guard clause tests for <see cref="NIS2Errors"/> factory methods.
/// Verifies that all error factory methods produce valid <see cref="EncinaError"/> instances.
/// </summary>
public sealed class NIS2ErrorsGuardTests
{
    [Fact]
    public void ComplianceCheckFailed_ShouldCreateError()
    {
        var error = NIS2Errors.ComplianceCheckFailed(5);

        error.Message.ShouldContain("compliance check failed");
    }

    [Fact]
    public void MeasureNotSatisfied_ShouldCreateError()
    {
        var error = NIS2Errors.MeasureNotSatisfied(NIS2Measure.RiskAnalysisAndSecurityPolicies, "Missing");

        error.Message.ShouldContain("RiskAnalysisAndSecurityPolicies");
    }

    [Fact]
    public void MFARequired_ShouldCreateError()
    {
        var error = NIS2Errors.MFARequired("TestRequest");

        error.Message.ShouldContain("TestRequest");
    }

    [Fact]
    public void MFARequired_WithUserId_ShouldCreateError()
    {
        var error = NIS2Errors.MFARequired("TestRequest", "user-1");

        error.Message.ShouldContain("user-1");
    }

    [Fact]
    public void EncryptionRequired_ShouldCreateError()
    {
        var error = NIS2Errors.EncryptionRequired("PII", "at-rest");

        error.Message.ShouldContain("PII");
    }

    [Fact]
    public void SupplierRiskHigh_ShouldCreateError()
    {
        var error = NIS2Errors.SupplierRiskHigh("supplier-1", SupplierRiskLevel.Critical);

        error.Message.ShouldContain("supplier-1");
    }

    [Fact]
    public void SupplyChainCheckFailed_ShouldCreateError()
    {
        var error = NIS2Errors.SupplyChainCheckFailed("supplier-1", "failed");

        error.Message.ShouldContain("supplier-1");
    }

    [Fact]
    public void SupplierNotFound_ShouldCreateError()
    {
        var error = NIS2Errors.SupplierNotFound("unknown-supplier");

        error.Message.ShouldContain("unknown-supplier");
    }

    [Fact]
    public void DeadlineExceeded_ShouldCreateError()
    {
        var error = NIS2Errors.DeadlineExceeded(NIS2NotificationPhase.EarlyWarning, 2.5);

        error.Message.ShouldContain("EarlyWarning");
    }

    [Fact]
    public void IncidentReportFailed_ShouldCreateError()
    {
        var error = NIS2Errors.IncidentReportFailed(Guid.NewGuid(), "failed");

        error.Message.ShouldContain("failed");
    }

    [Fact]
    public void AllPhasesComplete_ShouldCreateError()
    {
        var error = NIS2Errors.AllPhasesComplete(Guid.NewGuid());

        error.Message.ShouldContain("completed");
    }

    [Fact]
    public void ManagementAccountabilityMissing_ShouldCreateError()
    {
        var error = NIS2Errors.ManagementAccountabilityMissing();

        error.Message.ShouldContain("management accountability");
    }

    [Fact]
    public void PipelineBlocked_ShouldCreateError()
    {
        var error = NIS2Errors.PipelineBlocked("TestRequest", "reason");

        error.Message.ShouldContain("TestRequest");
    }

    [Fact]
    public void MeasureEvaluationFailed_ShouldCreateError()
    {
        var error = NIS2Errors.MeasureEvaluationFailed(NIS2Measure.IncidentHandling, "failed");

        error.Message.ShouldContain("IncidentHandling");
    }
}

#endregion

#region NIS2AttributeInfo

/// <summary>
/// Guard clause tests for <see cref="NIS2AttributeInfo"/>.
/// </summary>
public sealed class NIS2AttributeInfoGuardTests
{
    [Fact]
    public void FromType_WithNoAttributes_ShouldHaveNoAttributes()
    {
        var info = NIS2AttributeInfo.FromType(typeof(PlainRequest));

        info.IsNIS2Critical.ShouldBeFalse();
        info.RequiresMFA.ShouldBeFalse();
        info.SupplyChainChecks.ShouldBeEmpty();
        info.HasAnyAttribute.ShouldBeFalse();
    }

    [Fact]
    public void FromType_WithNIS2Critical_ShouldDetectAttribute()
    {
        var info = NIS2AttributeInfo.FromType(typeof(CriticalRequest));

        info.IsNIS2Critical.ShouldBeTrue();
        info.CriticalDescription.ShouldBe("Critical operation");
        info.HasAnyAttribute.ShouldBeTrue();
    }

    [Fact]
    public void FromType_WithRequireMFA_ShouldDetectAttribute()
    {
        var info = NIS2AttributeInfo.FromType(typeof(MFARequest));

        info.RequiresMFA.ShouldBeTrue();
        info.MFAReason.ShouldBe("Admin operation");
        info.HasAnyAttribute.ShouldBeTrue();
    }

    [Fact]
    public void FromType_WithSupplyChainCheck_ShouldDetectAttribute()
    {
        var info = NIS2AttributeInfo.FromType(typeof(SupplyChainRequest));

        info.SupplyChainChecks.ShouldContain("payment-provider");
        info.SupplyChainChecks.Count.ShouldBe(1);
        info.HasAnyAttribute.ShouldBeTrue();
    }

    private sealed record PlainRequest;

    [NIS2Critical(Description = "Critical operation")]
    private sealed record CriticalRequest;

    [RequireMFA(Reason = "Admin operation")]
    private sealed record MFARequest;

    [NIS2SupplyChainCheck("payment-provider")]
    private sealed record SupplyChainRequest;
}

#endregion

#region NIS2SupplyChainCheckAttribute

/// <summary>
/// Guard clause tests for <see cref="NIS2SupplyChainCheckAttribute"/>.
/// </summary>
public sealed class NIS2SupplyChainCheckAttributeGuardTests
{
    [Fact]
    public void Constructor_NullSupplierId_ThrowsArgumentNullException()
    {
        var act = () => new NIS2SupplyChainCheckAttribute(null!);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("supplierId");
    }

    [Fact]
    public void Constructor_ValidSupplierId_ShouldSetProperty()
    {
        var attr = new NIS2SupplyChainCheckAttribute("supplier-1");

        attr.SupplierId.ShouldBe("supplier-1");
        attr.MinimumRiskLevel.ShouldBe(SupplierRiskLevel.Medium);
    }
}

#endregion
