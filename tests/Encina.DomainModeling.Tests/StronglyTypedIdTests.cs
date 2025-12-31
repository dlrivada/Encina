using System.Reflection;
using Encina.DomainModeling;
using Shouldly;

namespace Encina.DomainModeling.Tests;

public class StronglyTypedIdTests
{
    private sealed class OrderId : GuidStronglyTypedId<OrderId>
    {
        public OrderId(Guid value) : base(value) { }
    }

    private sealed class CustomerId : GuidStronglyTypedId<CustomerId>
    {
        public CustomerId(Guid value) : base(value) { }
    }

    private sealed class ProductSku : StringStronglyTypedId<ProductSku>
    {
        public ProductSku(string value) : base(value) { }
    }

    private sealed class UserId : IntStronglyTypedId<UserId>
    {
        public UserId(int value) : base(value) { }
    }

    private sealed class TransactionId : LongStronglyTypedId<TransactionId>
    {
        public TransactionId(long value) : base(value) { }
    }

    #region GuidStronglyTypedId Tests

    [Fact]
    public void GuidId_WithSameValue_ShouldBeEqual()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var id1 = new OrderId(guid);
        var id2 = new OrderId(guid);

        // Act & Assert
        id1.ShouldBe(id2);
        id1.Equals(id2).ShouldBeTrue();
        (id1 == id2).ShouldBeTrue();
        (id1 != id2).ShouldBeFalse();
    }

    [Fact]
    public void GuidId_WithDifferentValue_ShouldNotBeEqual()
    {
        // Arrange
        var id1 = OrderId.New();
        var id2 = OrderId.New();

        // Act & Assert
        id1.ShouldNotBe(id2);
        (id1 == id2).ShouldBeFalse();
    }

    [Fact]
    public void GuidId_DifferentTypes_ShouldNotBeEqual_EvenWithSameValue()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var orderId = new OrderId(guid);
        var customerId = new CustomerId(guid);

        // Act & Assert
        orderId.Equals(customerId).ShouldBeFalse();
    }

    [Fact]
    public void GuidId_New_ShouldCreateUniqueId()
    {
        // Arrange & Act
        var id1 = OrderId.New();
        var id2 = OrderId.New();

        // Assert
        id1.ShouldNotBe(id2);
        id1.Value.ShouldNotBe(Guid.Empty);
        id2.Value.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void GuidId_From_ShouldCreateIdWithGivenValue()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        var id = OrderId.From(guid);

        // Assert
        id.Value.ShouldBe(guid);
    }

    [Fact]
    public void GuidId_Empty_ShouldReturnEmptyGuid()
    {
        // Act
        var id = OrderId.Empty;

        // Assert
        id.Value.ShouldBe(Guid.Empty);
    }

    [Fact]
    public void GuidId_TryParse_ValidGuid_ShouldReturnSome()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var guidString = guid.ToString();

        // Act
        var result = OrderId.TryParse(guidString);

        // Assert
        result.IsSome.ShouldBeTrue();
        result.IfSome(id => id.Value.ShouldBe(guid));
    }

    [Fact]
    public void GuidId_TryParse_InvalidString_ShouldReturnNone()
    {
        // Act
        var result = OrderId.TryParse("not-a-guid");

        // Assert
        result.IsNone.ShouldBeTrue();
    }

    [Fact]
    public void GuidId_ImplicitConversion_ShouldWork()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var id = new OrderId(guid);

        // Act
        Guid converted = id;

        // Assert
        converted.ShouldBe(guid);
    }

    [Fact]
    public void GuidId_ToString_ShouldReturnGuidString()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var id = new OrderId(guid);

        // Act & Assert
        id.ToString().ShouldBe(guid.ToString());
    }

    [Fact]
    public void GuidId_GetHashCode_ShouldIncludeType()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var orderId = new OrderId(guid);
        var customerId = new CustomerId(guid);

        // Act & Assert - Different types should have different hash codes even with same value
        orderId.GetHashCode().ShouldNotBe(customerId.GetHashCode());
    }

    #endregion

    #region IntStronglyTypedId Tests

    [Fact]
    public void IntId_WithSameValue_ShouldBeEqual()
    {
        // Arrange
        var id1 = UserId.From(42);
        var id2 = UserId.From(42);

        // Act & Assert
        id1.ShouldBe(id2);
    }

    [Fact]
    public void IntId_TryParse_ValidInt_ShouldReturnSome()
    {
        // Act
        var result = UserId.TryParse("42");

        // Assert
        result.IsSome.ShouldBeTrue();
        result.IfSome(id => id.Value.ShouldBe(42));
    }

    [Fact]
    public void IntId_TryParse_InvalidString_ShouldReturnNone()
    {
        // Act
        var result = UserId.TryParse("not-a-number");

        // Assert
        result.IsNone.ShouldBeTrue();
    }

    #endregion

    #region LongStronglyTypedId Tests

    [Fact]
    public void LongId_WithSameValue_ShouldBeEqual()
    {
        // Arrange
        var id1 = TransactionId.From(123456789L);
        var id2 = TransactionId.From(123456789L);

        // Act & Assert
        id1.ShouldBe(id2);
    }

    [Fact]
    public void LongId_TryParse_ValidLong_ShouldReturnSome()
    {
        // Act
        var result = TransactionId.TryParse("123456789");

        // Assert
        result.IsSome.ShouldBeTrue();
        result.IfSome(id => id.Value.ShouldBe(123456789L));
    }

    #endregion

    #region StringStronglyTypedId Tests

    [Fact]
    public void StringId_WithSameValue_ShouldBeEqual()
    {
        // Arrange
        var id1 = ProductSku.From("SKU-001");
        var id2 = ProductSku.From("SKU-001");

        // Act & Assert
        id1.ShouldBe(id2);
    }

    [Fact]
    public void StringId_WithDifferentValue_ShouldNotBeEqual()
    {
        // Arrange
        var id1 = ProductSku.From("SKU-001");
        var id2 = ProductSku.From("SKU-002");

        // Act & Assert
        id1.ShouldNotBe(id2);
    }

    [Fact]
    public void StringId_FromEmptyString_ShouldThrow()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => new ProductSku(""));
    }

    [Fact]
    public void StringId_FromWhitespace_ShouldThrow()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => new ProductSku("   "));
    }

    #endregion

    #region Comparison Tests

    [Fact]
    public void StronglyTypedId_CompareTo_ShouldCompareByValue()
    {
        // Arrange
        var id1 = UserId.From(1);
        var id2 = UserId.From(2);
        var id3 = UserId.From(1);

        // Act & Assert
        id1.CompareTo(id2).ShouldBeLessThan(0);
        id2.CompareTo(id1).ShouldBeGreaterThan(0);
        id1.CompareTo(id3).ShouldBe(0);
    }

    [Fact]
    public void StronglyTypedId_CompareTo_Null_ShouldReturnPositive()
    {
        // Arrange
        var id = UserId.From(1);

        // Act & Assert
        id.CompareTo(null).ShouldBeGreaterThan(0);
    }

    #endregion
}
