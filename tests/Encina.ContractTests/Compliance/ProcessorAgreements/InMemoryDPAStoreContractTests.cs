#pragma warning disable CA1859 // Contract tests intentionally use interface types

using Encina.Compliance.ProcessorAgreements;

using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.ContractTests.Compliance.ProcessorAgreements;

/// <summary>
/// Contract verification for the in-memory <see cref="IDPAStore"/> implementation.
/// </summary>
[Trait("Category", "Contract")]
public sealed class InMemoryDPAStoreContractTests : DPAStoreContractTestsBase
{
    protected override IDPAStore CreateStore() =>
        new InMemoryDPAStore(NullLogger<InMemoryDPAStore>.Instance);
}
