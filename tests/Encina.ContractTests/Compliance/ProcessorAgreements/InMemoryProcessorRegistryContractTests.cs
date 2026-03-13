#pragma warning disable CA1859 // Contract tests intentionally use interface types

using Encina.Compliance.ProcessorAgreements;

using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.ContractTests.Compliance.ProcessorAgreements;

/// <summary>
/// Contract verification for the in-memory <see cref="IProcessorRegistry"/> implementation.
/// </summary>
[Trait("Category", "Contract")]
public sealed class InMemoryProcessorRegistryContractTests : ProcessorRegistryContractTestsBase
{
    protected override IProcessorRegistry CreateStore() =>
        new InMemoryProcessorRegistry(NullLogger<InMemoryProcessorRegistry>.Instance);
}
