using Encina.DomainModeling;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.DomainModeling.Tests;

/// <summary>
/// Tests for ResultMapper classes: MappingError, ResultMapperExtensions, ResultMapperRegistrationExtensions.
/// </summary>
public sealed class ResultMapperTests
{
    #region Test Types

    public sealed record TestDomain(int Id, string Name, decimal Price);
    public sealed record TestDto(int Id, string DisplayName);

    public sealed class TestMapper : IResultMapper<TestDomain, TestDto>
    {
        public Either<MappingError, TestDto> Map(TestDomain domain)
        {
            if (string.IsNullOrEmpty(domain.Name))
            {
                return MappingError.NullProperty<TestDomain, TestDto>("Name");
            }

            return new TestDto(domain.Id, domain.Name);
        }
    }

    public sealed class FailingMapper : IResultMapper<TestDomain, TestDto>
    {
        public Either<MappingError, TestDto> Map(TestDomain domain)
        {
            return MappingError.ValidationFailed<TestDomain, TestDto>("Always fails");
        }
    }

    public sealed class ThrowingMapper : IResultMapper<TestDomain, TestDto>
    {
        public Either<MappingError, TestDto> Map(TestDomain domain)
        {
            throw new InvalidOperationException("Mapping failed");
        }
    }

    public sealed record IntermediateDto(int Id, string Name);

    public sealed class FirstMapper : IResultMapper<TestDomain, IntermediateDto>
    {
        public Either<MappingError, IntermediateDto> Map(TestDomain domain)
        {
            return new IntermediateDto(domain.Id, $"First: {domain.Name}");
        }
    }

    public sealed class SecondMapper : IResultMapper<IntermediateDto, TestDto>
    {
        public Either<MappingError, TestDto> Map(IntermediateDto domain)
        {
            return new TestDto(domain.Id, $"Second({domain.Name})");
        }
    }

    public sealed class AsyncTestMapper : IAsyncResultMapper<TestDomain, TestDto>
    {
        public Task<Either<MappingError, TestDto>> MapAsync(TestDomain domain, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Either<MappingError, TestDto>>(new TestDto(domain.Id, domain.Name));
        }
    }

    public sealed class AsyncFailingMapper : IAsyncResultMapper<TestDomain, TestDto>
    {
        public Task<Either<MappingError, TestDto>> MapAsync(TestDomain domain, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Either<MappingError, TestDto>>(
                MappingError.ValidationFailed<TestDomain, TestDto>("Async failure"));
        }
    }

    #endregion

    #region MappingError Tests

    [Fact]
    public void MappingError_NullProperty_CreatesCorrectError()
    {
        // Act
        var error = MappingError.NullProperty<TestDomain, TestDto>("Name");

        // Assert
        error.ErrorCode.ShouldBe("MAPPING_NULL_PROPERTY");
        error.SourceType.ShouldBe(typeof(TestDomain));
        error.TargetType.ShouldBe(typeof(TestDto));
        error.PropertyName.ShouldBe("Name");
        error.Message.ShouldContain("Name");
    }

    [Fact]
    public void MappingError_ValidationFailed_CreatesCorrectError()
    {
        // Act
        var error = MappingError.ValidationFailed<TestDomain, TestDto>("Invalid price");

        // Assert
        error.ErrorCode.ShouldBe("MAPPING_VALIDATION_FAILED");
        error.Message.ShouldContain("Invalid price");
    }

    [Fact]
    public void MappingError_ConversionFailed_CreatesCorrectError()
    {
        // Arrange
        var exception = new FormatException("Bad format");

        // Act
        var error = MappingError.ConversionFailed<TestDomain, TestDto>("Price", exception);

        // Assert
        error.ErrorCode.ShouldBe("MAPPING_CONVERSION_FAILED");
        error.PropertyName.ShouldBe("Price");
        error.InnerException.ShouldBe(exception);
    }

    [Fact]
    public void MappingError_ConversionFailed_WithoutException_CreatesCorrectError()
    {
        // Act
        var error = MappingError.ConversionFailed<TestDomain, TestDto>("Price");

        // Assert
        error.ErrorCode.ShouldBe("MAPPING_CONVERSION_FAILED");
        error.InnerException.ShouldBeNull();
    }

    [Fact]
    public void MappingError_EmptyCollection_CreatesCorrectError()
    {
        // Act
        var error = MappingError.EmptyCollection<TestDomain, TestDto>("Items");

        // Assert
        error.ErrorCode.ShouldBe("MAPPING_EMPTY_COLLECTION");
        error.PropertyName.ShouldBe("Items");
        error.Message.ShouldContain("Items");
    }

