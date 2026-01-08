using Encina.DomainModeling;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.DomainModeling.Tests;

/// <summary>
/// Tests for ApplicationService classes and extensions.
/// </summary>
public sealed class ApplicationServiceTests
{
    #region Test Types

    public sealed record CreateOrderInput(string CustomerId, decimal Amount);
    public sealed record OrderDto(Guid Id, string CustomerId, decimal Amount);

    public sealed class CreateOrderService : IApplicationService<CreateOrderInput, OrderDto>
    {
        public Task<Either<ApplicationServiceError, OrderDto>> ExecuteAsync(
            CreateOrderInput input,
            CancellationToken cancellationToken = default)
        {
            var order = new OrderDto(Guid.NewGuid(), input.CustomerId, input.Amount);
            return Task.FromResult<Either<ApplicationServiceError, OrderDto>>(order);
        }
    }

    public sealed class GetStatsService : IApplicationService<string>
    {
        public Task<Either<ApplicationServiceError, string>> ExecuteAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Either<ApplicationServiceError, string>>("stats");
        }
    }

    public sealed class DeleteOrderService : IVoidApplicationService<Guid>
    {
        public Task<Either<ApplicationServiceError, Unit>> ExecuteAsync(
            Guid input,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Either<ApplicationServiceError, Unit>>(Unit.Default);
        }
    }

    public interface ITestAppPort : IOutboundPort { }

    // Local test entity to avoid depending on RepositoryTests.cs
    public sealed record DummyRepoEntity(Guid Id);

    #endregion

    #region ApplicationServiceError Tests

    [Fact]
    public void ApplicationServiceError_NotFound_CreatesCorrectError()
    {
        // Act
        var error = ApplicationServiceError.NotFound<CreateOrderService>("Customer", "cust-123");

        // Assert
        error.ErrorCode.ShouldBe("APP_SERVICE_NOT_FOUND");
        error.ServiceType.ShouldBe(typeof(CreateOrderService));
        error.Message.ShouldContain("Customer");
        error.Message.ShouldContain("cust-123");
    }

    [Fact]
    public void ApplicationServiceError_ValidationFailed_CreatesCorrectError()
    {
        // Act
        var error = ApplicationServiceError.ValidationFailed<CreateOrderService>("Amount must be positive");

        // Assert
        error.ErrorCode.ShouldBe("APP_SERVICE_VALIDATION_FAILED");
        error.Message.ShouldContain("Amount must be positive");
    }

    [Fact]
    public void ApplicationServiceError_BusinessRuleViolation_CreatesCorrectError()
    {
        // Act
        var error = ApplicationServiceError.BusinessRuleViolation<CreateOrderService>("Order total exceeds credit limit");

        // Assert
        error.ErrorCode.ShouldBe("APP_SERVICE_BUSINESS_RULE_VIOLATION");
        error.Message.ShouldContain("Order total exceeds credit limit");
    }

    [Fact]
    public void ApplicationServiceError_ConcurrencyConflict_CreatesCorrectError()
    {
        // Act
        var error = ApplicationServiceError.ConcurrencyConflict<CreateOrderService>("Order", "order-456");

        // Assert
        error.ErrorCode.ShouldBe("APP_SERVICE_CONCURRENCY_CONFLICT");
        error.Message.ShouldContain("Order");
        error.Message.ShouldContain("order-456");
    }

    [Fact]
    public void ApplicationServiceError_InfrastructureFailure_CreatesCorrectError()
    {
        // Arrange
        var exception = new TimeoutException("Database timeout");

        // Act
        var error = ApplicationServiceError.InfrastructureFailure<CreateOrderService>("SaveOrder", exception);

        // Assert
        error.ErrorCode.ShouldBe("APP_SERVICE_INFRASTRUCTURE_FAILURE");
        error.Message.ShouldContain("SaveOrder");
        error.InnerException.ShouldBe(exception);
    }

    [Fact]
    public void ApplicationServiceError_Unauthorized_CreatesCorrectError()
    {
        // Act
        var error = ApplicationServiceError.Unauthorized<CreateOrderService>("User not allowed to create orders");

        // Assert
        error.ErrorCode.ShouldBe("APP_SERVICE_UNAUTHORIZED");
        error.Message.ShouldContain("User not allowed");
    }

    [Fact]
    public void ApplicationServiceError_FromAdapterError_CreatesCorrectError()
    {
        // Arrange
        var adapterError = AdapterError.OperationFailed<ITestAppPort>("FetchData", new InvalidOperationException("Failed"));

        // Act
        var error = ApplicationServiceError.FromAdapterError<CreateOrderService>(adapterError);

        // Assert
        error.ErrorCode.ShouldBe("APP_SERVICE_ADAPTER_ADAPTER_OPERATION_FAILED");
        error.InnerException.ShouldNotBeNull();
    }

    [Fact]
    public void ApplicationServiceError_FromMappingError_CreatesCorrectError()
    {
        // Arrange
        var mappingError = new MappingError("Mapping failed", "MAPPING_ERROR", typeof(string), typeof(int), null);

        // Act
        var error = ApplicationServiceError.FromMappingError<CreateOrderService>(mappingError);

        // Assert
        error.ErrorCode.ShouldBe("APP_SERVICE_MAPPING_MAPPING_ERROR");
    }

    [Fact]
    public void ApplicationServiceError_FromRepositoryError_CreatesCorrectError()
    {
        // Arrange
        var repoError = RepositoryError.NotFound<DummyRepoEntity, Guid>(Guid.NewGuid());

        // Act
        var error = ApplicationServiceError.FromRepositoryError<CreateOrderService>(repoError);

        // Assert
        error.ErrorCode.ShouldBe("APP_SERVICE_REPOSITORY_REPOSITORY_NOT_FOUND");
    }

    #endregion

    #region ApplicationServiceExtensions Tests

    [Fact]
    public void ToApplicationServiceError_FromAdapterError_Converts()
    {
        // Arrange
        Either<AdapterError, string> result = AdapterError.NotFound<ITestAppPort>("Resource");

        // Act
        var converted = result.ToApplicationServiceError<CreateOrderService, string>();

        // Assert
        converted.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void ToApplicationServiceError_FromAdapterError_PreservesRight()
    {
        // Arrange
        Either<AdapterError, string> result = "success";

        // Act
        var converted = result.ToApplicationServiceError<CreateOrderService, string>();

        // Assert
        converted.IsRight.ShouldBeTrue();
        converted.Match(
            Right: s => s.ShouldBe("success"),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public void ToApplicationServiceError_FromMappingError_Converts()
    {
        // Arrange
        Either<MappingError, string> result = new MappingError("Error", "CODE", typeof(string), typeof(int), null);

        // Act
        var converted = result.ToApplicationServiceError<CreateOrderService, string>();

        // Assert
        converted.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void ToApplicationServiceError_FromRepositoryError_Converts()
    {
        // Arrange
        Either<RepositoryError, string> result = RepositoryError.NotFound<DummyRepoEntity, Guid>(Guid.NewGuid());

        // Act
        var converted = result.ToApplicationServiceError<CreateOrderService, string>();

        // Assert
        converted.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region ApplicationServiceRegistrationExtensions Tests

    [Fact]
    public void AddApplicationService_RegistersService()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddApplicationService<CreateOrderService>();
        using var provider = services.BuildServiceProvider();

        // Assert (inside scope so provider is disposed afterwards)
        provider.GetService<CreateOrderService>().ShouldNotBeNull();
        provider.GetService<IApplicationService<CreateOrderInput, OrderDto>>().ShouldNotBeNull();
    }

    [Fact]
    public void AddApplicationService_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services!.AddApplicationService<CreateOrderService>());
    }

    [Fact]
    public void AddApplicationService_WithLifetime_UsesSpecifiedLifetime()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddApplicationService<CreateOrderService>(ServiceLifetime.Singleton);

        // Assert
        var descriptor = services.First(d => d.ServiceType == typeof(CreateOrderService));
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddApplicationServicesFromAssembly_RegistersServicesFromAssembly()
    {
        // Arrange
        var services = new ServiceCollection();
        var assembly = typeof(ApplicationServiceTests).Assembly;

        // Act
        services.AddApplicationServicesFromAssembly(assembly);
        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<CreateOrderService>().ShouldNotBeNull();
        provider.GetService<GetStatsService>().ShouldNotBeNull();
        provider.GetService<DeleteOrderService>().ShouldNotBeNull();
    }

    [Fact]
    public void AddApplicationServicesFromAssembly_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;
        var assembly = typeof(ApplicationServiceTests).Assembly;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services!.AddApplicationServicesFromAssembly(assembly));
    }

    [Fact]
    public void AddApplicationServicesFromAssembly_NullAssembly_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddApplicationServicesFromAssembly(null!));
    }

    #endregion

    #region Service Execution Tests

    [Fact]
    public async Task ApplicationService_ExecuteAsync_ReturnsResult()
    {
        // Arrange
        var service = new CreateOrderService();
        var input = new CreateOrderInput("cust-1", 100m);

        // Act
        var result = await service.ExecuteAsync(input);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: order =>
            {
                order.CustomerId.ShouldBe("cust-1");
                order.Amount.ShouldBe(100m);
            },
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task VoidApplicationService_ExecuteAsync_ReturnsUnit()
    {
        // Arrange
        var service = new DeleteOrderService();
        var orderId = Guid.NewGuid();

        // Act
        var result = await service.ExecuteAsync(orderId);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task ApplicationServiceWithoutInput_ExecuteAsync_ReturnsResult()
    {
        // Arrange
        var service = new GetStatsService();

        // Act
        var result = await service.ExecuteAsync();

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: stats => stats.ShouldBe("stats"),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    #endregion
}
