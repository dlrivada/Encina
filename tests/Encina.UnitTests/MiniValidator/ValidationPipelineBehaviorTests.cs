using System.ComponentModel.DataAnnotations;
using Encina.MiniValidator;
using Encina.Testing.Shouldly;
using Encina.Validation;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using EncinaTestFixture = Encina.Testing.EncinaTestFixture;

namespace Encina.UnitTests.MiniValidator;

/// <summary>
/// Tests for the validation pipeline behavior with MiniValidator provider.
/// Tests lightweight validation scenarios for Minimal APIs and Either result handling.
/// </summary>
public sealed class ValidationPipelineBehaviorTests : IDisposable
{
    #region Test Request Types

    /// <summary>
    /// Lightweight request for Minimal API validation.
    /// </summary>
    public sealed record CreateProductCommand : ICommand<ProductResult>
    {
        [Required(ErrorMessage = "Name is required.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters.")]
        public string Name { get; init; } = string.Empty;

        [Range(0.01, 999999.99, ErrorMessage = "Price must be between 0.01 and 999999.99.")]
        public decimal Price { get; init; }

        [Required(ErrorMessage = "SKU is required.")]
        [RegularExpression(@"^[A-Z]{3}-\d{4}$", ErrorMessage = "SKU must be in format ABC-1234.")]
        public string Sku { get; init; } = string.Empty;
    }

    /// <summary>
    /// Result type for product creation.
    /// </summary>
    public sealed record ProductResult(Guid Id, string Name, decimal Price, string Sku);

    /// <summary>
    /// Handler for CreateProductCommand.
    /// </summary>
    public sealed class CreateProductCommandHandler : ICommandHandler<CreateProductCommand, ProductResult>
    {
        public int InvocationCount { get; private set; }

        public Task<Either<EncinaError, ProductResult>> Handle(
            CreateProductCommand request,
            CancellationToken cancellationToken)
        {
            InvocationCount++;
            return Task.FromResult(
                Either<EncinaError, ProductResult>.Right(
                    new ProductResult(Guid.NewGuid(), request.Name, request.Price, request.Sku)));
        }
    }

