using Encina.DomainModeling;
using FsCheck;
using FsCheck.Xunit;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using static LanguageExt.Prelude;

namespace Encina.DomainModeling.PropertyTests;

/// <summary>
/// Property-based tests for application service patterns.
/// </summary>
public class ApplicationServiceProperties
{
    // Test types
    private sealed record TestInput(string Value, int Number);
    private sealed record TestOutput(string ProcessedValue, int ProcessedNumber);

    private sealed class TestApplicationService : IApplicationService<TestInput, TestOutput>
    {
        public Task<Either<ApplicationServiceError, TestOutput>> ExecuteAsync(
            TestInput input,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(input.Value))
            {
                return Task.FromResult<Either<ApplicationServiceError, TestOutput>>(
                    ApplicationServiceError.ValidationFailed<TestApplicationService>("Value is required"));
            }

            return Task.FromResult<Either<ApplicationServiceError, TestOutput>>(
                new TestOutput(input.Value.ToUpperInvariant(), input.Number * 2));
        }
    }

    private sealed class ParameterlessApplicationService : IApplicationService<TestOutput>
    {
        public Task<Either<ApplicationServiceError, TestOutput>> ExecuteAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Either<ApplicationServiceError, TestOutput>>(
                new TestOutput("Default", 42));
        }
    }

    private sealed class VoidApplicationService : IVoidApplicationService<TestInput>
    {
        public Task<Either<ApplicationServiceError, Unit>> ExecuteAsync(
            TestInput input,
            CancellationToken cancellationToken = default)
        {
            if (input.Number < 0)
            {
                return Task.FromResult<Either<ApplicationServiceError, Unit>>(
                    ApplicationServiceError.ValidationFailed<VoidApplicationService>("Number must be positive"));
            }

            return Task.FromResult<Either<ApplicationServiceError, Unit>>(Unit.Default);
        }
    }

    // === ApplicationServiceError Factory Methods ===

    [Property(MaxTest = 100)]
    public bool ApplicationServiceError_NotFound_HasCorrectCode(
        NonEmptyString entityType,
        NonEmptyString entityId)
    {
        var error = ApplicationServiceError.NotFound<TestApplicationService>(entityType.Get, entityId.Get);

        return error.ErrorCode == "APP_SERVICE_NOT_FOUND"
            && error.ServiceType == typeof(TestApplicationService)
            && error.Message.Contains(entityType.Get)
            && error.Message.Contains(entityId.Get);
    }

    [Property(MaxTest = 100)]
    public bool ApplicationServiceError_ValidationFailed_HasCorrectCode(NonEmptyString reason)
    {
        var error = ApplicationServiceError.ValidationFailed<TestApplicationService>(reason.Get);

        return error.ErrorCode == "APP_SERVICE_VALIDATION_FAILED"
            && error.ServiceType == typeof(TestApplicationService)
            && error.Message.Contains(reason.Get);
    }

    [Property(MaxTest = 100)]
    public bool ApplicationServiceError_BusinessRuleViolation_HasCorrectCode(NonEmptyString rule)
    {
        var error = ApplicationServiceError.BusinessRuleViolation<TestApplicationService>(rule.Get);

        return error.ErrorCode == "APP_SERVICE_BUSINESS_RULE_VIOLATION"
            && error.ServiceType == typeof(TestApplicationService)
            && error.Message.Contains(rule.Get);
    }

    [Property(MaxTest = 100)]
    public bool ApplicationServiceError_ConcurrencyConflict_HasCorrectCode(
        NonEmptyString entityType,
        NonEmptyString entityId)
    {
        var error = ApplicationServiceError.ConcurrencyConflict<TestApplicationService>(entityType.Get, entityId.Get);

        return error.ErrorCode == "APP_SERVICE_CONCURRENCY_CONFLICT"
            && error.ServiceType == typeof(TestApplicationService)
            && error.Message.Contains(entityType.Get)
            && error.Message.Contains(entityId.Get);
    }

    [Property(MaxTest = 100)]
    public bool ApplicationServiceError_InfrastructureFailure_HasCorrectCode(NonEmptyString operation)
    {
        var exception = new InvalidOperationException("DB connection failed");
        var error = ApplicationServiceError.InfrastructureFailure<TestApplicationService>(operation.Get, exception);

        return error.ErrorCode == "APP_SERVICE_INFRASTRUCTURE_FAILURE"
            && error.ServiceType == typeof(TestApplicationService)
            && error.InnerException == exception
            && error.Message.Contains(operation.Get);
    }

    [Property(MaxTest = 100)]
    public bool ApplicationServiceError_Unauthorized_HasCorrectCode(NonEmptyString reason)
    {
        var error = ApplicationServiceError.Unauthorized<TestApplicationService>(reason.Get);

        return error.ErrorCode == "APP_SERVICE_UNAUTHORIZED"
            && error.ServiceType == typeof(TestApplicationService)
            && error.Message.Contains(reason.Get);
    }

    // === IApplicationService<TInput, TOutput> ===

    [Property(MaxTest = 100)]
    public async Task ApplicationService_ExecuteAsync_SucceedsForValidInput(NonEmptyString value, int number)
    {
        var service = new TestApplicationService();
        var input = new TestInput(value.Get, number);

        var result = await service.ExecuteAsync(input);

        Assert.True(result.IsRight);
        result.IfRight(output =>
        {
            Assert.Equal(value.Get.ToUpperInvariant(), output.ProcessedValue);
            Assert.Equal(number * 2, output.ProcessedNumber);
        });
    }

    [Property(MaxTest = 100)]
    public async Task ApplicationService_ExecuteAsync_FailsForInvalidInput(int number)
    {
        var service = new TestApplicationService();
        var input = new TestInput(string.Empty, number);

        var result = await service.ExecuteAsync(input);

        Assert.True(result.IsLeft);
    }

    // === IApplicationService<TOutput> ===

    [Property(MaxTest = 100)]
    public async Task ParameterlessApplicationService_ExecuteAsync_Succeeds()
    {
        var service = new ParameterlessApplicationService();

        var result = await service.ExecuteAsync();

        Assert.True(result.IsRight);
        result.IfRight(output =>
        {
            Assert.Equal("Default", output.ProcessedValue);
            Assert.Equal(42, output.ProcessedNumber);
        });
    }

    // === IVoidApplicationService<TInput> ===

    [Property(MaxTest = 100)]
    public async Task VoidApplicationService_ExecuteAsync_ReturnsUnitOnSuccess(NonEmptyString value, PositiveInt number)
    {
        var service = new VoidApplicationService();
        var input = new TestInput(value.Get, number.Get);

        var result = await service.ExecuteAsync(input);

        Assert.True(result.IsRight);
    }

    [Property(MaxTest = 100)]
    public async Task VoidApplicationService_ExecuteAsync_FailsForNegativeNumber(NonEmptyString value, NegativeInt number)
    {
        var service = new VoidApplicationService();
        var input = new TestInput(value.Get, number.Get);

        var result = await service.ExecuteAsync(input);

        Assert.True(result.IsLeft);
    }

    // === Error Conversion ===

    [Property(MaxTest = 100)]
    public bool ApplicationServiceError_FromAdapterError_PreservesMessage(NonEmptyString operationName)
    {
        var adapterError = AdapterError.OperationFailed<ITestPort>(operationName.Get, new InvalidOperationException("Test"));
        var appServiceError = ApplicationServiceError.FromAdapterError<TestApplicationService>(adapterError);

        return appServiceError.Message == adapterError.Message
            && appServiceError.ErrorCode.StartsWith("APP_SERVICE_ADAPTER_", StringComparison.Ordinal);
    }

    [Property(MaxTest = 100)]
    public bool ApplicationServiceError_FromMappingError_PreservesMessage(NonEmptyString propertyName)
    {
        var mappingError = MappingError.NullProperty<TestInput, TestOutput>(propertyName.Get);
        var appServiceError = ApplicationServiceError.FromMappingError<TestApplicationService>(mappingError);

        return appServiceError.Message == mappingError.Message
            && appServiceError.ErrorCode.StartsWith("APP_SERVICE_MAPPING_", StringComparison.Ordinal);
    }

    [Property(MaxTest = 100)]
    public bool ApplicationServiceError_FromRepositoryError_PreservesMessage(Guid entityId)
    {
        var repositoryError = RepositoryError.NotFound<TestEntity, Guid>(entityId);
        var appServiceError = ApplicationServiceError.FromRepositoryError<TestApplicationService>(repositoryError);

        return appServiceError.Message == repositoryError.Message
            && appServiceError.ErrorCode.StartsWith("APP_SERVICE_REPOSITORY_", StringComparison.Ordinal);
    }

    // === DI Registration ===

    [Property(MaxTest = 100)]
    public bool AddApplicationService_RegistersCorrectly()
    {
        var services = new ServiceCollection();
        services.AddApplicationService<TestApplicationService>();

        var provider = services.BuildServiceProvider();
        var service = provider.GetService<IApplicationService<TestInput, TestOutput>>();

        return service is not null;
    }

    private interface ITestPort : IOutboundPort { }
    private sealed class TestEntity(Guid id) : Entity<Guid>(id);
}
