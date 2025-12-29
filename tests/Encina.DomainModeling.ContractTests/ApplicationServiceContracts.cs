using System.Reflection;
using Encina.DomainModeling;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.DomainModeling.ContractTests;

/// <summary>
/// Contract tests verifying the public API of application service patterns.
/// </summary>
public class ApplicationServiceContracts
{
    // === IApplicationService Interface ===

    [Fact]
    public void IApplicationService_IsInterface()
    {
        Assert.True(typeof(IApplicationService).IsInterface);
    }

    [Fact]
    public void IApplicationService_IsMarkerInterface()
    {
        var members = typeof(IApplicationService).GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        Assert.Empty(members);
    }

    // === IApplicationService<TInput, TOutput> Interface ===

    [Fact]
    public void IApplicationService_WithInputOutput_IsGenericInterface()
    {
        Assert.True(typeof(IApplicationService<,>).IsInterface);
        Assert.True(typeof(IApplicationService<,>).IsGenericType);
        Assert.Equal(2, typeof(IApplicationService<,>).GetGenericArguments().Length);
    }

    [Fact]
    public void IApplicationService_WithInputOutput_ExtendsIApplicationService()
    {
        var interfaces = typeof(IApplicationService<,>).GetInterfaces();
        Assert.Contains(typeof(IApplicationService), interfaces);
    }

    [Fact]
    public void IApplicationService_WithInputOutput_HasExecuteAsyncMethod()
    {
        var method = typeof(IApplicationService<,>).GetMethod("ExecuteAsync");
        Assert.NotNull(method);

        // Check return type is Task<Either>
        Assert.True(method.ReturnType.IsGenericType);
        Assert.Equal(typeof(Task<>), method.ReturnType.GetGenericTypeDefinition());
    }

    [Fact]
    public void IApplicationService_WithInputOutput_ExecuteAsyncHasInputAndCancellationToken()
    {
        var method = typeof(IApplicationService<,>).GetMethod("ExecuteAsync");
        Assert.NotNull(method);

        var parameters = method.GetParameters();
        Assert.Equal(2, parameters.Length);
        Assert.Equal(typeof(CancellationToken), parameters[1].ParameterType);
    }

    // === IApplicationService<TOutput> Interface ===

    [Fact]
    public void IApplicationService_WithOutput_IsGenericInterface()
    {
        Assert.True(typeof(IApplicationService<>).IsInterface);
        Assert.True(typeof(IApplicationService<>).IsGenericType);
        Assert.Single(typeof(IApplicationService<>).GetGenericArguments());
    }

    [Fact]
    public void IApplicationService_WithOutput_ExtendsIApplicationService()
    {
        var interfaces = typeof(IApplicationService<>).GetInterfaces();
        Assert.Contains(typeof(IApplicationService), interfaces);
    }

    [Fact]
    public void IApplicationService_WithOutput_HasExecuteAsyncMethod()
    {
        var method = typeof(IApplicationService<>).GetMethod("ExecuteAsync");
        Assert.NotNull(method);
    }

    [Fact]
    public void IApplicationService_WithOutput_ExecuteAsyncHasOnlyCancellationToken()
    {
        var method = typeof(IApplicationService<>).GetMethod("ExecuteAsync");
        Assert.NotNull(method);

        var parameters = method.GetParameters();
        Assert.Single(parameters);
        Assert.Equal(typeof(CancellationToken), parameters[0].ParameterType);
    }

    // === IVoidApplicationService<TInput> Interface ===

    [Fact]
    public void IVoidApplicationService_IsGenericInterface()
    {
        Assert.True(typeof(IVoidApplicationService<>).IsInterface);
        Assert.True(typeof(IVoidApplicationService<>).IsGenericType);
    }

    [Fact]
    public void IVoidApplicationService_ExtendsIApplicationService()
    {
        var interfaces = typeof(IVoidApplicationService<>).GetInterfaces();
        Assert.Contains(typeof(IApplicationService), interfaces);
    }

    [Fact]
    public void IVoidApplicationService_HasExecuteAsyncMethod()
    {
        var method = typeof(IVoidApplicationService<>).GetMethod("ExecuteAsync");
        Assert.NotNull(method);
    }

    [Fact]
    public void IVoidApplicationService_ExecuteAsyncReturnsEitherUnit()
    {
        var method = typeof(IVoidApplicationService<>).GetMethod("ExecuteAsync");
        Assert.NotNull(method);

        // Task<Either<ApplicationServiceError, Unit>>
        var returnType = method.ReturnType;
        Assert.True(returnType.IsGenericType);
        Assert.Equal(typeof(Task<>), returnType.GetGenericTypeDefinition());

        var taskArg = returnType.GetGenericArguments()[0];
        Assert.True(taskArg.IsGenericType);
        Assert.Equal(typeof(Either<,>), taskArg.GetGenericTypeDefinition());

        var eitherArgs = taskArg.GetGenericArguments();
        Assert.Equal(typeof(ApplicationServiceError), eitherArgs[0]);
        Assert.Equal(typeof(Unit), eitherArgs[1]);
    }

    // === ApplicationServiceError Record ===

