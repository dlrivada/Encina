using Encina.DomainModeling;
using LanguageExt;
using Shouldly;

namespace Encina.DomainModeling.Tests;

public sealed class DomainBuilderTests
{
    private sealed class TestProduct
    {
        public string Name { get; init; } = string.Empty;
        public decimal Price { get; init; }
    }

    private sealed class TestProductBuilder : DomainBuilder<TestProduct, TestProductBuilder>
    {
        private string? _name;
        private decimal _price;

        protected override TestProductBuilder This => this;

        public TestProductBuilder WithName(string name)
        {
            _name = name;
            return this;
        }

        public TestProductBuilder WithPrice(decimal price)
        {
            _price = price;
            return this;
        }

        public override Either<DomainBuilderError, TestProduct> Build()
        {
            if (string.IsNullOrWhiteSpace(_name))
                return DomainBuilderError.MissingValue("Name");

            if (_price < 0)
                return DomainBuilderError.ValidationFailed("Price cannot be negative");

            return new TestProduct { Name = _name, Price = _price };
        }
    }

    [Fact]
    public void DomainBuilder_Build_Success_ReturnsRight()
    {
        // Arrange
        var builder = new TestProductBuilder()
            .WithName("Test Product")
            .WithPrice(29.99m);

        // Act
        var result = builder.Build();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: p =>
            {
                p.Name.ShouldBe("Test Product");
                p.Price.ShouldBe(29.99m);
            },
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public void DomainBuilder_Build_MissingValue_ReturnsLeft()
    {
        // Arrange
        var builder = new TestProductBuilder()
            .WithPrice(29.99m); // Missing name

        // Act
        var result = builder.Build();

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: error => error.ErrorCode.ShouldBe("BUILDER_MISSING_VALUE"));
    }

    [Fact]
    public void DomainBuilder_Build_ValidationFailed_ReturnsLeft()
    {
        // Arrange
        var builder = new TestProductBuilder()
            .WithName("Test")
            .WithPrice(-10m); // Invalid price

        // Act
        var result = builder.Build();

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: error => error.ErrorCode.ShouldBe("BUILDER_VALIDATION_FAILED"));
    }

    [Fact]
    public void DomainBuilder_BuildOrThrow_Success_ReturnsValue()
    {
        // Arrange
        var builder = new TestProductBuilder()
            .WithName("Test Product")
            .WithPrice(29.99m);

        // Act
        var product = builder.BuildOrThrow();

        // Assert
        product.Name.ShouldBe("Test Product");
    }

    [Fact]
    public void DomainBuilder_BuildOrThrow_Failure_ThrowsInvalidOperationException()
    {
        // Arrange
        var builder = new TestProductBuilder().WithPrice(10m); // Missing name

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => builder.BuildOrThrow());
    }

    [Fact]
    public void DomainBuilder_TryBuild_Success_ReturnsSome()
    {
        // Arrange
        var builder = new TestProductBuilder()
            .WithName("Test")
            .WithPrice(10m);

        // Act
        var result = builder.TryBuild();

        // Assert
        result.IsSome.ShouldBeTrue();
    }

    [Fact]
    public void DomainBuilder_TryBuild_Failure_ReturnsNone()
    {
        // Arrange
        var builder = new TestProductBuilder().WithPrice(10m); // Missing name

        // Act
        var result = builder.TryBuild();

        // Assert
        result.IsNone.ShouldBeTrue();
    }
}
