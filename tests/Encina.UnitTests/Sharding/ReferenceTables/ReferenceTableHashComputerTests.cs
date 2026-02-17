using System.ComponentModel.DataAnnotations;
using Encina.Sharding.ReferenceTables;

namespace Encina.UnitTests.Sharding.ReferenceTables;

/// <summary>
/// Unit tests for <see cref="ReferenceTableHashComputer"/>.
/// </summary>
public sealed class ReferenceTableHashComputerTests
{
    // ────────────────────────────────────────────────────────────
    //  Test entity stubs
    // ────────────────────────────────────────────────────────────

    private sealed class Product
    {
        [Key]
        public int ProductId { get; set; }
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
    }

    private sealed class SimpleEntity
    {
        public int Id { get; set; }
        public string Value { get; set; } = "";
    }

    // ────────────────────────────────────────────────────────────
    //  Empty Collection
    // ────────────────────────────────────────────────────────────

    #region Empty Collection

    [Fact]
    public void ComputeHash_EmptyCollection_ReturnsZeroHash()
    {
        // Act
        var hash = ReferenceTableHashComputer.ComputeHash<Product>([]);

        // Assert
        hash.ShouldBe("0000000000000000");
    }

    #endregion

    // ────────────────────────────────────────────────────────────
    //  Determinism
    // ────────────────────────────────────────────────────────────

    #region Determinism

    [Fact]
    public void ComputeHash_SameData_ReturnsSameHash()
    {
        // Arrange
        var entities = new List<Product>
        {
            new() { ProductId = 1, Name = "A", Price = 10.0m },
            new() { ProductId = 2, Name = "B", Price = 20.0m }
        };

        // Act
        var hash1 = ReferenceTableHashComputer.ComputeHash<Product>(entities);
        var hash2 = ReferenceTableHashComputer.ComputeHash<Product>(entities);

        // Assert
        hash1.ShouldBe(hash2);
    }

    [Fact]
    public void ComputeHash_DifferentOrder_ReturnsSameHash()
    {
        // Arrange
        var entities1 = new List<Product>
        {
            new() { ProductId = 1, Name = "A", Price = 10.0m },
            new() { ProductId = 2, Name = "B", Price = 20.0m }
        };
        var entities2 = new List<Product>
        {
            new() { ProductId = 2, Name = "B", Price = 20.0m },
            new() { ProductId = 1, Name = "A", Price = 10.0m }
        };

        // Act
        var hash1 = ReferenceTableHashComputer.ComputeHash<Product>(entities1);
        var hash2 = ReferenceTableHashComputer.ComputeHash<Product>(entities2);

        // Assert
        hash1.ShouldBe(hash2);
    }

    [Fact]
    public void ComputeHash_SameDataNewInstances_ReturnsSameHash()
    {
        // Arrange
        var entities1 = new List<SimpleEntity>
        {
            new() { Id = 1, Value = "test" }
        };
        var entities2 = new List<SimpleEntity>
        {
            new() { Id = 1, Value = "test" }
        };

        // Act
        var hash1 = ReferenceTableHashComputer.ComputeHash<SimpleEntity>(entities1);
        var hash2 = ReferenceTableHashComputer.ComputeHash<SimpleEntity>(entities2);

        // Assert
        hash1.ShouldBe(hash2);
    }

    #endregion

    // ────────────────────────────────────────────────────────────
    //  Different Data
    // ────────────────────────────────────────────────────────────

    #region Different Data

    [Fact]
    public void ComputeHash_DifferentData_ReturnsDifferentHash()
    {
        // Arrange
        var entities1 = new List<Product>
        {
            new() { ProductId = 1, Name = "A", Price = 10.0m }
        };
        var entities2 = new List<Product>
        {
            new() { ProductId = 1, Name = "B", Price = 20.0m }
        };

        // Act
        var hash1 = ReferenceTableHashComputer.ComputeHash<Product>(entities1);
        var hash2 = ReferenceTableHashComputer.ComputeHash<Product>(entities2);

        // Assert
        hash1.ShouldNotBe(hash2);
    }

    [Fact]
    public void ComputeHash_AdditionalEntity_ReturnsDifferentHash()
    {
        // Arrange
        var entities1 = new List<Product>
        {
            new() { ProductId = 1, Name = "A", Price = 10.0m }
        };
        var entities2 = new List<Product>
        {
            new() { ProductId = 1, Name = "A", Price = 10.0m },
            new() { ProductId = 2, Name = "B", Price = 20.0m }
        };

        // Act
        var hash1 = ReferenceTableHashComputer.ComputeHash<Product>(entities1);
        var hash2 = ReferenceTableHashComputer.ComputeHash<Product>(entities2);

        // Assert
        hash1.ShouldNotBe(hash2);
    }

    [Fact]
    public void ComputeHash_RemovedEntity_ReturnsDifferentHash()
    {
        // Arrange
        var entities1 = new List<SimpleEntity>
        {
            new() { Id = 1, Value = "A" },
            new() { Id = 2, Value = "B" }
        };
        var entities2 = new List<SimpleEntity>
        {
            new() { Id = 1, Value = "A" }
        };

        // Act
        var hash1 = ReferenceTableHashComputer.ComputeHash<SimpleEntity>(entities1);
        var hash2 = ReferenceTableHashComputer.ComputeHash<SimpleEntity>(entities2);

        // Assert
        hash1.ShouldNotBe(hash2);
    }

    #endregion

    // ────────────────────────────────────────────────────────────
    //  Hash Format
    // ────────────────────────────────────────────────────────────

    #region Hash Format

    [Fact]
    public void ComputeHash_ReturnsHexStringOf16Characters()
    {
        // Arrange
        var entities = new List<SimpleEntity>
        {
            new() { Id = 1, Value = "test" }
        };

        // Act
        var hash = ReferenceTableHashComputer.ComputeHash<SimpleEntity>(entities);

        // Assert
        hash.Length.ShouldBe(16);
        hash.ShouldMatch("^[0-9a-f]{16}$");
    }

    [Fact]
    public void ComputeHash_NonEmpty_DoesNotReturnZeroHash()
    {
        // Arrange
        var entities = new List<SimpleEntity>
        {
            new() { Id = 1, Value = "test" }
        };

        // Act
        var hash = ReferenceTableHashComputer.ComputeHash<SimpleEntity>(entities);

        // Assert
        hash.ShouldNotBe("0000000000000000");
    }

    #endregion
}
