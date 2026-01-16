using Encina.Testing;
using Encina.Testing.Modules;
using LanguageExt;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Testing.Modules;

public sealed class MockModuleApiTests
{
    #region Test Infrastructure

    public interface IInventoryModuleApi
    {
        string ModuleName { get; }
        Task<Either<EncinaError, string>> ReserveStockAsync(string productId, int quantity);
        Either<EncinaError, int> GetAvailableStock(string productId);
        void LogAccess(string message);
    }

    public interface IPaymentModuleApi
    {
        Task<Either<EncinaError, Guid>> ProcessPaymentAsync(decimal amount);
    }

    #endregion

    #region Setup Method Tests

    [Fact]
    public void Setup_NullMethodName_ThrowsArgumentNullException()
    {
        // Arrange
        var mock = new MockModuleApi<IInventoryModuleApi>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            mock.Setup(null!, (string id, int qty) => Task.FromResult(Right<EncinaError, string>("ok"))));
    }

    [Fact]
    public void Setup_NullImplementation_ThrowsArgumentNullException()
    {
        // Arrange
        var mock = new MockModuleApi<IInventoryModuleApi>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            mock.Setup("ReserveStockAsync", null!));
    }

    [Fact]
    public void Setup_ReturnsBuilder()
    {
        // Arrange
        var mock = new MockModuleApi<IInventoryModuleApi>();

        // Act
        var result = mock.Setup("ReserveStockAsync",
            (string id, int qty) => Task.FromResult(Right<EncinaError, string>("reserved")));

        // Assert
        result.ShouldBe(mock);
    }

    #endregion

    #region SetupProperty Tests

    [Fact]
    public void SetupProperty_NullPropertyName_ThrowsArgumentNullException()
    {
        // Arrange
        var mock = new MockModuleApi<IInventoryModuleApi>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            mock.SetupProperty(null!, "value"));
    }

    [Fact]
    public void SetupProperty_ReturnsBuilder()
    {
        // Arrange
        var mock = new MockModuleApi<IInventoryModuleApi>();

        // Act
        var result = mock.SetupProperty("ModuleName", "Inventory");

        // Assert
        result.ShouldBe(mock);
    }

    #endregion

    #region Build Tests

    [Fact]
    public void Build_ReturnsProxyImplementation()
    {
        // Arrange
        var mock = new MockModuleApi<IInventoryModuleApi>();

        // Act
        var api = mock.Build();

        // Assert
        api.ShouldNotBeNull();
        api.ShouldBeAssignableTo<IInventoryModuleApi>();
    }

    [Fact]
    public void Build_ConfiguredMethod_ReturnsSetupValue()
    {
        // Arrange
        var mock = new MockModuleApi<IInventoryModuleApi>()
            .Setup("GetAvailableStock",
                (string productId) => Right<EncinaError, int>(42));

        // Act
        var api = mock.Build();
        var result = api.GetAvailableStock("product-1");

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: v => v.ShouldBe(42),
            Left: _ => throw new InvalidOperationException("Should be right"));
    }

    [Fact]
    public async Task Build_ConfiguredAsyncMethod_ReturnsSetupValue()
    {
        // Arrange
        var mock = new MockModuleApi<IInventoryModuleApi>()
            .Setup("ReserveStockAsync",
                (string productId, int quantity) =>
                    Task.FromResult(Right<EncinaError, string>($"reserved-{productId}-{quantity}")));

        // Act
        var api = mock.Build();
        var result = await api.ReserveStockAsync("product-1", 5);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: v => v.ShouldBe("reserved-product-1-5"),
            Left: _ => throw new InvalidOperationException("Should be right"));
    }

    [Fact]
    public void Build_ConfiguredProperty_ReturnsSetupValue()
    {
        // Arrange
        var mock = new MockModuleApi<IInventoryModuleApi>()
            .SetupProperty("ModuleName", "TestInventory");

        // Act
        var api = mock.Build();

        // Assert
        api.ModuleName.ShouldBe("TestInventory");
    }

    [Fact]
    public void Build_UnconfiguredMethod_ThrowsNotImplementedException()
    {
        // Arrange
        var mock = new MockModuleApi<IInventoryModuleApi>();
        var api = mock.Build();

        // Act & Assert
        Should.Throw<NotImplementedException>(() => api.GetAvailableStock("product-1"));
    }

    [Fact]
    public void Build_UnconfiguredProperty_ThrowsNotImplementedException()
    {
        // Arrange
        var mock = new MockModuleApi<IInventoryModuleApi>();
        var api = mock.Build();

        // Act & Assert
        Should.Throw<NotImplementedException>(() => api.ModuleName);
    }

    [Fact]
    public void Build_VoidMethod_CanBeSetup()
    {
        // Arrange
        var wasCalled = false;
        var mock = new MockModuleApi<IInventoryModuleApi>()
            .Setup("LogAccess", (string message) =>
            {
                wasCalled = true;
                return (object?)null;
            });

        // Act
        var api = mock.Build();
        api.LogAccess("test");

        // Assert
        wasCalled.ShouldBeTrue();
    }

    #endregion

    #region Error Scenario Tests

    [Fact]
    public void Build_MethodReturningError_ReturnsLeft()
    {
        // Arrange
        var mock = new MockModuleApi<IInventoryModuleApi>()
            .Setup("GetAvailableStock",
                (string productId) => Left<EncinaError, int>(
                    EncinaErrors.Create("inventory.not_found", $"Product {productId} not found")));

        // Act
        var api = mock.Build();
        var result = api.GetAvailableStock("unknown-product");

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.Match(
            Right: _ => throw new InvalidOperationException("Should be left"),
            Left: e => e.Message.ShouldContain("not found"));
    }

    [Fact]
    public async Task Build_AsyncMethodReturningError_ReturnsLeft()
    {
        // Arrange
        var mock = new MockModuleApi<IPaymentModuleApi>()
            .Setup("ProcessPaymentAsync",
                (decimal amount) => Task.FromResult(
                    Left<EncinaError, Guid>(
                        EncinaErrors.Create("payment.failed", "Insufficient funds"))));

        // Act
        var api = mock.Build();
        var result = await api.ProcessPaymentAsync(1000m);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.Match(
            Right: _ => throw new InvalidOperationException("Should be left"),
            Left: e => e.Message.ShouldBe("Insufficient funds"));
    }

    #endregion

    #region Fluent Chaining Tests

    [Fact]
    public void Setup_CanBeChained()
    {
        // Arrange & Act
        var mock = new MockModuleApi<IInventoryModuleApi>()
            .SetupProperty("ModuleName", "Inventory")
            .Setup("GetAvailableStock", (string id) => Right<EncinaError, int>(100))
            .Setup("ReserveStockAsync",
                (string id, int qty) => Task.FromResult(Right<EncinaError, string>("ok")));

        var api = mock.Build();

        // Assert
        api.ModuleName.ShouldBe("Inventory");
        api.GetAvailableStock("p1").Match(
            Right: v => v.ShouldBe(100),
            Left: _ => throw new InvalidOperationException("Should be right"));
    }

    #endregion
}