    [Fact]
    public void MappingError_OperationFailed_CreatesCorrectError()
    {
        // Arrange
        var exception = new InvalidOperationException("Failed");

        // Act
        var error = MappingError.OperationFailed<TestDomain, TestDto>(exception);

        // Assert
        error.ErrorCode.ShouldBe("MAPPING_OPERATION_FAILED");
        error.InnerException.ShouldBe(exception);
    }

    #endregion

    #region ResultMapperExtensions Tests

    [Fact]
    public void MapAll_MapsAllItems()
    {
        // Arrange
        var mapper = new TestMapper();
        var domains = new[]
        {
            new TestDomain(1, "Item1", 10m),
            new TestDomain(2, "Item2", 20m),
            new TestDomain(3, "Item3", 30m)
        };

        // Act
        var result = mapper.MapAll(domains);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(dtos =>
        {
            dtos.Count.ShouldBe(3);
            dtos[0].DisplayName.ShouldBe("Item1");
            dtos[1].DisplayName.ShouldBe("Item2");
            dtos[2].DisplayName.ShouldBe("Item3");
        });
    }

    [Fact]
    public void MapAll_ReturnsFirstError()
    {
        // Arrange
        var mapper = new TestMapper();
        var domains = new[]
        {
            new TestDomain(1, "Item1", 10m),
            new TestDomain(2, "", 20m), // Will fail - empty name
            new TestDomain(3, "Item3", 30m)
        };

        // Act
        var result = mapper.MapAll(domains);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.ErrorCode.ShouldBe("MAPPING_NULL_PROPERTY"));
    }

    [Fact]
    public void MapAll_NullMapper_ThrowsArgumentNullException()
    {
        // Arrange
        IResultMapper<TestDomain, TestDto>? mapper = null;
        var domains = new[] { new TestDomain(1, "Item1", 10m) };

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => mapper!.MapAll(domains));
    }

    [Fact]
    public void MapAll_NullDomains_ThrowsArgumentNullException()
    {
        // Arrange
        var mapper = new TestMapper();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => mapper.MapAll(null!));
    }

    [Fact]
    public void MapAllCollectErrors_CollectsAllErrors()
    {
        // Arrange
        var mapper = new TestMapper();
        var domains = new[]
        {
            new TestDomain(1, "", 10m), // Will fail
            new TestDomain(2, "Item2", 20m), // Will succeed
            new TestDomain(3, "", 30m) // Will fail
        };

        // Act
        var result = mapper.MapAllCollectErrors(domains);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(errors => errors.Count.ShouldBe(2));
    }

    [Fact]
    public void MapAllCollectErrors_AllSuccess_ReturnsRight()
    {
        // Arrange
        var mapper = new TestMapper();
        var domains = new[]
        {
            new TestDomain(1, "Item1", 10m),
            new TestDomain(2, "Item2", 20m)
        };

        // Act
        var result = mapper.MapAllCollectErrors(domains);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(dtos => dtos.Count.ShouldBe(2));
    }

    [Fact]
    public void MapAllCollectErrors_NullMapper_ThrowsArgumentNullException()
    {
        // Arrange
        IResultMapper<TestDomain, TestDto>? mapper = null;
        var domains = new[] { new TestDomain(1, "Item1", 10m) };

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => mapper!.MapAllCollectErrors(domains));
    }

    [Fact]
    public void MapAllCollectErrors_NullDomains_ThrowsArgumentNullException()
    {
        // Arrange
        var mapper = new TestMapper();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => mapper.MapAllCollectErrors(null!));
    }

    [Fact]
    public async Task MapAllAsync_MapsAllItems()
    {
        // Arrange
        var mapper = new AsyncTestMapper();
        var domains = new[]
        {
            new TestDomain(1, "Item1", 10m),
            new TestDomain(2, "Item2", 20m)
        };

        // Act
        var result = await mapper.MapAllAsync(domains);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(dtos => dtos.Count.ShouldBe(2));
    }

    [Fact]
    public async Task MapAllAsync_ReturnsFirstError()
    {
        // Arrange
        var mapper = new AsyncFailingMapper();
        var domains = new[]
        {
            new TestDomain(1, "Item1", 10m),
            new TestDomain(2, "Item2", 20m)
        };

        // Act
        var result = await mapper.MapAllAsync(domains);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task MapAllAsync_NullMapper_ThrowsArgumentNullException()
    {
        // Arrange
        IAsyncResultMapper<TestDomain, TestDto>? mapper = null;
        var domains = new[] { new TestDomain(1, "Item1", 10m) };

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await mapper!.MapAllAsync(domains));
    }

    [Fact]
    public async Task MapAllAsync_NullDomains_ThrowsArgumentNullException()
    {
        // Arrange
        var mapper = new AsyncTestMapper();

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await mapper.MapAllAsync(null!));
    }

    [Fact]
    public void Compose_ComposesMappers()
    {
        // Arrange
        var first = new FirstMapper();
        var second = new SecondMapper();
        var domain = new TestDomain(1, "Test", 100m);

        // Act
        var composed = first.Compose(second);
        var result = composed.Map(domain);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(dto => dto.DisplayName.ShouldBe("Second(First: Test)"));
    }

    [Fact]
    public void Compose_NullFirst_ThrowsArgumentNullException()
    {
        // Arrange
        IResultMapper<TestDomain, IntermediateDto>? first = null;
        var second = new SecondMapper();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => first!.Compose(second));
    }

    [Fact]
    public void Compose_NullSecond_ThrowsArgumentNullException()
    {
        // Arrange
        var first = new FirstMapper();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            first.Compose((IResultMapper<IntermediateDto, TestDto>)null!));
    }

    [Fact]
    public void TryMap_Success_ReturnsSome()
    {
        // Arrange
        var mapper = new TestMapper();
        var domain = new TestDomain(1, "Test", 100m);

        // Act
        var result = mapper.TryMap(domain);

        // Assert
        result.IsSome.ShouldBeTrue();
        result.IfSome(dto => dto.DisplayName.ShouldBe("Test"));
    }

    [Fact]
    public void TryMap_MappingFails_ReturnsNone()
    {
        // Arrange
        var mapper = new FailingMapper();
        var domain = new TestDomain(1, "Test", 100m);

        // Act
        var result = mapper.TryMap(domain);

        // Assert
        result.IsNone.ShouldBeTrue();
    }

    [Fact]
    public void TryMap_MapperThrows_ReturnsNone()
    {
        // Arrange
        var mapper = new ThrowingMapper();
        var domain = new TestDomain(1, "Test", 100m);

        // Act
        var result = mapper.TryMap(domain);

        // Assert
        result.IsNone.ShouldBeTrue();
    }

    [Fact]
    public void TryMap_NullMapper_ReturnsNone()
    {
        // Arrange
        IResultMapper<TestDomain, TestDto>? mapper = null;
        var domain = new TestDomain(1, "Test", 100m);

        // Act
        var result = mapper!.TryMap(domain);

        // Assert
        result.IsNone.ShouldBeTrue();
    }

    [Fact]
    public void TryMap_NullDomain_ReturnsNone()
    {
        // Arrange
        var mapper = new TestMapper();

        // Act
        var result = mapper.TryMap(null!);

        // Assert
        result.IsNone.ShouldBeTrue();
    }

    #endregion

    #region ResultMapperRegistrationExtensions Tests

    [Fact]
    public void AddResultMapper_RegistersMapper()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddResultMapper<TestDomain, TestDto, TestMapper>();
        var provider = services.BuildServiceProvider();

        // Assert
        var mapper = provider.GetService<IResultMapper<TestDomain, TestDto>>();
        mapper.ShouldNotBeNull();
        mapper.ShouldBeOfType<TestMapper>();
    }

    [Fact]
    public void AddResultMapper_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services!.AddResultMapper<TestDomain, TestDto, TestMapper>());
    }

    [Fact]
    public void AddResultMapper_WithLifetime_UsesSpecifiedLifetime()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddResultMapper<TestDomain, TestDto, TestMapper>(ServiceLifetime.Singleton);

        // Assert
        var descriptor = services.First();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddAsyncResultMapper_RegistersMapper()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddAsyncResultMapper<TestDomain, TestDto, AsyncTestMapper>();
        var provider = services.BuildServiceProvider();

        // Assert
        var mapper = provider.GetService<IAsyncResultMapper<TestDomain, TestDto>>();
        mapper.ShouldNotBeNull();
        mapper.ShouldBeOfType<AsyncTestMapper>();
    }

    [Fact]
    public void AddAsyncResultMapper_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services!.AddAsyncResultMapper<TestDomain, TestDto, AsyncTestMapper>());
    }

    [Fact]
    public void AddResultMappersFromAssembly_RegistersMappers()
    {
        // Arrange
        var services = new ServiceCollection();
        var assembly = typeof(ResultMapperTests).Assembly;

        // Act
        services.AddResultMappersFromAssembly(assembly);
        var provider = services.BuildServiceProvider();

        // Assert
        // Should find and register TestMapper and AsyncTestMapper
        provider.GetService<IResultMapper<TestDomain, TestDto>>().ShouldNotBeNull();
        provider.GetService<IAsyncResultMapper<TestDomain, TestDto>>().ShouldNotBeNull();
    }

    [Fact]
    public void AddResultMappersFromAssembly_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;
        var assembly = typeof(ResultMapperTests).Assembly;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services!.AddResultMappersFromAssembly(assembly));
    }

    [Fact]
    public void AddResultMappersFromAssembly_NullAssembly_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddResultMappersFromAssembly(null!));
    }

    #endregion
}
