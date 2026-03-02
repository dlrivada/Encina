using Encina.Compliance.Retention;
using Encina.Compliance.Retention.InMemory;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace Encina.ContractTests.Compliance.Retention;

/// <summary>
/// Contract test implementation for <see cref="InMemoryRetentionRecordStore"/>.
/// </summary>
public sealed class InMemoryRetentionRecordStoreContractTests : RetentionRecordStoreContractTestsBase
{
    protected override IRetentionRecordStore CreateStore() =>
        new InMemoryRetentionRecordStore(
            new FakeTimeProvider(DateTimeOffset.UtcNow),
            NullLogger<InMemoryRetentionRecordStore>.Instance);
}
