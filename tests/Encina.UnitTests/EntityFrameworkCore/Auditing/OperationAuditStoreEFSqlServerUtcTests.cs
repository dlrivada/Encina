using Microsoft.EntityFrameworkCore;

namespace Encina.UnitTests.EntityFrameworkCore.Auditing;

/// <summary>Proves the EF Core operation audit store hands only UTC instants to the SQL Server provider.</summary>
public sealed class OperationAuditStoreEFSqlServerUtcTests : OperationAuditStoreEFUtcTestsBase
{
    /// <inheritdoc/>
    protected override void UseProvider(DbContextOptionsBuilder<OperationAuditTestContext> options) =>
        options.UseSqlServer("Server=127.0.0.1;Database=audit;User Id=sa;Password=unused;TrustServerCertificate=true");
}
