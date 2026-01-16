using System.Reflection;
using Encina.DomainModeling;

namespace Encina.GuardTests.DomainModeling;

/// <summary>
/// Guard tests for StronglyTypedId types to verify null/empty parameter handling.
/// </summary>
public class StronglyTypedIdGuardTests
{
    private sealed class OrderId : GuidStronglyTypedId<OrderId>
    {
        public OrderId(Guid value) : base(value) { }
    }

    private sealed class UserId : IntStronglyTypedId<UserId>
    {
        public UserId(int value) : base(value) { }
    }

    private sealed class TransactionId : LongStronglyTypedId<TransactionId>
    {
        public TransactionId(long value) : base(value) { }
    }

    private sealed class ProductSku : StringStronglyTypedId<ProductSku>
    {
        public ProductSku(string value) : base(value) { }
    }

    #region GuidStronglyTypedId Guards

    /// <summary>
    /// Verifies that TryParse handles null string correctly.
    /// </summary>
    [Fact]
    public void GuidId_TryParse_NullString_ReturnsNone()
    {
        // Act
        var result = OrderId.TryParse(null!);

        // Assert
        result.IsNone.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that TryParse handles empty string correctly.
    /// </summary>
    [Fact]
    public void GuidId_TryParse_EmptyString_ReturnsNone()
    {
        // Act
        var result = OrderId.TryParse(string.Empty);

        // Assert
        result.IsNone.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that TryParse handles invalid GUID string correctly.
    /// </summary>
    [Fact]
    public void GuidId_TryParse_InvalidGuid_ReturnsNone()
    {
        // Act
        var result = OrderId.TryParse("not-a-guid");

        // Assert
        result.IsNone.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that CompareTo handles null correctly.
    /// </summary>
    [Fact]
    public void GuidId_CompareTo_Null_ReturnsPositive()
    {
        // Arrange
        var id = OrderId.New();
        OrderId? nullId = null;

        // Act
        var result = id.CompareTo(nullId);

        // Assert
        result.ShouldBeGreaterThan(0);
    }

    /// <summary>
    /// Verifies equality operator with null.
    /// </summary>
    [Fact]
    public void GuidId_EqualityOperator_RightNull_ReturnsFalse()
    {
        // Arrange
        var id = OrderId.New();

        // Act & Assert
        (id == null).ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that Empty property returns an instance with Guid.Empty.
    /// </summary>
    [Fact]
    public void GuidId_Empty_ReturnsEmptyGuid()
    {
        // Act
        var empty = OrderId.Empty;

        // Assert
        empty.Value.ShouldBe(Guid.Empty);
    }

    #endregion

    #region IntStronglyTypedId Guards

    /// <summary>
    /// Verifies that TryParse handles null string correctly.
    /// </summary>
    [Fact]
    public void IntId_TryParse_NullString_ReturnsNone()
    {
        // Act
        var result = UserId.TryParse(null!);

        // Assert
        result.IsNone.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that TryParse handles empty string correctly.
    /// </summary>
    [Fact]
    public void IntId_TryParse_EmptyString_ReturnsNone()
    {
        // Act
        var result = UserId.TryParse(string.Empty);

        // Assert
        result.IsNone.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that TryParse handles invalid int string correctly.
    /// </summary>
    [Fact]
    public void IntId_TryParse_InvalidInt_ReturnsNone()
    {
        // Act
        var result = UserId.TryParse("not-an-int");

        // Assert
        result.IsNone.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that CompareTo handles null correctly.
    /// </summary>
    [Fact]
    public void IntId_CompareTo_Null_ReturnsPositive()
    {
        // Arrange
        var id = UserId.From(1);
        UserId? nullId = null;

        // Act
        var result = id.CompareTo(nullId);

        // Assert
        result.ShouldBeGreaterThan(0);
    }

    #endregion

    #region LongStronglyTypedId Guards

    /// <summary>
    /// Verifies that TryParse handles null string correctly.
    /// </summary>
    [Fact]
    public void LongId_TryParse_NullString_ReturnsNone()
    {
        // Act
        var result = TransactionId.TryParse(null!);

        // Assert
        result.IsNone.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that TryParse handles empty string correctly.
    /// </summary>
    [Fact]
    public void LongId_TryParse_EmptyString_ReturnsNone()
    {
        // Act
        var result = TransactionId.TryParse(string.Empty);

        // Assert
        result.IsNone.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that TryParse handles invalid long string correctly.
    /// </summary>
    [Fact]
    public void LongId_TryParse_InvalidLong_ReturnsNone()
    {
        // Act
        var result = TransactionId.TryParse("not-a-long");

        // Assert
        result.IsNone.ShouldBeTrue();
    }

    #endregion

    #region StringStronglyTypedId Guards

    /// <summary>
    /// Verifies that constructor throws on null string.
    /// </summary>
    [Fact]
    public void StringId_Constructor_NullString_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new ProductSku(null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("value");
    }

    /// <summary>
    /// Verifies that constructor throws on empty string.
    /// </summary>
    [Fact]
    public void StringId_Constructor_EmptyString_ThrowsArgumentException()
    {
        // Act
        var act = () => new ProductSku(string.Empty);

        // Assert
        var ex = Should.Throw<ArgumentException>(act);
        ex.ParamName.ShouldBe("value");
    }

    /// <summary>
    /// Verifies that constructor throws on whitespace string.
    /// </summary>
    [Fact]
    public void StringId_Constructor_WhitespaceString_ThrowsArgumentException()
    {
        // Act
        var act = () => new ProductSku("   ");

        // Assert
        var ex = Should.Throw<ArgumentException>(act);
        ex.ParamName.ShouldBe("value");
    }

    /// <summary>
    /// Verifies that From throws on null string (via reflection).
    /// </summary>
    [Fact]
    public void StringId_From_NullString_ThrowsTargetInvocationException()
    {
        // Act
        var act = () => ProductSku.From(null!);

        // Assert
        var ex = Should.Throw<TargetInvocationException>(act);
        ex.InnerException.ShouldBeOfType<ArgumentNullException>();
    }

    /// <summary>
    /// Verifies that From throws on empty string (via reflection).
    /// </summary>
    [Fact]
    public void StringId_From_EmptyString_ThrowsTargetInvocationException()
    {
        // Act
        var act = () => ProductSku.From(string.Empty);

        // Assert
        var ex = Should.Throw<TargetInvocationException>(act);
        ex.InnerException.ShouldBeOfType<ArgumentException>();
    }

    #endregion
}
