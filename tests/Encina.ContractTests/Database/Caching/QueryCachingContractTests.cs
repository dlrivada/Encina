using System.Data.Common;
using System.Reflection;

using Encina.Caching;
using Encina.EntityFrameworkCore;
using Encina.EntityFrameworkCore.Caching;

using Microsoft.EntityFrameworkCore;

namespace Encina.ContractTests.Database.Caching;

/// <summary>
/// Contract tests verifying that <see cref="IQueryCacheKeyGenerator"/> implementations
/// follow the interface contract and produce consistent, well-formed results.
/// </summary>
[Trait("Category", "Contract")]
public sealed class QueryCachingContractTests
{
    #region IQueryCacheKeyGenerator Interface Contract

    [Fact]
    public void Contract_IQueryCacheKeyGenerator_HasGenerateMethod()
    {
        // Contract: IQueryCacheKeyGenerator must have Generate(DbCommand, DbContext)
        var iface = typeof(IQueryCacheKeyGenerator);
        var methods = iface.GetMethods(BindingFlags.Public | BindingFlags.Instance);

        var generateMethod = methods.FirstOrDefault(m =>
            m.Name == "Generate"
            && m.GetParameters().Length == 2
            && m.GetParameters()[0].ParameterType == typeof(DbCommand)
            && m.GetParameters()[1].ParameterType == typeof(DbContext));

        generateMethod.ShouldNotBeNull(
            "IQueryCacheKeyGenerator must have Generate(DbCommand, DbContext) method");
        generateMethod!.ReturnType.ShouldBe(typeof(QueryCacheKey),
            "Generate must return QueryCacheKey");
    }

    [Fact]
    public void Contract_IQueryCacheKeyGenerator_HasGenerateWithTenantMethod()
    {
        // Contract: IQueryCacheKeyGenerator must have Generate(DbCommand, DbContext, IRequestContext)
        var iface = typeof(IQueryCacheKeyGenerator);
        var methods = iface.GetMethods(BindingFlags.Public | BindingFlags.Instance);

        var generateMethod = methods.FirstOrDefault(m =>
            m.Name == "Generate"
            && m.GetParameters().Length == 3
            && m.GetParameters()[0].ParameterType == typeof(DbCommand)
            && m.GetParameters()[1].ParameterType == typeof(DbContext)
            && m.GetParameters()[2].ParameterType == typeof(IRequestContext));

        generateMethod.ShouldNotBeNull(
            "IQueryCacheKeyGenerator must have Generate(DbCommand, DbContext, IRequestContext) method");
        generateMethod!.ReturnType.ShouldBe(typeof(QueryCacheKey),
            "Generate with tenant must return QueryCacheKey");
    }

    [Fact]
    public void Contract_IQueryCacheKeyGenerator_HasExactlyTwoMethods()
    {
        // Contract: IQueryCacheKeyGenerator should have exactly two Generate overloads
        var iface = typeof(IQueryCacheKeyGenerator);
        var methods = iface.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        methods.Length.ShouldBe(2,
            "IQueryCacheKeyGenerator should declare exactly 2 methods");
        methods.ShouldAllBe(m => m.Name == "Generate",
            "All methods should be named 'Generate'");
    }

    #endregion

    #region DefaultQueryCacheKeyGenerator Implementation Contract

    [Fact]
    public void Contract_DefaultQueryCacheKeyGenerator_ImplementsInterface()
    {
        // Contract: DefaultQueryCacheKeyGenerator must implement IQueryCacheKeyGenerator
        typeof(IQueryCacheKeyGenerator)
            .IsAssignableFrom(typeof(DefaultQueryCacheKeyGenerator))
            .ShouldBeTrue("DefaultQueryCacheKeyGenerator must implement IQueryCacheKeyGenerator");
    }

    [Fact]
    public void Contract_DefaultQueryCacheKeyGenerator_IsSealed()
    {
        // Contract: DefaultQueryCacheKeyGenerator should be sealed for performance
        typeof(DefaultQueryCacheKeyGenerator).IsSealed.ShouldBeTrue(
            "DefaultQueryCacheKeyGenerator should be sealed");
    }

