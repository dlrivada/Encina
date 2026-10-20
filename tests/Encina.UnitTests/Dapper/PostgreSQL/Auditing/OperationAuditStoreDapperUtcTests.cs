using System.Data.Common;
using Encina.Dapper.PostgreSQL.Auditing;
using Encina.Security.Audit;
using Encina.UnitTests.Security.Audit.Relational;

namespace Encina.UnitTests.Dapper.PostgreSQL.Auditing;

/// <summary>
/// Proves <see cref="OperationAuditStoreDapper"/> binds only UTC instants to the driver, whatever the machine's time zone.
/// </summary>
public sealed class OperationAuditStoreDapperUtcTests : OperationAuditStoreUtcTestsBase
{
    /// <inheritdoc/>
    protected override IOperationAuditStore CreateStore(DbConnection connection) => new OperationAuditStoreDapper(connection);
}
