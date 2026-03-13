#pragma warning disable CA1859 // Contract tests intentionally use interface types

using Encina.Compliance.ProcessorAgreements;

using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.ContractTests.Compliance.ProcessorAgreements;

/// <summary>
/// Contract verification for the in-memory <see cref="IProcessorAuditStore"/> implementation.
/// </summary>
[Trait("Category", "Contract")]
public sealed class InMemoryProcessorAuditStoreContractTests : ProcessorAuditStoreContractTestsBase
{
    protected override IProcessorAuditStore CreateStore() =>
        new InMemoryProcessorAuditStore(NullLogger<InMemoryProcessorAuditStore>.Instance);
}