    [Fact]
    public void ApplicationServiceError_IsRecord()
    {
        Assert.True(typeof(ApplicationServiceError).IsClass);
        var equalityContract = typeof(ApplicationServiceError).GetProperty("EqualityContract", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(equalityContract);
    }

    [Fact]
    public void ApplicationServiceError_HasRequiredProperties()
    {
        var properties = typeof(ApplicationServiceError).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        Assert.Contains(properties, p => p.Name == "Message");
        Assert.Contains(properties, p => p.Name == "ErrorCode");
        Assert.Contains(properties, p => p.Name == "ServiceType");
        Assert.Contains(properties, p => p.Name == "InnerException");
    }

    [Fact]
    public void ApplicationServiceError_HasNotFoundFactory()
    {
        var method = typeof(ApplicationServiceError).GetMethod("NotFound");
        Assert.NotNull(method);
        Assert.True(method.IsStatic);
        Assert.True(method.IsGenericMethod);
    }

    [Fact]
    public void ApplicationServiceError_HasValidationFailedFactory()
    {
        var method = typeof(ApplicationServiceError).GetMethod("ValidationFailed");
        Assert.NotNull(method);
        Assert.True(method.IsStatic);
        Assert.True(method.IsGenericMethod);
    }

    [Fact]
    public void ApplicationServiceError_HasBusinessRuleViolationFactory()
    {
        var method = typeof(ApplicationServiceError).GetMethod("BusinessRuleViolation");
        Assert.NotNull(method);
        Assert.True(method.IsStatic);
        Assert.True(method.IsGenericMethod);
    }

    [Fact]
    public void ApplicationServiceError_HasConcurrencyConflictFactory()
    {
        var method = typeof(ApplicationServiceError).GetMethod("ConcurrencyConflict");
        Assert.NotNull(method);
        Assert.True(method.IsStatic);
        Assert.True(method.IsGenericMethod);
    }

    [Fact]
    public void ApplicationServiceError_HasInfrastructureFailureFactory()
    {
        var method = typeof(ApplicationServiceError).GetMethod("InfrastructureFailure");
        Assert.NotNull(method);
        Assert.True(method.IsStatic);
        Assert.True(method.IsGenericMethod);
    }

    [Fact]
    public void ApplicationServiceError_HasUnauthorizedFactory()
    {
        var method = typeof(ApplicationServiceError).GetMethod("Unauthorized");
        Assert.NotNull(method);
        Assert.True(method.IsStatic);
        Assert.True(method.IsGenericMethod);
    }

    [Fact]
    public void ApplicationServiceError_HasFromAdapterErrorFactory()
    {
        var method = typeof(ApplicationServiceError).GetMethod("FromAdapterError");
        Assert.NotNull(method);
        Assert.True(method.IsStatic);
        Assert.True(method.IsGenericMethod);
    }

    [Fact]
    public void ApplicationServiceError_HasFromMappingErrorFactory()
    {
        var method = typeof(ApplicationServiceError).GetMethod("FromMappingError");
        Assert.NotNull(method);
        Assert.True(method.IsStatic);
        Assert.True(method.IsGenericMethod);
    }

    [Fact]
    public void ApplicationServiceError_HasFromRepositoryErrorFactory()
    {
        var method = typeof(ApplicationServiceError).GetMethod("FromRepositoryError");
        Assert.NotNull(method);
        Assert.True(method.IsStatic);
        Assert.True(method.IsGenericMethod);
    }

    // === ApplicationServiceExtensions ===

    [Fact]
    public void ApplicationServiceExtensions_IsStaticClass()
    {
        Assert.True(typeof(ApplicationServiceExtensions).IsAbstract);
        Assert.True(typeof(ApplicationServiceExtensions).IsSealed);
    }

    [Fact]
    public void ApplicationServiceExtensions_HasToApplicationServiceErrorForAdapterError()
    {
        var methods = typeof(ApplicationServiceExtensions).GetMethods()
            .Where(m => m.Name == "ToApplicationServiceError");

        Assert.NotEmpty(methods);
    }

    // === ApplicationServiceRegistrationExtensions ===

    [Fact]
    public void ApplicationServiceRegistrationExtensions_IsStaticClass()
    {
        Assert.True(typeof(ApplicationServiceRegistrationExtensions).IsAbstract);
        Assert.True(typeof(ApplicationServiceRegistrationExtensions).IsSealed);
    }

    [Fact]
    public void ApplicationServiceRegistrationExtensions_HasAddApplicationServiceMethod()
    {
        var method = typeof(ApplicationServiceRegistrationExtensions).GetMethod("AddApplicationService");
        Assert.NotNull(method);
        Assert.True(method.IsStatic);
        Assert.True(method.IsGenericMethod);
    }

    [Fact]
    public void ApplicationServiceRegistrationExtensions_HasAddApplicationServicesFromAssemblyMethod()
    {
        var method = typeof(ApplicationServiceRegistrationExtensions).GetMethod("AddApplicationServicesFromAssembly");
        Assert.NotNull(method);
        Assert.True(method.IsStatic);
    }

    [Fact]
    public void AddApplicationService_ReturnsIServiceCollection()
    {
        var method = typeof(ApplicationServiceRegistrationExtensions).GetMethod("AddApplicationService");
        Assert.NotNull(method);
        Assert.Equal(typeof(IServiceCollection), method.ReturnType);
    }
}
