using Encina.Compliance.Anonymization;
using Encina.Compliance.Anonymization.InMemory;

namespace Encina.ContractTests.Compliance.Anonymization;

/// <summary>
/// Contract test implementation for <see cref="InMemoryTokenMappingStore"/>.
/// </summary>
public sealed class InMemoryTokenMappingStoreContractTests : TokenMappingStoreContractTestsBase
{
    protected override ITokenMappingStore CreateStore() => new InMemoryTokenMappingStore();
}
