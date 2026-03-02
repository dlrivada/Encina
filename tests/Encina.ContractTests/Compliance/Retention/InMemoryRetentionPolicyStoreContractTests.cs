using Encina.Compliance.Retention;
using Encina.Compliance.Retention.InMemory;

using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.ContractTests.Compliance.Retention;

/// <summary>
/// Contract test implementation for <see cref="InMemoryRetentionPolicyStore"/>.
/// </summary>
public sealed class InMemoryRetentionPolicyStoreContractTests : RetentionPolicyStoreContractTestsBase
{
    protected override IRetentionPolicyStore CreateStore() =>
        new InMemoryRetentionPolicyStore(NullLogger<InMemoryRetentionPolicyStore>.Instance);
}
