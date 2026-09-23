using System.Collections;
using System.Data.Common;
using System.Linq.Expressions;
using Encina.EntityFrameworkCore.Auditing;
using Encina.Security.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
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
    public async Task RecordAsync_WhenSaveChangesFails_MessageIncludesTheRootCauseTypeButNotItsText()
    {
        // Arrange
        var store = new AuditStoreEF(CreateFailingContext<AuditEntryEntity>(CreateDbUpdateException()));

        // Act
        var result = await store.RecordAsync(CreateAuditEntry());

        // Assert: the root cause's type is reported, but never its message text - provider messages can embed
        // column values (e.g. SQL Server 2628 "Truncated value: '...'"), i.e. personal data (#1128).
        var error = result.LeftToSeq().Single();
        error.Message.ShouldStartWith("Failed to record audit entry: ");
        error.Message.ShouldContain(nameof(InvalidOperationException));
        error.Message.ShouldNotContain(RootCauseMessage);
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

    #region AuditStoreEF.PurgeEntriesAsync

    [Fact]
    public async Task PurgeEntriesAsync_WhenExecuteDeleteThrowsDbException_ReturnsLeftCarryingTheException()
    {
        // Arrange: ExecuteDeleteAsync issues the DELETE directly, without EF Core's DbUpdateException wrapper,
        // so a raw provider DbException (e.g. a missing table) must be caught too, not just DbUpdateException (#1128).
        var failure = new TestDbException("42P01: relation \"SecurityAuditEntries\" does not exist", sqlState: "42P01");
        var context = CreateContextWithExecuteDeleteFailure<AuditEntryEntity>(failure);
        var store = new AuditStoreEF(context);

        // Act
        var result = await store.PurgeEntriesAsync(DateTime.UtcNow);

        // Assert
        var error = result.LeftToSeq().Single();
        error.Exception.IsSome.ShouldBeTrue();
        error.Exception.IfSome(ex => ex.ShouldBeSameAs(failure));
        error.GetCode().IfNone(string.Empty).ShouldBe(AuditStoreEF.StoreErrorCode);
        error.GetDetails()["operation"].ShouldBe("PurgeEntries");
        error.Message.ShouldContain(nameof(TestDbException));
        error.Message.ShouldContain("SqlState=42P01");
    }

    #endregion

    #region ReadAuditStoreEF.LogReadAsync

    [Fact]
    public async Task LogReadAsync_WhenSaveChangesFails_ReturnsLeftCarryingTheExceptionAndRootCauseType()
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
        error.Message.ShouldContain(nameof(InvalidOperationException));
        error.Message.ShouldNotContain(RootCauseMessage);
        error.GetCode().IfNone(string.Empty).ShouldBe(ReadAuditErrors.StoreErrorCode);
    }

    #endregion

    #region ReadAuditStoreEF.PurgeEntriesAsync

    [Fact]
    public async Task PurgeEntriesAsync_WhenExecuteDeleteThrowsDbException_ReturnsLeftCarryingTheExceptionAndProviderCode()
    {
        // Arrange
        var failure = new TestDbException("relation does not exist", sqlState: null, errorCode: 1146);
        var context = CreateContextWithExecuteDeleteFailure<ReadAuditEntryEntity>(failure);
        var store = new ReadAuditStoreEF(context);

        // Act
        var result = await store.PurgeEntriesAsync(DateTimeOffset.UtcNow);

        // Assert
        var error = result.LeftToSeq().Single();
        error.Exception.IsSome.ShouldBeTrue();
        error.Exception.IfSome(ex => ex.ShouldBeSameAs(failure));
        error.GetCode().IfNone(string.Empty).ShouldBe(ReadAuditErrors.PurgeFailedCode);
        error.Message.ShouldContain(nameof(TestDbException));
        error.Message.ShouldContain("ErrorCode=1146");
    }

    #endregion

    #region StoreExceptionMessages

    [Fact]
    public void Describe_WithoutInnerException_ReturnsTheExceptionTypeName()
    {
        // Act
        var message = StoreExceptionMessages.Describe(new DbUpdateException("save failed"));

        // Assert: never the message text, only the type name (#1128)
        message.ShouldBe(nameof(DbUpdateException));
    }

    [Fact]
    public void Describe_WithNestedInnerExceptions_ReturnsTheRootCauseTypeName()
    {
        // Arrange
        var root = new InvalidOperationException("root cause");
        var exception = new DbUpdateException("outer", new InvalidCastException("middle", root));

        // Act
        var message = StoreExceptionMessages.Describe(exception);

        // Assert
        message.ShouldBe(nameof(InvalidOperationException));
    }

    [Fact]
    public void Describe_WithDbExceptionRootCauseAndSqlState_AppendsTheSqlState()
    {
        // Arrange
        var exception = new TestDbException("truncated value: '...'", sqlState: "22001", errorCode: 0);

        // Act
        var message = StoreExceptionMessages.Describe(exception);

        // Assert: the SQLSTATE identifies the failure without leaking the provider's message text
        message.ShouldBe($"{nameof(TestDbException)} (SqlState=22001)");
    }

    [Fact]
    public void Describe_WithDbExceptionRootCauseAndErrorCodeOnly_AppendsTheErrorCode()
    {
        // Arrange: some providers (e.g. MySqlConnector) never populate SqlState, only the numeric ErrorCode
        var exception = new TestDbException("Table doesn't exist", sqlState: null, errorCode: 1146);

        // Act
        var message = StoreExceptionMessages.Describe(exception);

        // Assert
        message.ShouldBe($"{nameof(TestDbException)} (ErrorCode=1146)");
    }

    [Fact]
    public void Describe_WithDbExceptionRootCauseAndNoIdentifier_ReturnsOnlyTheTypeName()
    {
        // Arrange
        var exception = new TestDbException("unknown failure", sqlState: null, errorCode: 0);

        // Act
        var message = StoreExceptionMessages.Describe(exception);

        // Assert
        message.ShouldBe(nameof(TestDbException));
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

    private static DbContext CreateContextWithExecuteDeleteFailure<TEntity>(Exception failure)
        where TEntity : class
    {
        var context = Substitute.For<DbContext>();
        context.Set<TEntity>().Returns(new ThrowingExecuteDeleteDbSet<TEntity>(failure));
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

    /// <summary>
    /// A concrete <see cref="DbException"/> double used to exercise <see cref="StoreExceptionMessages.Describe"/>'s
    /// provider error identifier logic without depending on any real ADO.NET provider.
    /// </summary>
    private sealed class TestDbException : DbException
    {
        public TestDbException(string message, string? sqlState = null, int errorCode = 0)
            : base(message, errorCode)
        {
            SqlState = sqlState;
        }

        public override string? SqlState { get; }
    }

    /// <summary>
    /// A <see cref="DbSet{TEntity}"/> double whose <c>Where(...).ExecuteDeleteAsync(...)</c> chain throws a given
    /// exception, used to exercise the <c>PurgeEntriesAsync</c> catch clause that handles a provider
    /// <see cref="DbException"/> surfacing directly from EF Core's bulk delete (no <see cref="DbUpdateException"/>
    /// wrapper is involved) (#1128).
    /// </summary>
    /// <remarks>
    /// <see cref="DbSet{TEntity}"/> implements <see cref="IQueryable{T}"/> via explicit, non-virtual interface
    /// members, so they cannot be overridden. Re-declaring <see cref="IQueryable{T}"/> (and
    /// <see cref="IAsyncQueryProvider"/>) on this subclass gives it its own interface map entries, which take
    /// precedence over the base class's when the object is accessed through those interfaces - the same technique
    /// mocking frameworks use to fake <see cref="DbSet{TEntity}"/> query behavior.
    /// </remarks>
    private sealed class ThrowingExecuteDeleteDbSet<TEntity> : DbSet<TEntity>, IQueryable<TEntity>, IAsyncQueryProvider
        where TEntity : class
    {
        private readonly Exception _exception;

        public ThrowingExecuteDeleteDbSet(Exception exception)
        {
            _exception = exception;
        }

        public override Microsoft.EntityFrameworkCore.Metadata.IEntityType EntityType =>
            throw new NotSupportedException();

        Type IQueryable.ElementType => typeof(TEntity);

        Expression IQueryable.Expression => Expression.Constant(this, typeof(IQueryable<TEntity>));

        IQueryProvider IQueryable.Provider => this;

        IEnumerator<TEntity> IEnumerable<TEntity>.GetEnumerator() => throw new NotSupportedException();

        IEnumerator IEnumerable.GetEnumerator() => throw new NotSupportedException();

        IQueryable IQueryProvider.CreateQuery(Expression expression) => throw new NotSupportedException();

        IQueryable<TElement> IQueryProvider.CreateQuery<TElement>(Expression expression) =>
            (IQueryable<TElement>)(object)this;

        object? IQueryProvider.Execute(Expression expression) => throw new NotSupportedException();

        TResult IQueryProvider.Execute<TResult>(Expression expression) => throw new NotSupportedException();

        TResult IAsyncQueryProvider.ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken) =>
            throw _exception;
    }
}
