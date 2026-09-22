using Encina.EntityFrameworkCore.Auditing;
using Encina.Security.Audit;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;

namespace Encina.UnitTests.EntityFrameworkCore.Auditing;

/// <summary>
/// Unit tests for how <see cref="AuditStoreEF"/> and <see cref="ReadAuditStoreEF"/> report database failures and for
/// the filtered indexes of their entity configurations (#1128).
/// </summary>
[Trait("Category", "Unit")]
public sealed class SecurityAuditStoreEFFailureTests
{
    private const string RootCauseMessage = "42P01: relation \"SecurityAuditEntries\" does not exist";

    #region AuditStoreEF.RecordAsync

    [Fact]
    public async Task RecordAsync_WhenSaveChangesFails_ReturnsLeftCarryingTheException()
    {
        // Arrange
        var failure = CreateDbUpdateException();
        var store = new AuditStoreEF(CreateFailingContext<AuditEntryEntity>(failure));

        // Act
        var result = await store.RecordAsync(CreateAuditEntry());

        // Assert
        var error = result.LeftToSeq().Single();
        error.Exception.IsSome.ShouldBeTrue();
        error.Exception.IfSome(ex => ex.ShouldBeSameAs(failure));
    }

    [Fact]
    public async Task RecordAsync_WhenSaveChangesFails_MessageIncludesTheRootCause()
    {
        // Arrange
        var store = new AuditStoreEF(CreateFailingContext<AuditEntryEntity>(CreateDbUpdateException()));

        // Act
        var result = await store.RecordAsync(CreateAuditEntry());

        // Assert
        var error = result.LeftToSeq().Single();
        error.Message.ShouldStartWith("Failed to record audit entry: ");
        error.Message.ShouldContain(RootCauseMessage);
        error.Message.ShouldContain(nameof(InvalidOperationException));
    }

    [Fact]
    public async Task RecordAsync_WhenSaveChangesFails_ReturnsStoreErrorCodeAndOperation()
    {
        // Arrange
        var store = new AuditStoreEF(CreateFailingContext<AuditEntryEntity>(CreateDbUpdateException()));

        // Act
        var result = await store.RecordAsync(CreateAuditEntry());

        // Assert
        var error = result.LeftToSeq().Single();
        error.GetCode().IfNone(string.Empty).ShouldBe(AuditStoreEF.StoreErrorCode);
        error.GetDetails()["operation"].ShouldBe("Record");
    }

    #endregion

    #region ReadAuditStoreEF.LogReadAsync

    [Fact]
    public async Task LogReadAsync_WhenSaveChangesFails_ReturnsLeftCarryingTheExceptionAndRootCause()
    {
        // Arrange
        var failure = CreateDbUpdateException();
        var store = new ReadAuditStoreEF(CreateFailingContext<ReadAuditEntryEntity>(failure));

        // Act
        var result = await store.LogReadAsync(CreateReadAuditEntry());

        // Assert
        var error = result.LeftToSeq().Single();
        error.Exception.IfSome(ex => ex.ShouldBeSameAs(failure));
        error.Exception.IsSome.ShouldBeTrue();
        error.Message.ShouldContain(RootCauseMessage);
        error.GetCode().IfNone(string.Empty).ShouldBe(ReadAuditErrors.StoreErrorCode);
    }

    #endregion

    #region StoreExceptionMessages

    [Fact]
    public void Describe_WithoutInnerException_ReturnsTheExceptionMessage()
    {
        // Act
        var message = StoreExceptionMessages.Describe(new DbUpdateException("save failed"));

        // Assert
        message.ShouldBe("save failed");
    }

    [Fact]
    public void Describe_WithNestedInnerExceptions_AppendsTheRootCause()
    {
        // Arrange
        var root = new InvalidOperationException("root cause");
        var exception = new DbUpdateException("outer", new InvalidCastException("middle", root));

        // Act
        var message = StoreExceptionMessages.Describe(exception);

        // Assert
        message.ShouldBe("outer (InvalidOperationException: root cause)");
    }

