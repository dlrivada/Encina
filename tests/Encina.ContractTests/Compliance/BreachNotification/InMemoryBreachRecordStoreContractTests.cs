using Encina.Compliance.BreachNotification;
using Encina.Compliance.BreachNotification.InMemory;

using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.ContractTests.Compliance.BreachNotification;

/// <summary>
/// Contract test implementation for <see cref="InMemoryBreachRecordStore"/>.
/// </summary>
public sealed class InMemoryBreachRecordStoreContractTests : BreachRecordStoreContractTestsBase
{
    protected override IBreachRecordStore CreateStore()
        => new InMemoryBreachRecordStore(TimeProvider.System, NullLogger<InMemoryBreachRecordStore>.Instance);
}
