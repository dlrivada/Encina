using Encina.DomainModeling;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Encina.DomainModeling.ContractTests;

/// <summary>
/// Contract tests verifying the public API of result mapper patterns.
/// </summary>
public class ResultMapperContracts
{
    // === IResultMapper<TDomain, TDto> Interface ===

    [Fact]
    public void IResultMapper_IsGenericInterface()
    {
        Assert.True(typeof(IResultMapper<,>).IsInterface);
        Assert.True(typeof(IResultMapper<,>).IsGenericType);
        Assert.Equal(2, typeof(IResultMapper<,>).GetGenericArguments().Length);
    }

    [Fact]
    public void IResultMapper_HasMapMethod()
    {
        var method = typeof(IResultMapper<,>).GetMethod("Map");
        Assert.NotNull(method);

        // Check return type is Either
        Assert.True(method.ReturnType.IsGenericType);
        Assert.Equal(typeof(Either<,>), method.ReturnType.GetGenericTypeDefinition());
    }

    [Fact]
    public void IResultMapper_MapReturnsEitherWithMappingError()
    {
        var method = typeof(IResultMapper<,>).GetMethod("Map");
        Assert.NotNull(method);

        var returnType = method.ReturnType;
        var genericArgs = returnType.GetGenericArguments();

        Assert.Equal(typeof(MappingError), genericArgs[0]);
    }

    // === IAsyncResultMapper<TDomain, TDto> Interface ===

    [Fact]
    public void IAsyncResultMapper_IsGenericInterface()
    {
        Assert.True(typeof(IAsyncResultMapper<,>).IsInterface);
        Assert.True(typeof(IAsyncResultMapper<,>).IsGenericType);
    }

    [Fact]
    public void IAsyncResultMapper_HasMapAsyncMethod()
    {
        var method = typeof(IAsyncResultMapper<,>).GetMethod("MapAsync");
        Assert.NotNull(method);

        // Check return type is Task<Either>
        Assert.True(method.ReturnType.IsGenericType);
        Assert.Equal(typeof(Task<>), method.ReturnType.GetGenericTypeDefinition());
    }

    [Fact]
    public void IAsyncResultMapper_MapAsyncHasCancellationToken()
    {
        var method = typeof(IAsyncResultMapper<,>).GetMethod("MapAsync");
        Assert.NotNull(method);

        var parameters = method.GetParameters();
        Assert.Equal(2, parameters.Length);
        Assert.Equal(typeof(CancellationToken), parameters[1].ParameterType);
    }

    // === IBidirectionalMapper<TDomain, TDto> Interface ===

    [Fact]
    public void IBidirectionalMapper_IsGenericInterface()
    {
        Assert.True(typeof(IBidirectionalMapper<,>).IsInterface);
        Assert.True(typeof(IBidirectionalMapper<,>).IsGenericType);
    }

