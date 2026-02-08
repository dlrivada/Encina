using System.Data.Common;
using Encina.Caching;
using Encina.EntityFrameworkCore.Caching;
using Microsoft.EntityFrameworkCore;

namespace Encina.GuardTests.EntityFrameworkCore.Caching;

/// <summary>
/// Guard clause tests for all query caching public constructors and methods.
/// Verifies that null parameters throw <see cref="ArgumentNullException"/>
/// with the correct parameter name.
/// </summary>
public class QueryCachingGuardTests
{
    #region DefaultQueryCacheKeyGenerator Guards

    [Fact]
    public void DefaultQueryCacheKeyGenerator_Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(
            () => new DefaultQueryCacheKeyGenerator(null!));
        ex.ParamName.ShouldBe("options");
    }

    [Fact]
    public void DefaultQueryCacheKeyGenerator_Generate_NullCommand_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = new DefaultQueryCacheKeyGenerator(Options.Create(new QueryCacheOptions()));
        var context = Substitute.For<DbContext>();

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(
            () => sut.Generate(null!, context));
        ex.ParamName.ShouldBe("command");
    }

    [Fact]
    public void DefaultQueryCacheKeyGenerator_Generate_NullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = new DefaultQueryCacheKeyGenerator(Options.Create(new QueryCacheOptions()));
        var command = Substitute.For<DbCommand>();

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(
            () => sut.Generate(command, null!));
        ex.ParamName.ShouldBe("context");
    }

    [Fact]
    public void DefaultQueryCacheKeyGenerator_GenerateWithTenant_NullCommand_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = new DefaultQueryCacheKeyGenerator(Options.Create(new QueryCacheOptions()));
        var context = Substitute.For<DbContext>();
        var requestContext = Substitute.For<IRequestContext>();

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(
            () => sut.Generate(null!, context, requestContext));
        ex.ParamName.ShouldBe("command");
    }

    [Fact]
    public void DefaultQueryCacheKeyGenerator_GenerateWithTenant_NullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = new DefaultQueryCacheKeyGenerator(Options.Create(new QueryCacheOptions()));
        var command = Substitute.For<DbCommand>();
        var requestContext = Substitute.For<IRequestContext>();

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(
            () => sut.Generate(command, null!, requestContext));
        ex.ParamName.ShouldBe("context");
    }

    [Fact]
    public void DefaultQueryCacheKeyGenerator_GenerateWithTenant_NullRequestContext_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = new DefaultQueryCacheKeyGenerator(Options.Create(new QueryCacheOptions()));
        var command = Substitute.For<DbCommand>();
        var context = Substitute.For<DbContext>();

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(
            () => sut.Generate(command, context, null!));
        ex.ParamName.ShouldBe("requestContext");
    }

    #endregion

    #region QueryCacheInterceptor Guards

    [Fact]
    public void QueryCacheInterceptor_Constructor_NullCacheProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() => new QueryCacheInterceptor(
            null!,
            Substitute.For<IQueryCacheKeyGenerator>(),
            Options.Create(new QueryCacheOptions()),
            Substitute.For<IServiceProvider>(),
            Substitute.For<ILogger<QueryCacheInterceptor>>()));
        ex.ParamName.ShouldBe("cacheProvider");
    }

    [Fact]
    public void QueryCacheInterceptor_Constructor_NullKeyGenerator_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() => new QueryCacheInterceptor(
            Substitute.For<ICacheProvider>(),
            null!,
            Options.Create(new QueryCacheOptions()),
            Substitute.For<IServiceProvider>(),
            Substitute.For<ILogger<QueryCacheInterceptor>>()));
        ex.ParamName.ShouldBe("keyGenerator");
    }

    [Fact]
    public void QueryCacheInterceptor_Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() => new QueryCacheInterceptor(
            Substitute.For<ICacheProvider>(),
            Substitute.For<IQueryCacheKeyGenerator>(),
            null!,
            Substitute.For<IServiceProvider>(),
            Substitute.For<ILogger<QueryCacheInterceptor>>()));
        ex.ParamName.ShouldBe("options");
    }

    [Fact]
    public void QueryCacheInterceptor_Constructor_NullServiceProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() => new QueryCacheInterceptor(
            Substitute.For<ICacheProvider>(),
            Substitute.For<IQueryCacheKeyGenerator>(),
            Options.Create(new QueryCacheOptions()),
            null!,
            Substitute.For<ILogger<QueryCacheInterceptor>>()));
        ex.ParamName.ShouldBe("serviceProvider");
    }

    [Fact]
    public void QueryCacheInterceptor_Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() => new QueryCacheInterceptor(
            Substitute.For<ICacheProvider>(),
            Substitute.For<IQueryCacheKeyGenerator>(),
            Options.Create(new QueryCacheOptions()),
            Substitute.For<IServiceProvider>(),
            null!));
        ex.ParamName.ShouldBe("logger");
    }

    #endregion

    #region QueryCachingExtensions Guards

    [Fact]
    public void AddQueryCaching_NullServices_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(
            () => QueryCachingExtensions.AddQueryCaching(null!));
        ex.ParamName.ShouldBe("services");
    }

    [Fact]
    public void AddQueryCaching_WithConfigure_NullServices_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(
            () => QueryCachingExtensions.AddQueryCaching(null!, _ => { }));
        ex.ParamName.ShouldBe("services");
    }

    [Fact]
    public void AddQueryCaching_NullConfigure_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(
            () => services.AddQueryCaching(null!));
        ex.ParamName.ShouldBe("configure");
    }

    [Fact]
    public void UseQueryCaching_NullOptionsBuilder_ThrowsArgumentNullException()
    {
        // Arrange
        var sp = new ServiceCollection().BuildServiceProvider();

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(
            () => QueryCachingExtensions.UseQueryCaching(null!, sp));
        ex.ParamName.ShouldBe("optionsBuilder");
    }

    [Fact]
    public void UseQueryCaching_NullServiceProvider_ThrowsArgumentNullException()
    {
        // Arrange
        var optionsBuilder = new DbContextOptionsBuilder();

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(
            () => optionsBuilder.UseQueryCaching(null!));
        ex.ParamName.ShouldBe("serviceProvider");
    }

    #endregion

    #region CachedDataReader Guards

    [Fact]
    public void CachedDataReader_Constructor_NullResult_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(
            () => new CachedDataReader(null!));
        ex.ParamName.ShouldBe("result");
    }

    [Fact]
    public void CachedDataReader_GetOrdinal_NullName_ThrowsArgumentNullException()
    {
        // Arrange
        var result = new CachedQueryResult
        {
            Columns = [new CachedColumnSchema("Id", 0, "int", typeof(int).AssemblyQualifiedName!, false)],
            Rows = [],
            CachedAtUtc = DateTime.UtcNow
        };
        using var sut = new CachedDataReader(result);

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(
            () => sut.GetOrdinal(null!));
        ex.ParamName.ShouldBe("name");
    }

    [Fact]
    public void CachedDataReader_GetValues_NullArray_ThrowsArgumentNullException()
    {
        // Arrange
        var result = new CachedQueryResult
        {
            Columns = [new CachedColumnSchema("Id", 0, "int", typeof(int).AssemblyQualifiedName!, false)],
            Rows = [new object?[] { 1 }],
            CachedAtUtc = DateTime.UtcNow
        };
        using var sut = new CachedDataReader(result);
        sut.Read();

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(
            () => sut.GetValues(null!));
        ex.ParamName.ShouldBe("values");
    }

    #endregion
}