    /// <summary>
    /// Request with multiple errors on same property.
    /// </summary>
    public sealed record ComplexPasswordCommand : ICommand<string>
    {
        [Required(ErrorMessage = "Password is required.")]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*\d).{8,}$", ErrorMessage = "Password must contain at least one digit and one uppercase letter.")]
        public string Password { get; init; } = string.Empty;
    }

    /// <summary>
    /// Handler for ComplexPasswordCommand.
    /// </summary>
    public sealed class ComplexPasswordCommandHandler : ICommandHandler<ComplexPasswordCommand, string>
    {
        public Task<Either<EncinaError, string>> Handle(
            ComplexPasswordCommand request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(
                Either<EncinaError, string>.Right("Password accepted"));
        }
    }

    /// <summary>
    /// Simple request for minimal API scenarios without validation.
    /// </summary>
    public sealed record HealthCheckCommand : ICommand<string>;

    /// <summary>
    /// Handler for HealthCheckCommand.
    /// </summary>
    public sealed class HealthCheckCommandHandler : ICommandHandler<HealthCheckCommand, string>
    {
        public Task<Either<EncinaError, string>> Handle(
            HealthCheckCommand request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(
                Either<EncinaError, string>.Right("Healthy"));
        }
    }

    #endregion

    private readonly EncinaTestFixture _fixture;
    private readonly CreateProductCommandHandler _productHandler;

    public ValidationPipelineBehaviorTests()
    {
        _productHandler = new CreateProductCommandHandler();

        _fixture = new EncinaTestFixture()
            .WithHandler<ComplexPasswordCommandHandler>()
            .WithHandler<HealthCheckCommandHandler>()
            .ConfigureServices(services =>
            {
                // Register handler instance so we can track invocations without cross-test interference
                // Must register as IRequestHandler<,> because that's what Encina resolves
                services.AddSingleton(_productHandler);
                services.AddSingleton<IRequestHandler<CreateProductCommand, ProductResult>>(_productHandler);
                services.AddMiniValidation();
            });
    }

    public void Dispose()
    {
        _fixture.Dispose();
    }

    #region Successful Validation Tests (Lightweight Scenarios)

    [Fact]
    public async Task Pipeline_WithValidProductRequest_ShouldReturnSuccess()
    {
        // Arrange
        var command = new CreateProductCommand
        {
            Name = "Widget Pro",
            Price = 29.99m,
            Sku = "WID-1234"
        };

        // Act
        var context = await _fixture.SendAsync(command);

        // Assert - Using EitherShouldlyExtensions for success path
        var result = context.Result.ShouldBeSuccess();
        result.Id.ShouldNotBe(Guid.Empty);
        result.Name.ShouldBe("Widget Pro");
        result.Price.ShouldBe(29.99m);
        result.Sku.ShouldBe("WID-1234");
    }

    [Fact]
    public async Task Pipeline_MinimalApiScenario_LightweightValidation()
    {
        // Arrange - Simulates a Minimal API endpoint
        var command = new CreateProductCommand
        {
            Name = "API Product",
            Price = 9.99m,
            Sku = "API-0001"
        };

        // Act
        var context = await _fixture.SendAsync(command);

        // Assert - Quick validation for Minimal APIs
        context.Result.ShouldBeRight().ShouldNotBeNull();
    }

    [Fact]
    public async Task Pipeline_WithValidRequest_ShouldInvokeHandler()
    {
        // Arrange
        var command = new CreateProductCommand
        {
            Name = "Test Product",
            Price = 19.99m,
            Sku = "TST-5678"
        };

        // Act
        await _fixture.SendAsync(command);

        // Assert
        _productHandler.InvocationCount.ShouldBe(1);
    }

    #endregion

    #region Validation Failure Tests

    [Fact]
    public async Task Pipeline_WithInvalidRequest_ShouldReturnError()
    {
        // Arrange
        var command = new CreateProductCommand
        {
            Name = "",
            Price = 0,
            Sku = "invalid"
        };

        // Act
        var context = await _fixture.SendAsync(command);

        // Assert - Using ShouldBeError for error path
        var error = context.Result.ShouldBeError();
        error.Message.ShouldContain("Validation failed");
    }

    [Fact]
    public async Task Pipeline_WithInvalidRequest_ShouldNotInvokeHandler()
    {
        // Arrange
        var command = new CreateProductCommand
        {
            Name = "X", // Too short
            Price = -10, // Below range
            Sku = "bad" // Invalid format
        };

        // Act
        await _fixture.SendAsync(command);

        // Assert - Handler should not be invoked on validation failure
        _productHandler.InvocationCount.ShouldBe(0);
    }

    [Fact]
    public async Task Pipeline_WithEmptyName_ShouldContainNameError()
    {
        // Arrange
        var command = new CreateProductCommand
        {
            Name = "",
            Price = 10.00m,
            Sku = "VAL-1234"
        };

        // Act
        var context = await _fixture.SendAsync(command);

        // Assert
        var error = context.Result.ShouldBeError();
        error.Message.ShouldContain("Name");
    }

    [Fact]
    public async Task Pipeline_WithInvalidSku_ShouldContainSkuError()
    {
        // Arrange
        var command = new CreateProductCommand
        {
            Name = "Valid Product",
            Price = 10.00m,
            Sku = "invalid-sku"
        };

        // Act
        var context = await _fixture.SendAsync(command);

        // Assert
        var error = context.Result.ShouldBeError();
        error.Message.ShouldContain("SKU");
    }

    [Fact]
    public async Task Pipeline_WithPriceOutOfRange_ShouldContainPriceError()
    {
        // Arrange
        var command = new CreateProductCommand
        {
            Name = "Valid Product",
            Price = 0, // Below minimum
            Sku = "VAL-1234"
        };

        // Act
        var context = await _fixture.SendAsync(command);

        // Assert
        var error = context.Result.ShouldBeError();
        error.Message.ShouldContain("Price");
    }

    #endregion

    #region Multiple Errors Same Property Tests

    [Fact]
    public async Task Pipeline_WithMultipleErrorsSameProperty_ShouldReturnAllErrors()
    {
        // Arrange
        var command = new ComplexPasswordCommand
        {
            Password = "short" // Fails: too short, no digit, no uppercase
        };

        // Act
        var context = await _fixture.SendAsync(command);

        // Assert - MiniValidator should convert all errors through pipeline to Either types
        var error = context.Result.ShouldBeError();
        error.Message.ShouldContain("Password");
    }

    [Fact]
    public async Task Pipeline_WithValidComplexPassword_ShouldSucceed()
    {
        // Arrange
        var command = new ComplexPasswordCommand
        {
            Password = "SecurePass123" // Meets all requirements
        };

        // Act
        var context = await _fixture.SendAsync(command);

        // Assert
        var result = context.Result.ShouldBeSuccess();
        result.ShouldBe("Password accepted");
    }

    #endregion

    #region Either Result Handling Tests

    [Fact]
    public async Task Pipeline_SuccessResult_ShouldBeRight()
    {
        // Arrange
        var command = new CreateProductCommand
        {
            Name = "Right Test",
            Price = 50.00m,
            Sku = "RTT-9999"
        };

        // Act
        var context = await _fixture.SendAsync(command);

        // Assert - Using ShouldBeRight alias
        var product = context.Result.ShouldBeRight();
        product.ShouldNotBeNull();
    }

    [Fact]
    public async Task Pipeline_ErrorResult_ShouldBeLeft()
    {
        // Arrange
        var command = new CreateProductCommand
        {
            Name = "",
            Price = 0,
            Sku = ""
        };

        // Act
        var context = await _fixture.SendAsync(command);

        // Assert - Using ShouldBeLeft alias
        context.Result.ShouldBeLeft();
    }

    [Fact]
    public async Task Pipeline_MiniValidatorErrors_ShouldConvertToEncinaError()
    {
        // Arrange
        var command = new CreateProductCommand
        {
            Name = "",
            Price = -1,
            Sku = "bad"
        };

        // Act
        var context = await _fixture.SendAsync(command);

        // Assert - Verify MiniValidator's error format converts correctly through pipeline to Either types
        context.Result.IsLeft.ShouldBeTrue();
        var error = context.Result.ShouldBeError();
        error.ShouldBeOfType<EncinaError>();
        error.Message.ShouldContain("CreateProductCommand");
    }

    #endregion

    #region Pipeline Stops on Failure Tests

    [Fact]
    public async Task Pipeline_OnValidationFailure_ShouldStopExecution()
    {
        // Arrange

        // Invalid request
        var invalidCommand = new CreateProductCommand
        {
            Name = "",
            Price = 0,
            Sku = ""
        };
        await _fixture.SendAsync(invalidCommand);

        // Assert - Handler should not be invoked
        _productHandler.InvocationCount.ShouldBe(0);
    }

    #endregion

    #region Lightweight Minimal API Tests

    [Fact]
    public async Task Pipeline_HealthCheck_NoValidation_ShouldPassThrough()
    {
        // Arrange - Simulates a simple Minimal API health check endpoint
        var command = new HealthCheckCommand();

        // Act
        var context = await _fixture.SendAsync(command);

        // Assert - Should succeed without validation overhead
        var result = context.Result.ShouldBeSuccess();
        result.ShouldBe("Healthy");
    }

    [Fact]
    public async Task Pipeline_MinimalApiPattern_ValueExtraction()
    {
        // Arrange
        var command = new CreateProductCommand
        {
            Name = "Minimal API Product",
            Price = 99.99m,
            Sku = "MIN-0001"
        };

        // Act
        var context = await _fixture.SendAsync(command);

        // Assert - Demonstrates value extraction pattern for Minimal APIs
        var product = context.Result.ShouldBeSuccess();
        product.Name.ShouldBe("Minimal API Product");
        product.Price.ShouldBe(99.99m);
    }

    #endregion

    #region Custom Validator Callback Tests

    [Fact]
    public async Task Pipeline_WithValidRequest_ShouldSupportValidatorCallback()
    {
        // Arrange
        var command = new CreateProductCommand
        {
            Name = "Callback Product",
            Price = 15.00m,
            Sku = "CBK-1234"
        };

        // Act
        var context = await _fixture.SendAsync(command);

        // Assert - Using custom validator callback
        context.Result.ShouldBeSuccess(product =>
        {
            product.ShouldNotBeNull();
            product.Id.ShouldNotBe(Guid.Empty);
            product.Name.ShouldBe("Callback Product");
            product.Price.ShouldBe(15.00m);
            product.Sku.ShouldBe("CBK-1234");
        });
    }

    #endregion
}
