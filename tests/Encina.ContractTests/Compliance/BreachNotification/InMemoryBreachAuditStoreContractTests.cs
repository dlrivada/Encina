using Encina.Compliance.BreachNotification;
using Encina.Compliance.BreachNotification.InMemory;

using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.ContractTests.Compliance.BreachNotification;

/// <summary>
/// Contract test implementation for <see cref="InMemoryBreachAuditStore"/>.
/// </summary>
public sealed class InMemoryBreachAuditStoreContractTests : BreachAuditStoreContractTestsBase
{
    protected override IBreachAuditStore CreateStore()
        => new InMemoryBreachAuditStore(NullLogger<InMemoryBreachAuditStore>.Instance);
}
