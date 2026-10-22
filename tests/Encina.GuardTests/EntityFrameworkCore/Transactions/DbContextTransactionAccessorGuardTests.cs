using Encina.EntityFrameworkCore.Transactions;
using Encina.EntityFrameworkCore.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using Xunit;
using Encina.Messaging.Transactions;

namespace Encina.GuardTests.EntityFrameworkCore.Transactions;

/// <summary>
/// Guard tests for the constructors of the EF Core shared transaction types.
/// </summary>
[Trait("Category", "Guard")]
public sealed class DbContextTransactionAccessorGuardTests
{
    private sealed class GuardDbContext(DbContextOptions<GuardDbContext> options) : DbContext(options);

    private static GuardDbContext NewContext()
        => new(new DbContextOptionsBuilder<GuardDbContext>().UseSqlite("DataSource=:memory:").Options);

    [Fact]
    public void Accessor_NullDbContext_Throws()
    {
        var ex = Should.Throw<ArgumentNullException>(() =>
            new DbContextTransactionAccessor(null!, NullLogger<DbContextTransactionAccessor>.Instance));
        ex.ParamName.ShouldBe("dbContext");
    }

    [Fact]
    public void Accessor_NullLogger_Throws()
    {
        using var db = NewContext();
        var ex = Should.Throw<ArgumentNullException>(() => new DbContextTransactionAccessor(db, null!));
        ex.ParamName.ShouldBe("logger");
    }

    [Fact]
    public void UnitOfWork_NullTransactionAccessor_Throws()
    {
        using var db = NewContext();
        var ex = Should.Throw<ArgumentNullException>(() => new UnitOfWorkEF(db, null!, Substitute.For<IServiceProvider>()));
        ex.ParamName.ShouldBe("transactionAccessor");
    }

    [Fact]
    public void UnitOfWork_NullServiceProvider_Throws()
    {
        using var db = NewContext();
        var ex = Should.Throw<ArgumentNullException>(() => new UnitOfWorkEF(db, Substitute.For<IDbTransactionAccessor>(), null!));
        ex.ParamName.ShouldBe("serviceProvider");
    }
}