    [Fact]
    public void IBidirectionalMapper_ExtendsIResultMapper()
    {
        var interfaces = typeof(IBidirectionalMapper<,>).GetInterfaces();
        Assert.Contains(interfaces, i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IResultMapper<,>));
    }

    [Fact]
    public void IBidirectionalMapper_HasMapToDomainMethod()
    {
        var method = typeof(IBidirectionalMapper<,>).GetMethod("MapToDomain");
        Assert.NotNull(method);
    }

    // === IAsyncBidirectionalMapper<TDomain, TDto> Interface ===

    [Fact]
    public void IAsyncBidirectionalMapper_IsGenericInterface()
    {
        Assert.True(typeof(IAsyncBidirectionalMapper<,>).IsInterface);
        Assert.True(typeof(IAsyncBidirectionalMapper<,>).IsGenericType);
    }

    [Fact]
    public void IAsyncBidirectionalMapper_ExtendsIAsyncResultMapper()
    {
        var interfaces = typeof(IAsyncBidirectionalMapper<,>).GetInterfaces();
        Assert.Contains(interfaces, i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IAsyncResultMapper<,>));
    }

    [Fact]
    public void IAsyncBidirectionalMapper_HasMapToDomainAsyncMethod()
    {
        var method = typeof(IAsyncBidirectionalMapper<,>).GetMethod("MapToDomainAsync");
        Assert.NotNull(method);
    }

    // === MappingError Record ===

    [Fact]
    public void MappingError_IsRecord()
    {
        Assert.True(typeof(MappingError).IsClass);
        var equalityContract = typeof(MappingError).GetProperty("EqualityContract", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(equalityContract);
    }

    [Fact]
    public void MappingError_HasRequiredProperties()
    {
        var properties = typeof(MappingError).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        Assert.Contains(properties, p => p.Name == "Message");
        Assert.Contains(properties, p => p.Name == "ErrorCode");
        Assert.Contains(properties, p => p.Name == "SourceType");
        Assert.Contains(properties, p => p.Name == "TargetType");
        Assert.Contains(properties, p => p.Name == "PropertyName");
        Assert.Contains(properties, p => p.Name == "InnerException");
    }

    [Fact]
    public void MappingError_HasNullPropertyFactory()
    {
        var method = typeof(MappingError).GetMethod("NullProperty");
        Assert.NotNull(method);
        Assert.True(method.IsStatic);
        Assert.True(method.IsGenericMethod);
    }

    [Fact]
    public void MappingError_HasValidationFailedFactory()
    {
        var method = typeof(MappingError).GetMethod("ValidationFailed");
        Assert.NotNull(method);
        Assert.True(method.IsStatic);
        Assert.True(method.IsGenericMethod);
    }

    [Fact]
    public void MappingError_HasConversionFailedFactory()
    {
        var method = typeof(MappingError).GetMethod("ConversionFailed");
        Assert.NotNull(method);
        Assert.True(method.IsStatic);
        Assert.True(method.IsGenericMethod);
    }

    [Fact]
    public void MappingError_HasEmptyCollectionFactory()
    {
        var method = typeof(MappingError).GetMethod("EmptyCollection");
        Assert.NotNull(method);
        Assert.True(method.IsStatic);
        Assert.True(method.IsGenericMethod);
    }

    [Fact]
    public void MappingError_HasOperationFailedFactory()
    {
        var method = typeof(MappingError).GetMethod("OperationFailed");
        Assert.NotNull(method);
        Assert.True(method.IsStatic);
        Assert.True(method.IsGenericMethod);
    }

    // === ResultMapperExtensions ===

    [Fact]
    public void ResultMapperExtensions_IsStaticClass()
    {
        Assert.True(typeof(ResultMapperExtensions).IsAbstract);
        Assert.True(typeof(ResultMapperExtensions).IsSealed);
    }

    [Fact]
    public void ResultMapperExtensions_HasMapAllMethod()
    {
        var method = typeof(ResultMapperExtensions).GetMethod("MapAll",
            BindingFlags.Public | BindingFlags.Static,
            null,
            [typeof(IResultMapper<,>).MakeGenericType(Type.MakeGenericMethodParameter(0), Type.MakeGenericMethodParameter(1)),
             typeof(IEnumerable<>).MakeGenericType(Type.MakeGenericMethodParameter(0))],
            null);

        // Using different approach due to generic method complexity
        var methods = typeof(ResultMapperExtensions).GetMethods()
            .Where(m => m.Name == "MapAll" && !m.Name.Contains("Async") && !m.Name.Contains("Errors"));

        Assert.NotEmpty(methods);
    }

    [Fact]
    public void ResultMapperExtensions_HasMapAllCollectErrorsMethod()
    {
        var methods = typeof(ResultMapperExtensions).GetMethods()
            .Where(m => m.Name == "MapAllCollectErrors");

        Assert.NotEmpty(methods);
    }

    [Fact]
    public void ResultMapperExtensions_HasMapAllAsyncMethod()
    {
        var methods = typeof(ResultMapperExtensions).GetMethods()
            .Where(m => m.Name == "MapAllAsync");

        Assert.NotEmpty(methods);
    }

    [Fact]
    public void ResultMapperExtensions_HasComposeMethod()
    {
        var methods = typeof(ResultMapperExtensions).GetMethods()
            .Where(m => m.Name == "Compose");

        Assert.NotEmpty(methods);
    }

    [Fact]
    public void ResultMapperExtensions_HasTryMapMethod()
    {
        var methods = typeof(ResultMapperExtensions).GetMethods()
            .Where(m => m.Name == "TryMap");

        Assert.NotEmpty(methods);
    }

    // === ResultMapperRegistrationExtensions ===

    [Fact]
    public void ResultMapperRegistrationExtensions_IsStaticClass()
    {
        Assert.True(typeof(ResultMapperRegistrationExtensions).IsAbstract);
        Assert.True(typeof(ResultMapperRegistrationExtensions).IsSealed);
    }

    [Fact]
    public void ResultMapperRegistrationExtensions_HasAddResultMapperMethod()
    {
        var methods = typeof(ResultMapperRegistrationExtensions).GetMethods()
            .Where(m => m.Name == "AddResultMapper");

        Assert.NotEmpty(methods);
    }

    [Fact]
    public void ResultMapperRegistrationExtensions_HasAddAsyncResultMapperMethod()
    {
        var method = typeof(ResultMapperRegistrationExtensions).GetMethod("AddAsyncResultMapper");
        Assert.NotNull(method);
        Assert.True(method.IsStatic);
    }

    [Fact]
    public void ResultMapperRegistrationExtensions_HasAddResultMappersFromAssemblyMethod()
    {
        var method = typeof(ResultMapperRegistrationExtensions).GetMethod("AddResultMappersFromAssembly");
        Assert.NotNull(method);
        Assert.True(method.IsStatic);
    }

    [Fact]
    public void AddResultMapper_ReturnsIServiceCollection()
    {
        var method = typeof(ResultMapperRegistrationExtensions).GetMethods()
            .First(m => m.Name == "AddResultMapper");

        Assert.Equal(typeof(IServiceCollection), method.ReturnType);
    }
}
