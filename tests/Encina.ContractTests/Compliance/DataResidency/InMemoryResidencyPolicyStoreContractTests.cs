using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.InMemory;

using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.ContractTests.Compliance.DataResidency;

public sealed class InMemoryResidencyPolicyStoreContractTests : ResidencyPolicyStoreContractTestsBase
{
    protected override IResidencyPolicyStore CreateStore() =>
        new InMemoryResidencyPolicyStore(NullLogger<InMemoryResidencyPolicyStore>.Instance);
}
