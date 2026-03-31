using Encina.EntityFrameworkCore.BulkOperations;
using Microsoft.EntityFrameworkCore;

namespace Encina.GuardTests.EntityFrameworkCore.BulkOperations;

/// <summary>
/// Guard clause tests for <see cref="BulkOperationsEF{TEntity}"/>.
/// </summary>
[Trait("Category", "Guard")]
[Trait("Provider", "EntityFrameworkCore")]
public sealed class BulkOperationsEFGuardTests
{
    #region Constructor Guards

    [Fact]
    public void Constructor_NullDbContext_ThrowsArgumentNullException()
    {
        // Arrange
        DbContext dbContext = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new BulkOperationsEF<BulkTestEntity>(dbContext));
        ex.ParamName.ShouldBe("dbContext");
    }

    #endregion

    #region Test Infrastructure

    private sealed class BulkTestEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    #endregion
}
