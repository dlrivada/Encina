using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.InMemory;

using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.ContractTests.Compliance.DataResidency;

public sealed class InMemoryResidencyAuditStoreContractTests : ResidencyAuditStoreContractTestsBase
{
    protected override IResidencyAuditStore CreateStore() =>
        new InMemoryResidencyAuditStore(NullLogger<InMemoryResidencyAuditStore>.Instance);
}
