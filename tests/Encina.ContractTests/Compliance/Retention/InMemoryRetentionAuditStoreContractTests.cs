using Encina.Compliance.Retention;
using Encina.Compliance.Retention.InMemory;

using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.ContractTests.Compliance.Retention;

/// <summary>
/// Contract test implementation for <see cref="InMemoryRetentionAuditStore"/>.
/// </summary>
public sealed class InMemoryRetentionAuditStoreContractTests : RetentionAuditStoreContractTestsBase
{
    protected override IRetentionAuditStore CreateStore() =>
        new InMemoryRetentionAuditStore(NullLogger<InMemoryRetentionAuditStore>.Instance);
}
