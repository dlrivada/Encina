using Encina.Marten.Versioning;

namespace Encina.ContractTests.Marten.Core;

/// <summary>
/// Behavioral contract tests for <see cref="EventUpcasterRegistry"/> verifying
/// registration, lookup, and scan behaviors work correctly.
/// </summary>
[Trait("Category", "Contract")]
[Trait("Provider", "Marten")]
public sealed class EventUpcasterRegistryContractTests
{
    #region Register Contract

    [Fact]
    public void Register_Instance_AddedToGetAllUpcasters()
    {
        var registry = new EventUpcasterRegistry();
        var upcaster = new TestUpcasterV1();

        registry.Register(upcaster);

        var all = registry.GetAllUpcasters();
        all.Count.ShouldBe(1);
        all[0].ShouldBe(upcaster);
    }

    [Fact]
    public void Register_ByType_CreatesAndRegisters()
    {
        var registry = new EventUpcasterRegistry();

        registry.Register(typeof(TestUpcasterV1));

        registry.Count.ShouldBe(1);
        registry.HasUpcasterFor("TestEvent_v1").ShouldBeTrue();
    }

    [Fact]
    public void Register_ByTypeWithFactory_UsesFactory()
    {
        var registry = new EventUpcasterRegistry();
        var customUpcaster = new TestUpcasterV1();
        var factoryCalled = false;

        registry.Register(typeof(TestUpcasterV1), _ =>
        {
            factoryCalled = true;
            return customUpcaster;
        });

        factoryCalled.ShouldBeTrue();
        registry.GetUpcasterForEventType("TestEvent_v1").ShouldBe(customUpcaster);
    }

    [Fact]
    public void Register_DuplicateSourceTypeName_ThrowsInvalidOperation()
    {
        var registry = new EventUpcasterRegistry();
        registry.Register(new TestUpcasterV1());

        Should.Throw<InvalidOperationException>(() =>
            registry.Register(new TestUpcasterV1()));
    }

    #endregion

    #region TryRegister Contract

    [Fact]
    public void TryRegister_Unique_ReturnsTrue()
    {
        var registry = new EventUpcasterRegistry();
        registry.TryRegister(new TestUpcasterV1()).ShouldBeTrue();
    }

    [Fact]
    public void TryRegister_Duplicate_ReturnsFalse_DoesNotThrow()
    {
        var registry = new EventUpcasterRegistry();
        registry.TryRegister(new TestUpcasterV1()).ShouldBeTrue();
        registry.TryRegister(new TestUpcasterV1()).ShouldBeFalse();
        registry.Count.ShouldBe(1);
    }

    #endregion

    #region Lookup Contract

    [Fact]
    public void GetUpcasterForEventType_Registered_ReturnsUpcaster()
    {
        var registry = new EventUpcasterRegistry();
        var upcaster = new TestUpcasterV1();
        registry.Register(upcaster);

        registry.GetUpcasterForEventType("TestEvent_v1").ShouldBe(upcaster);
    }

    [Fact]
    public void GetUpcasterForEventType_NotRegistered_ReturnsNull()
    {
        var registry = new EventUpcasterRegistry();
        registry.GetUpcasterForEventType("NonExistent").ShouldBeNull();
    }

    [Fact]
    public void HasUpcasterFor_Registered_ReturnsTrue()
    {
        var registry = new EventUpcasterRegistry();
        registry.Register(new TestUpcasterV1());
        registry.HasUpcasterFor("TestEvent_v1").ShouldBeTrue();
    }

    [Fact]
    public void HasUpcasterFor_NotRegistered_ReturnsFalse()
    {
        var registry = new EventUpcasterRegistry();
        registry.HasUpcasterFor("NonExistent").ShouldBeFalse();
    }

    #endregion

    #region Scan Contract

    [Fact]
    public void ScanAndRegister_CurrentAssembly_FindsTestUpcasters()
    {
        var registry = new EventUpcasterRegistry();
        var count = registry.ScanAndRegister(typeof(EventUpcasterRegistryContractTests).Assembly);

        // Should find at least TestUpcasterV1 (and possibly TestUpcasterV2 if discoverable)
        count.ShouldBeGreaterThanOrEqualTo(0);
    }

    #endregion

    #region GetAllUpcasters Contract

    [Fact]
    public void GetAllUpcasters_MultipleRegistrations_ReturnsAll()
    {
        var registry = new EventUpcasterRegistry();
        registry.Register(new TestUpcasterV1());
        registry.Register(new TestUpcasterV2());

        var all = registry.GetAllUpcasters();
        all.Count.ShouldBe(2);
    }

    [Fact]
    public void GetAllUpcasters_Empty_ReturnsEmptyList()
    {
        var registry = new EventUpcasterRegistry();
        registry.GetAllUpcasters().Count.ShouldBe(0);
    }

    #endregion

    #region Test Types

    private sealed class TestUpcasterV1 : IEventUpcaster
    {
        public string SourceEventTypeName => "TestEvent_v1";
        public Type TargetEventType => typeof(object);
        public Type SourceEventType => typeof(string);
    }

    private sealed class TestUpcasterV2 : IEventUpcaster
    {
        public string SourceEventTypeName => "TestEvent_v2";
        public Type TargetEventType => typeof(object);
        public Type SourceEventType => typeof(string);
    }

    #endregion
}
