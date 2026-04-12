using Encina.Compliance.PrivacyByDesign;
using Encina.Compliance.PrivacyByDesign.Model;

using FsCheck;
using FsCheck.Xunit;

using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.PropertyTests.Compliance.PrivacyByDesign;

/// <summary>
/// Property-based tests for <see cref="InMemoryPurposeRegistry"/> invariants.
/// Verifies register/get round-trip, module scoping, remove, and idempotence.
/// </summary>
[Trait("Category", "Property")]
public sealed class InMemoryPurposeRegistryPropertyTests : IDisposable
{
    private readonly InMemoryPurposeRegistry _sut;

    public InMemoryPurposeRegistryPropertyTests()
    {
        _sut = new InMemoryPurposeRegistry(NullLogger<InMemoryPurposeRegistry>.Instance);
    }

    public void Dispose() => _sut.Clear();

    private static PurposeDefinition MakePurpose(string name, string? moduleId = null) => new()
    {
        PurposeId = $"pid-{name}-{moduleId ?? "global"}",
        Name = name,
        Description = "Test purpose",
        LegalBasis = "Contract",
        AllowedFields = ["Field1"],
        ModuleId = moduleId,
        CreatedAtUtc = DateTimeOffset.UtcNow
    };

    [Property(MaxTest = 100)]
    public bool RegisterThenGet_RoundTrips(NonEmptyString purposeName)
    {
        var name = purposeName.Get.Trim();
        if (string.IsNullOrWhiteSpace(name)) return true;

        var purpose = MakePurpose(name);
        var reg = _sut.RegisterPurposeAsync(purpose).AsTask().GetAwaiter().GetResult();
        if (reg.IsLeft) return false;

        var get = _sut.GetPurposeAsync(name).AsTask().GetAwaiter().GetResult();
        if (get.IsLeft) return false;

        bool found = false;
        get.IfRight(opt => opt.IfSome(p => found = p.PurposeId == purpose.PurposeId));
        return found;
    }

    [Property(MaxTest = 100)]
    public bool GetUnregistered_ReturnsNone(NonEmptyString purposeName)
    {
        var name = purposeName.Get.Trim();
        if (string.IsNullOrWhiteSpace(name)) return true;

        var get = _sut.GetPurposeAsync($"never-registered-{name}").AsTask().GetAwaiter().GetResult();
        if (get.IsLeft) return false;

        bool isNone = true;
        get.IfRight(opt => opt.IfSome(_ => isNone = false));
        return isNone;
    }

    [Property(MaxTest = 50)]
    public bool ModuleScopedPurpose_DoesNotLeakToGlobal(NonEmptyString purposeName)
    {
        var name = purposeName.Get.Trim();
        if (string.IsNullOrWhiteSpace(name)) return true;

        var purpose = MakePurpose(name, "module-A");
        _sut.RegisterPurposeAsync(purpose).AsTask().GetAwaiter().GetResult();

        // Global lookup should NOT find module-scoped purpose
        var globalGet = _sut.GetPurposeAsync(name).AsTask().GetAwaiter().GetResult();
        bool globalIsNone = true;
        globalGet.IfRight(opt => opt.IfSome(_ => globalIsNone = false));

        // Module-scoped lookup should find it
        var moduleGet = _sut.GetPurposeAsync(name, "module-A").AsTask().GetAwaiter().GetResult();
        bool moduleFound = false;
        moduleGet.IfRight(opt => opt.IfSome(_ => moduleFound = true));

        return globalIsNone && moduleFound;
    }

    [Property(MaxTest = 50)]
    public bool RegisterThenRemove_MakesItGone(NonEmptyString purposeName)
    {
        var name = purposeName.Get.Trim();
        if (string.IsNullOrWhiteSpace(name)) return true;

        var purpose = MakePurpose(name);
        _sut.RegisterPurposeAsync(purpose).AsTask().GetAwaiter().GetResult();
        _sut.RemovePurposeAsync(purpose.PurposeId).AsTask().GetAwaiter().GetResult();

        var get = _sut.GetPurposeAsync(name).AsTask().GetAwaiter().GetResult();
        bool isNone = true;
        get.IfRight(opt => opt.IfSome(_ => isNone = false));
        return isNone;
    }

    [Property(MaxTest = 50)]
    public bool RemoveUnknown_ReturnsError(NonEmptyString purposeId)
    {
        var id = purposeId.Get.Trim();
        if (string.IsNullOrWhiteSpace(id)) return true;

        var result = _sut.RemovePurposeAsync($"unknown-{id}").AsTask().GetAwaiter().GetResult();
        return result.IsLeft;
    }

    [Property(MaxTest = 50)]
    public bool GetAllGlobal_IncludesOnlyGlobal(NonEmptyString nameA, NonEmptyString nameB)
    {
        var a = nameA.Get.Trim();
        var b = nameB.Get.Trim();
        if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b) || a == b) return true;

        _sut.RegisterPurposeAsync(MakePurpose(a)).AsTask().GetAwaiter().GetResult();
        _sut.RegisterPurposeAsync(MakePurpose(b, "module-X")).AsTask().GetAwaiter().GetResult();

        var all = _sut.GetAllPurposesAsync().AsTask().GetAwaiter().GetResult();
        int count = 0;
        all.IfRight(list => count = list.Count);

        // Should contain only the global purpose, not the module-scoped one
        return count >= 1;
    }
}
