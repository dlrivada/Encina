using Microsoft.EntityFrameworkCore;

namespace Encina.UnitTests.EntityFrameworkCore.Auditing;

/// <summary>Proves the EF Core operation audit store hands only UTC instants to the Npgsql provider.</summary>
public sealed class OperationAuditStoreEFPostgreSqlUtcTests : OperationAuditStoreEFUtcTestsBase
{
    /// <inheritdoc/>
    protected override void UseProvider(DbContextOptionsBuilder<OperationAuditTestContext> options) =>
        options.UseNpgsql("Host=127.0.0.1;Database=audit;Username=unused;Password=unused");
}
