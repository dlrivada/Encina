using Encina.Compliance.Retention;
using Encina.Compliance.Retention.InMemory;

using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.ContractTests.Compliance.Retention;

/// <summary>
/// Contract test implementation for <see cref="InMemoryLegalHoldStore"/>.
/// </summary>
public sealed class InMemoryLegalHoldStoreContractTests : LegalHoldStoreContractTestsBase
{
    protected override ILegalHoldStore CreateStore() =>
        new InMemoryLegalHoldStore(NullLogger<InMemoryLegalHoldStore>.Instance);
}
