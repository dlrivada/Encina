using Encina.Security.BreakTheGlass;

namespace Encina.ContractTests.Security.BreakTheGlass;

/// <summary>
/// Runs <see cref="BreakTheGlassStoreContract"/> against <see cref="InMemoryBreakTheGlassStore"/>, so the
/// in-memory default keeps the behavior the persistent stores are held to.
/// </summary>
public sealed class InMemoryBreakTheGlassStoreContractTests : BreakTheGlassStoreContract
{
    private InMemoryBreakTheGlassStore _store = null!;

    protected override Task<IBreakTheGlassStore> CreateStoreAsync(TimeProvider timeProvider)
    {
        _store = new InMemoryBreakTheGlassStore();
        return Task.FromResult<IBreakTheGlassStore>(_store);
    }

    // The in-memory store is one process-local table: the second "store" is the same instance.
    protected override IBreakTheGlassStore CreateSecondStore() => _store;
}