    [Fact]
    public void Describe_WithNullException_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => StoreExceptionMessages.Describe(null!));
    }

    #endregion

    #region Filtered indexes

    [Fact]
    public void IsNotNull_QuotesTheColumnAsDelimitedIdentifier()
    {
        IndexFilters.IsNotNull("UserId").ShouldBe("\"UserId\" IS NOT NULL");
    }

    [Theory]
    [InlineData("SecurityAuditEntries", "IX_SecurityAuditEntries_UserId", "UserId")]
    [InlineData("SecurityAuditEntries", "IX_SecurityAuditEntries_TenantId", "TenantId")]
    [InlineData("ReadAuditEntries", "IX_ReadAuditEntries_UserId", "UserId")]
    [InlineData("ReadAuditEntries", "IX_ReadAuditEntries_TenantId", "TenantId")]
    [InlineData("ReadAuditEntries", "IX_ReadAuditEntries_CorrelationId", "CorrelationId")]
    public void PostgreSqlCreateScript_FilteredIndexes_QuoteTheColumn(string table, string index, string column)
    {
        // Arrange
        var options = new DbContextOptionsBuilder<SecurityAuditSchemaDbContext>()
            .UseNpgsql("Host=localhost;Database=unused")
            .Options;
        using var context = new SecurityAuditSchemaDbContext(options);

        // Act
        var script = context.Database.GenerateCreateScript();

        // Assert: an unquoted column is folded to lower case by PostgreSQL and the CREATE INDEX fails
        script.ShouldContain($"CREATE INDEX \"{index}\" ON \"{table}\" (\"{column}\") WHERE \"{column}\" IS NOT NULL;");
    }

    [Fact]
    public void SqlServerCreateScript_FilteredIndexes_UseTheQuotedFilter()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<SecurityAuditSchemaDbContext>()
            .UseSqlServer("Server=localhost;Database=unused")
            .Options;
        using var context = new SecurityAuditSchemaDbContext(options);

        // Act
        var script = context.Database.GenerateCreateScript();

        // Assert
        script.ShouldContain("CREATE INDEX [IX_SecurityAuditEntries_UserId] ON [SecurityAuditEntries] ([UserId]) WHERE \"UserId\" IS NOT NULL;");
        script.ShouldContain("CREATE INDEX [IX_ReadAuditEntries_CorrelationId] ON [ReadAuditEntries] ([CorrelationId]) WHERE \"CorrelationId\" IS NOT NULL;");
    }

    #endregion

    private static DbUpdateException CreateDbUpdateException() =>
        new(
            "An error occurred while saving the entity changes. See the inner exception for details.",
            new InvalidOperationException(RootCauseMessage));

    private static DbContext CreateFailingContext<TEntity>(DbUpdateException failure)
        where TEntity : class
    {
        var context = Substitute.For<DbContext>();
        var set = Substitute.For<DbSet<TEntity>>();
        context.Set<TEntity>().Returns(set);
        context.SaveChangesAsync(Arg.Any<CancellationToken>()).ThrowsAsync(failure);
        return context;
    }

    private static AuditEntry CreateAuditEntry()
    {
        var now = DateTimeOffset.UnixEpoch.AddYears(56);
        return new AuditEntry
        {
            Id = Guid.NewGuid(),
            CorrelationId = "corr-1128",
            UserId = "user-1128",
            Action = "Create",
            EntityType = "Order",
            EntityId = "order-1",
            Outcome = AuditOutcome.Success,
            TimestampUtc = now.UtcDateTime,
            StartedAtUtc = now,
            CompletedAtUtc = now.AddMilliseconds(10),
            Metadata = new Dictionary<string, object?>()
        };
    }

    private static ReadAuditEntry CreateReadAuditEntry() => new()
    {
        Id = Guid.NewGuid(),
        EntityType = "Patient",
        EntityId = "patient-1",
        UserId = "user-1128",
        AccessedAtUtc = DateTimeOffset.UnixEpoch.AddYears(56),
        AccessMethod = ReadAccessMethod.Repository,
        EntityCount = 1
    };

    private sealed class SecurityAuditSchemaDbContext(DbContextOptions<SecurityAuditSchemaDbContext> options)
        : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new AuditEntryEntityConfiguration());
            modelBuilder.ApplyConfiguration(new ReadAuditEntryEntityConfiguration());
        }
    }
}
