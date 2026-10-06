using System.Data.Common;
using Encina.ADO.SqlServer.Auditing;
using Encina.Security.Audit;
using Encina.UnitTests.Security.Audit.Relational;

namespace Encina.UnitTests.ADO.SqlServer.Auditing;

/// <summary>
/// Proves <see cref="OperationAuditStoreADO"/> binds only UTC instants to the driver, whatever the machine's time zone.
/// </summary>
public sealed class OperationAuditStoreADOUtcTests : OperationAuditStoreUtcTestsBase
{
    /// <inheritdoc/>
    protected override IOperationAuditStore CreateStore(DbConnection connection) => new OperationAuditStoreADO(connection);
}
