using Encina.Compliance.LawfulBasis.Events;

using GDPRLawfulBasis = global::Encina.Compliance.GDPR.LawfulBasis;

namespace Encina.UnitTests.Compliance.LawfulBasisModule.Events;

/// <summary>
/// Unit tests for lawful basis event records.
/// </summary>
public class LawfulBasisEventsTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void LawfulBasisRegistered_StoresAllProperties()
    {
        var registrationId = Guid.NewGuid();
        var evt = new LawfulBasisRegistered(
            registrationId,
            "MyApp.Command",
            GDPRLawfulBasis.Contract,
            "Order fulfillment",
            "LIA-001",
            "EU VAT",
            "ToS-v1",
            Now,
            "tenant-1",
            "module-1");

        evt.RegistrationId.ShouldBe(registrationId);
        evt.RequestTypeName.ShouldBe("MyApp.Command");
        evt.Basis.ShouldBe(GDPRLawfulBasis.Contract);
        evt.Purpose.ShouldBe("Order fulfillment");
        evt.LIAReference.ShouldBe("LIA-001");
        evt.LegalReference.ShouldBe("EU VAT");
        evt.ContractReference.ShouldBe("ToS-v1");
        evt.RegisteredAtUtc.ShouldBe(Now);
        evt.TenantId.ShouldBe("tenant-1");
        evt.ModuleId.ShouldBe("module-1");
    }

    [Fact]
    public void LawfulBasisRegistered_RecordEquality_WorksByValue()
    {
        var id = Guid.NewGuid();
        var a = new LawfulBasisRegistered(id, "T", GDPRLawfulBasis.Contract, null, null, null, null, Now, null, null);
        var b = new LawfulBasisRegistered(id, "T", GDPRLawfulBasis.Contract, null, null, null, null, Now, null, null);
        a.ShouldBe(b);
    }

    [Fact]
    public void LawfulBasisChanged_StoresAllProperties()
    {
        var registrationId = Guid.NewGuid();
        var evt = new LawfulBasisChanged(
            registrationId,
            GDPRLawfulBasis.Consent,
            GDPRLawfulBasis.Contract,
            "New purpose",
            null,
            null,
            "ToS-v2",
            Now,
            "tenant-1",
            null);

        evt.RegistrationId.ShouldBe(registrationId);
        evt.OldBasis.ShouldBe(GDPRLawfulBasis.Consent);
        evt.NewBasis.ShouldBe(GDPRLawfulBasis.Contract);
        evt.Purpose.ShouldBe("New purpose");
        evt.ContractReference.ShouldBe("ToS-v2");
        evt.ChangedAtUtc.ShouldBe(Now);
    }

    [Fact]
    public void LawfulBasisRevoked_StoresAllProperties()
    {
        var registrationId = Guid.NewGuid();
        var evt = new LawfulBasisRevoked(
            registrationId,
            "No longer required",
            Now,
            "tenant-1",
            "module-1");

        evt.RegistrationId.ShouldBe(registrationId);
        evt.Reason.ShouldBe("No longer required");
        evt.RevokedAtUtc.ShouldBe(Now);
        evt.TenantId.ShouldBe("tenant-1");
        evt.ModuleId.ShouldBe("module-1");
    }
}
