using Encina.Compliance.DataResidency;
using Encina.Compliance.DataResidency.InMemory;

using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.ContractTests.Compliance.DataResidency;

public sealed class InMemoryDataLocationStoreContractTests : DataLocationStoreContractTestsBase
{
    protected override IDataLocationStore CreateStore() =>
        new InMemoryDataLocationStore(NullLogger<InMemoryDataLocationStore>.Instance);
}