    [Fact]
    public void Contract_DefaultQueryCacheKeyGenerator_HasSingleConstructor()
    {
        // Contract: DefaultQueryCacheKeyGenerator should have exactly one public constructor
        var constructors = typeof(DefaultQueryCacheKeyGenerator)
            .GetConstructors(BindingFlags.Public | BindingFlags.Instance);

        constructors.Length.ShouldBe(1,
            "DefaultQueryCacheKeyGenerator should have exactly one public constructor");

        var parameters = constructors[0].GetParameters();
        parameters.Length.ShouldBe(1,
            "Constructor should take exactly one parameter");
        parameters[0].ParameterType.ShouldBe(
            typeof(Microsoft.Extensions.Options.IOptions<QueryCacheOptions>),
            "Constructor parameter should be IOptions<QueryCacheOptions>");
    }

    #endregion

    #region QueryCacheKey Record Contract

    [Fact]
    public void Contract_QueryCacheKey_IsSealed()
    {
        // Contract: QueryCacheKey should be sealed (records are sealed by default)
        typeof(QueryCacheKey).IsSealed.ShouldBeTrue(
            "QueryCacheKey should be sealed");
    }

    [Fact]
    public void Contract_QueryCacheKey_IsRecord()
    {
        // Contract: QueryCacheKey should be a record type
        // Records have a compiler-generated <Clone>$ method
        var cloneMethod = typeof(QueryCacheKey).GetMethod("<Clone>$", BindingFlags.Public | BindingFlags.Instance);
        cloneMethod.ShouldNotBeNull(
            "QueryCacheKey should be a record type (missing <Clone>$ method)");
    }

    [Fact]
    public void Contract_QueryCacheKey_HasKeyProperty()
    {
        // Contract: QueryCacheKey must have a Key property of type string
        var keyProp = typeof(QueryCacheKey).GetProperty("Key", BindingFlags.Public | BindingFlags.Instance);
        keyProp.ShouldNotBeNull("QueryCacheKey must have Key property");
        keyProp!.PropertyType.ShouldBe(typeof(string), "Key must be of type string");
    }

    [Fact]
    public void Contract_QueryCacheKey_HasEntityTypesProperty()
    {
        // Contract: QueryCacheKey must have EntityTypes property of type IReadOnlyList<string>
        var prop = typeof(QueryCacheKey).GetProperty("EntityTypes", BindingFlags.Public | BindingFlags.Instance);
        prop.ShouldNotBeNull("QueryCacheKey must have EntityTypes property");
        prop!.PropertyType.ShouldBe(typeof(IReadOnlyList<string>),
            "EntityTypes must be of type IReadOnlyList<string>");
    }

    [Fact]
    public void Contract_QueryCacheKey_HasRecordSemantics()
    {
        // Contract: QueryCacheKey is a record and supports with-expressions
        var entityTypes = new List<string> { "Order" }.AsReadOnly();
        var key1 = new QueryCacheKey("test:key", entityTypes);
        var key2 = new QueryCacheKey("test:key", entityTypes);

        // Same reference for EntityTypes → equal
        key1.ShouldBe(key2, "QueryCacheKeys with same Key and EntityTypes reference should be equal");
        key1.GetHashCode().ShouldBe(key2.GetHashCode(),
            "Equal QueryCacheKeys should have the same hash code");

        // Key property drives primary comparison
        key1.Key.ShouldBe(key2.Key, "Key values should match");
    }

    [Fact]
    public void Contract_QueryCacheKey_DifferentKeys_AreNotEqual()
    {
        // Contract: QueryCacheKeys with different keys are not equal
        var key1 = new QueryCacheKey("test:key:1", ["Order"]);
        var key2 = new QueryCacheKey("test:key:2", ["Order"]);

        key1.ShouldNotBe(key2, "QueryCacheKeys with different keys should not be equal");
    }

    #endregion

    #region QueryCacheOptions Contract

    [Fact]
    public void Contract_QueryCacheOptions_HasExpectedProperties()
    {
        // Contract: QueryCacheOptions must have required configuration properties
        var type = typeof(QueryCacheOptions);

        type.GetProperty("Enabled").ShouldNotBeNull("Must have Enabled property");
        type.GetProperty("DefaultExpiration").ShouldNotBeNull("Must have DefaultExpiration property");
        type.GetProperty("KeyPrefix").ShouldNotBeNull("Must have KeyPrefix property");
        type.GetProperty("ThrowOnCacheErrors").ShouldNotBeNull("Must have ThrowOnCacheErrors property");
        type.GetProperty("ExcludedEntityTypes").ShouldNotBeNull("Must have ExcludedEntityTypes property");
    }

    [Fact]
    public void Contract_QueryCacheOptions_HasExcludeTypeMethod()
    {
        // Contract: QueryCacheOptions must have ExcludeType<T>() fluent method
        var method = typeof(QueryCacheOptions)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(m => m.Name == "ExcludeType" && m.IsGenericMethod);

        method.ShouldNotBeNull("QueryCacheOptions must have ExcludeType<T>() method");
        method!.ReturnType.ShouldBe(typeof(QueryCacheOptions),
            "ExcludeType<T>() must return QueryCacheOptions for fluent chaining");
    }

    [Fact]
    public void Contract_QueryCacheOptions_DefaultsAreConsistent()
    {
        // Contract: Default values must be usable (not null, reasonable durations)
        var options = new QueryCacheOptions();

        options.KeyPrefix.ShouldNotBeNull("KeyPrefix must not be null by default");
        options.KeyPrefix.ShouldNotBeEmpty("KeyPrefix must not be empty by default");
        options.DefaultExpiration.ShouldBeGreaterThan(TimeSpan.Zero,
            "DefaultExpiration must be positive");
        options.ExcludedEntityTypes.ShouldNotBeNull(
            "ExcludedEntityTypes must not be null by default");
    }

    #endregion

    #region CachedQueryResult Contract

    [Fact]
    public void Contract_CachedQueryResult_HasRequiredProperties()
    {
        // Contract: CachedQueryResult must have Columns, Rows, and CachedAtUtc
        var type = typeof(CachedQueryResult);

        type.GetProperty("Columns").ShouldNotBeNull("Must have Columns property");
        type.GetProperty("Rows").ShouldNotBeNull("Must have Rows property");
        type.GetProperty("CachedAtUtc").ShouldNotBeNull("Must have CachedAtUtc property");
    }

    [Fact]
    public void Contract_CachedColumnSchema_IsRecord()
    {
        // Contract: CachedColumnSchema should be a record type
        var type = typeof(CachedColumnSchema);
        var cloneMethod = type.GetMethod("<Clone>$", BindingFlags.Public | BindingFlags.Instance);
        cloneMethod.ShouldNotBeNull(
            "CachedColumnSchema should be a record type");
    }

    [Fact]
    public void Contract_CachedColumnSchema_HasRequiredProperties()
    {
        // Contract: CachedColumnSchema must have column metadata
        var type = typeof(CachedColumnSchema);

        type.GetProperty("Name").ShouldNotBeNull("Must have Name property");
        type.GetProperty("Ordinal").ShouldNotBeNull("Must have Ordinal property");
        type.GetProperty("DataTypeName").ShouldNotBeNull("Must have DataTypeName property");
        type.GetProperty("FieldType").ShouldNotBeNull("Must have FieldType property");
        type.GetProperty("AllowDBNull").ShouldNotBeNull("Must have AllowDBNull property");
    }

    #endregion

    #region CachedDataReader Contract

    [Fact]
    public void Contract_CachedDataReader_ExtendsDbDataReader()
    {
        // Contract: CachedDataReader must extend DbDataReader
        typeof(DbDataReader).IsAssignableFrom(typeof(CachedDataReader))
            .ShouldBeTrue("CachedDataReader must extend DbDataReader");
    }

    [Fact]
    public void Contract_CachedDataReader_IsSealed()
    {
        // Contract: CachedDataReader should be sealed
        typeof(CachedDataReader).IsSealed.ShouldBeTrue(
            "CachedDataReader should be sealed");
    }

    [Fact]
    public void Contract_CachedDataReader_ImplementsIDisposable()
    {
        // Contract: CachedDataReader must implement IDisposable (from DbDataReader)
        typeof(IDisposable).IsAssignableFrom(typeof(CachedDataReader))
            .ShouldBeTrue("CachedDataReader must implement IDisposable");
    }

    [Fact]
    public void Contract_CachedDataReader_ImplementsIAsyncDisposable()
    {
        // Contract: CachedDataReader must implement IAsyncDisposable (from DbDataReader)
        typeof(IAsyncDisposable).IsAssignableFrom(typeof(CachedDataReader))
            .ShouldBeTrue("CachedDataReader must implement IAsyncDisposable");
    }

    #endregion

    #region QueryCacheInterceptor Contract

    [Fact]
    public void Contract_QueryCacheInterceptor_ExtendsDbCommandInterceptor()
    {
        // Contract: QueryCacheInterceptor must extend DbCommandInterceptor
        typeof(Microsoft.EntityFrameworkCore.Diagnostics.DbCommandInterceptor)
            .IsAssignableFrom(typeof(QueryCacheInterceptor))
            .ShouldBeTrue("QueryCacheInterceptor must extend DbCommandInterceptor");
    }

    [Fact]
    public void Contract_QueryCacheInterceptor_ImplementsSaveChangesInterceptor()
    {
        // Contract: QueryCacheInterceptor must implement ISaveChangesInterceptor
        typeof(Microsoft.EntityFrameworkCore.Diagnostics.ISaveChangesInterceptor)
            .IsAssignableFrom(typeof(QueryCacheInterceptor))
            .ShouldBeTrue("QueryCacheInterceptor must implement ISaveChangesInterceptor");
    }

    [Fact]
    public void Contract_QueryCacheInterceptor_IsSealed()
    {
        // Contract: QueryCacheInterceptor should be sealed
        typeof(QueryCacheInterceptor).IsSealed.ShouldBeTrue(
            "QueryCacheInterceptor should be sealed");
    }

    [Fact]
    public void Contract_QueryCacheInterceptor_HasFiveParameterConstructor()
    {
        // Contract: QueryCacheInterceptor constructor requires all 5 dependencies
        var constructors = typeof(QueryCacheInterceptor)
            .GetConstructors(BindingFlags.Public | BindingFlags.Instance);

        constructors.Length.ShouldBe(1,
            "QueryCacheInterceptor should have exactly one public constructor");

        var parameters = constructors[0].GetParameters();
        parameters.Length.ShouldBe(5,
            "Constructor should take exactly 5 parameters");

        // Verify parameter types
        parameters[0].ParameterType.ShouldBe(typeof(ICacheProvider),
            "First parameter should be ICacheProvider");
        parameters[1].ParameterType.ShouldBe(typeof(IQueryCacheKeyGenerator),
            "Second parameter should be IQueryCacheKeyGenerator");
        parameters[3].ParameterType.ShouldBe(typeof(IServiceProvider),
            "Fourth parameter should be IServiceProvider");
    }

    #endregion

    #region QueryCachingExtensions Contract

    [Fact]
    public void Contract_QueryCachingExtensions_IsStaticClass()
    {
        // Contract: QueryCachingExtensions must be a static class
        var type = typeof(QueryCachingExtensions);
        type.IsAbstract.ShouldBeTrue("Extensions class should be abstract (static)");
        type.IsSealed.ShouldBeTrue("Extensions class should be sealed (static)");
    }

    [Fact]
    public void Contract_QueryCachingExtensions_HasAddQueryCachingOverloads()
    {
        // Contract: Must have AddQueryCaching with and without configure action
        var methods = typeof(QueryCachingExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.Name == "AddQueryCaching")
            .ToList();

        methods.Count.ShouldBe(2,
            "QueryCachingExtensions should have 2 AddQueryCaching overloads");
    }

    [Fact]
    public void Contract_QueryCachingExtensions_HasUseQueryCaching()
    {
        // Contract: Must have UseQueryCaching extension method
        var method = typeof(QueryCachingExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(m => m.Name == "UseQueryCaching");

        method.ShouldNotBeNull(
            "QueryCachingExtensions should have UseQueryCaching method");
        method!.GetParameters().Length.ShouldBe(2,
            "UseQueryCaching should take 2 parameters (optionsBuilder, serviceProvider)");
    }

    #endregion
}
