using System.Reflection;
using Encina.DomainModeling;
using Shouldly;
using ADOMySQLRepository = Encina.ADO.MySQL.Repository;
using ADOOracleRepository = Encina.ADO.Oracle.Repository;
using ADOPostgreSQLRepository = Encina.ADO.PostgreSQL.Repository;
using ADOSqliteRepository = Encina.ADO.Sqlite.Repository;
using ADOSqlServerRepository = Encina.ADO.SqlServer.Repository;
using DapperMySQLRepository = Encina.Dapper.MySQL.Repository;
using DapperOracleRepository = Encina.Dapper.Oracle.Repository;
using DapperPostgreSQLRepository = Encina.Dapper.PostgreSQL.Repository;
using DapperSqliteRepository = Encina.Dapper.Sqlite.Repository;
using DapperSqlServerRepository = Encina.Dapper.SqlServer.Repository;
using EfCoreRepository = Encina.EntityFrameworkCore.Repository;
using MongoDbRepository = Encina.MongoDB.Repository;

namespace Encina.ContractTests.Database.Repository;

/// <summary>
/// Contract tests verifying that all Repository implementations follow the same interface contracts.
/// These tests ensure behavioral and API consistency across all 12 database providers.
/// </summary>
[Trait("Category", "Contract")]
public sealed class RepositoryContractTests
{
    #region IEntityMapping Contract Tests

    [Fact]
    public void Contract_AllADOProviders_IEntityMapping_HaveIdenticalMembers()
    {
        // Contract: All ADO.NET providers must have identical IEntityMapping interface members
        var adoSqliteType = typeof(ADOSqliteRepository.IEntityMapping<,>);
        var adoSqlServerType = typeof(ADOSqlServerRepository.IEntityMapping<,>);
        var adoPostgresType = typeof(ADOPostgreSQLRepository.IEntityMapping<,>);
        var adoMySQLType = typeof(ADOMySQLRepository.IEntityMapping<,>);
        var adoOracleType = typeof(ADOOracleRepository.IEntityMapping<,>);

        var referenceMembers = GetInterfaceMembers(adoSqliteType);

        // Verify all ADO providers have the same members
        VerifyInterfaceMembersMatch(adoSqliteType, adoSqlServerType, "ADO.SqlServer");
        VerifyInterfaceMembersMatch(adoSqliteType, adoPostgresType, "ADO.PostgreSQL");
        VerifyInterfaceMembersMatch(adoSqliteType, adoMySQLType, "ADO.MySQL");
        VerifyInterfaceMembersMatch(adoSqliteType, adoOracleType, "ADO.Oracle");

        // Verify required properties exist
        referenceMembers.ShouldContain("TableName", "ADO.Sqlite.IEntityMapping must have TableName property");
        referenceMembers.ShouldContain("IdColumnName", "ADO.Sqlite.IEntityMapping must have IdColumnName property");
        referenceMembers.ShouldContain("ColumnMappings", "ADO.Sqlite.IEntityMapping must have ColumnMappings property");
        referenceMembers.ShouldContain("GetId", "ADO.Sqlite.IEntityMapping must have GetId method");
        referenceMembers.ShouldContain("InsertExcludedProperties", "ADO.Sqlite.IEntityMapping must have InsertExcludedProperties property");
        referenceMembers.ShouldContain("UpdateExcludedProperties", "ADO.Sqlite.IEntityMapping must have UpdateExcludedProperties property");
    }

    [Fact]
    public void Contract_AllDapperProviders_IEntityMapping_HaveIdenticalMembers()
    {
        // Contract: All Dapper providers must have identical IEntityMapping interface members
        var dapperSqliteType = typeof(DapperSqliteRepository.IEntityMapping<,>);
        var dapperSqlServerType = typeof(DapperSqlServerRepository.IEntityMapping<,>);
        var dapperPostgresType = typeof(DapperPostgreSQLRepository.IEntityMapping<,>);
        var dapperMySQLType = typeof(DapperMySQLRepository.IEntityMapping<,>);
        var dapperOracleType = typeof(DapperOracleRepository.IEntityMapping<,>);

        // Verify all Dapper providers have the same members
        VerifyInterfaceMembersMatch(dapperSqliteType, dapperSqlServerType, "Dapper.SqlServer");
        VerifyInterfaceMembersMatch(dapperSqliteType, dapperPostgresType, "Dapper.PostgreSQL");
        VerifyInterfaceMembersMatch(dapperSqliteType, dapperMySQLType, "Dapper.MySQL");
        VerifyInterfaceMembersMatch(dapperSqliteType, dapperOracleType, "Dapper.Oracle");
    }

    [Fact]
    public void Contract_ADOAndDapper_IEntityMapping_AreEquivalent()
    {
        // Contract: ADO and Dapper IEntityMapping interfaces must be equivalent
        var adoType = typeof(ADOSqliteRepository.IEntityMapping<,>);
        var dapperType = typeof(DapperSqliteRepository.IEntityMapping<,>);

        VerifyInterfaceMembersMatch(adoType, dapperType, "Dapper.Sqlite vs ADO.Sqlite");
    }

    #endregion

    #region EntityMappingBuilder Contract Tests

    [Fact]
    public void Contract_AllADOProviders_EntityMappingBuilder_HaveIdenticalMethods()
    {
        // Contract: All ADO.NET providers must have identical EntityMappingBuilder methods
        var adoSqliteType = typeof(ADOSqliteRepository.EntityMappingBuilder<,>);
        var adoSqlServerType = typeof(ADOSqlServerRepository.EntityMappingBuilder<,>);
        var adoPostgresType = typeof(ADOPostgreSQLRepository.EntityMappingBuilder<,>);
        var adoMySQLType = typeof(ADOMySQLRepository.EntityMappingBuilder<,>);
        var adoOracleType = typeof(ADOOracleRepository.EntityMappingBuilder<,>);

        // Verify all ADO providers have the same public methods
        VerifyPublicMethodsMatch(adoSqliteType, adoSqlServerType, "ADO.SqlServer");
        VerifyPublicMethodsMatch(adoSqliteType, adoPostgresType, "ADO.PostgreSQL");
        VerifyPublicMethodsMatch(adoSqliteType, adoMySQLType, "ADO.MySQL");
        VerifyPublicMethodsMatch(adoSqliteType, adoOracleType, "ADO.Oracle");

        // Verify required methods exist
        var methods = GetPublicMethods(adoSqliteType);
        methods.ShouldContain("ToTable", "EntityMappingBuilder must have ToTable method");
        methods.ShouldContain("HasId", "EntityMappingBuilder must have HasId method");
        methods.ShouldContain("MapProperty", "EntityMappingBuilder must have MapProperty method");
        methods.ShouldContain("ExcludeFromInsert", "EntityMappingBuilder must have ExcludeFromInsert method");
        methods.ShouldContain("ExcludeFromUpdate", "EntityMappingBuilder must have ExcludeFromUpdate method");
        methods.ShouldContain("Build", "EntityMappingBuilder must have Build method");
    }

    [Fact]
    public void Contract_AllDapperProviders_EntityMappingBuilder_HaveIdenticalMethods()
    {
        // Contract: All Dapper providers must have identical EntityMappingBuilder methods
        var dapperSqliteType = typeof(DapperSqliteRepository.EntityMappingBuilder<,>);
        var dapperSqlServerType = typeof(DapperSqlServerRepository.EntityMappingBuilder<,>);
        var dapperPostgresType = typeof(DapperPostgreSQLRepository.EntityMappingBuilder<,>);
        var dapperMySQLType = typeof(DapperMySQLRepository.EntityMappingBuilder<,>);
        var dapperOracleType = typeof(DapperOracleRepository.EntityMappingBuilder<,>);

        // Verify all Dapper providers have the same public methods
        VerifyPublicMethodsMatch(dapperSqliteType, dapperSqlServerType, "Dapper.SqlServer");
        VerifyPublicMethodsMatch(dapperSqliteType, dapperPostgresType, "Dapper.PostgreSQL");
        VerifyPublicMethodsMatch(dapperSqliteType, dapperMySQLType, "Dapper.MySQL");
        VerifyPublicMethodsMatch(dapperSqliteType, dapperOracleType, "Dapper.Oracle");
    }

    [Fact]
    public void Contract_ADOAndDapper_EntityMappingBuilder_AreEquivalent()
    {
        // Contract: ADO and Dapper EntityMappingBuilder classes must have equivalent APIs
        var adoType = typeof(ADOSqliteRepository.EntityMappingBuilder<,>);
        var dapperType = typeof(DapperSqliteRepository.EntityMappingBuilder<,>);

        VerifyPublicMethodsMatch(adoType, dapperType, "Dapper.Sqlite vs ADO.Sqlite");
    }

    [Fact]
    public void Contract_AllProviders_EntityMappingBuilder_ToTable_ReturnsSelf()
    {
        // Contract: ToTable must return the builder instance for fluent chaining
        var adoSqlite = new ADOSqliteRepository.EntityMappingBuilder<ContractTestEntity, Guid>();
        var adoSqlServer = new ADOSqlServerRepository.EntityMappingBuilder<ContractTestEntity, Guid>();
        var adoPostgres = new ADOPostgreSQLRepository.EntityMappingBuilder<ContractTestEntity, Guid>();
        var adoMySQL = new ADOMySQLRepository.EntityMappingBuilder<ContractTestEntity, Guid>();
        var adoOracle = new ADOOracleRepository.EntityMappingBuilder<ContractTestEntity, Guid>();

        var dapperSqlite = new DapperSqliteRepository.EntityMappingBuilder<ContractTestEntity, Guid>();
        var dapperSqlServer = new DapperSqlServerRepository.EntityMappingBuilder<ContractTestEntity, Guid>();
        var dapperPostgres = new DapperPostgreSQLRepository.EntityMappingBuilder<ContractTestEntity, Guid>();
        var dapperMySQL = new DapperMySQLRepository.EntityMappingBuilder<ContractTestEntity, Guid>();
        var dapperOracle = new DapperOracleRepository.EntityMappingBuilder<ContractTestEntity, Guid>();

        // All ToTable calls must return the same instance
        adoSqlite.ToTable("Entities").ShouldBe(adoSqlite, "ADO.Sqlite ToTable must return self");
        adoSqlServer.ToTable("Entities").ShouldBe(adoSqlServer, "ADO.SqlServer ToTable must return self");
        adoPostgres.ToTable("Entities").ShouldBe(adoPostgres, "ADO.PostgreSQL ToTable must return self");
        adoMySQL.ToTable("Entities").ShouldBe(adoMySQL, "ADO.MySQL ToTable must return self");
        adoOracle.ToTable("Entities").ShouldBe(adoOracle, "ADO.Oracle ToTable must return self");

        dapperSqlite.ToTable("Entities").ShouldBe(dapperSqlite, "Dapper.Sqlite ToTable must return self");
        dapperSqlServer.ToTable("Entities").ShouldBe(dapperSqlServer, "Dapper.SqlServer ToTable must return self");
        dapperPostgres.ToTable("Entities").ShouldBe(dapperPostgres, "Dapper.PostgreSQL ToTable must return self");
        dapperMySQL.ToTable("Entities").ShouldBe(dapperMySQL, "Dapper.MySQL ToTable must return self");
        dapperOracle.ToTable("Entities").ShouldBe(dapperOracle, "Dapper.Oracle ToTable must return self");
    }

    [Fact]
    public void Contract_AllProviders_EntityMappingBuilder_Build_RequiresTableName()
    {
        // Contract: Build without ToTable must throw InvalidOperationException
        var adoSqlite = new ADOSqliteRepository.EntityMappingBuilder<ContractTestEntity, Guid>();
        var dapperSqlite = new DapperSqliteRepository.EntityMappingBuilder<ContractTestEntity, Guid>();

        var adoException = Should.Throw<InvalidOperationException>(() =>
            adoSqlite.HasId(e => e.Id).Build());
        adoException.Message.ShouldContain("Table");

        var dapperException = Should.Throw<InvalidOperationException>(() =>
            dapperSqlite.HasId(e => e.Id).Build());
        dapperException.Message.ShouldContain("Table");
    }

    [Fact]
    public void Contract_AllProviders_EntityMappingBuilder_Build_RequiresPrimaryKey()
    {
        // Contract: Build without HasId must throw InvalidOperationException
        var adoSqlite = new ADOSqliteRepository.EntityMappingBuilder<ContractTestEntity, Guid>();
        var dapperSqlite = new DapperSqliteRepository.EntityMappingBuilder<ContractTestEntity, Guid>();

        var adoException = Should.Throw<InvalidOperationException>(() =>
            adoSqlite.ToTable("Entities").Build());
        adoException.Message.ShouldContain("Primary key");

        var dapperException = Should.Throw<InvalidOperationException>(() =>
            dapperSqlite.ToTable("Entities").Build());
        dapperException.Message.ShouldContain("Primary key");
    }

    [Fact]
    public void Contract_AllProviders_EntityMappingBuilder_Build_ProducesValidMapping()
    {
        // Contract: Build with valid configuration must produce a working mapping
        var adoSqliteMapping = new ADOSqliteRepository.EntityMappingBuilder<ContractTestEntity, Guid>()
            .ToTable("Entities").HasId(e => e.Id).MapProperty(e => e.Name).Build();

        var dapperSqliteMapping = new DapperSqliteRepository.EntityMappingBuilder<ContractTestEntity, Guid>()
            .ToTable("Entities").HasId(e => e.Id).MapProperty(e => e.Name).Build();

        var adoPostgresMapping = new ADOPostgreSQLRepository.EntityMappingBuilder<ContractTestEntity, Guid>()
            .ToTable("Entities").HasId(e => e.Id).MapProperty(e => e.Name).Build();

        var dapperPostgresMapping = new DapperPostgreSQLRepository.EntityMappingBuilder<ContractTestEntity, Guid>()
            .ToTable("Entities").HasId(e => e.Id).MapProperty(e => e.Name).Build();

        // Verify consistent behavior
        adoSqliteMapping.TableName.ShouldBe("Entities", "ADO.Sqlite");
        dapperSqliteMapping.TableName.ShouldBe("Entities", "Dapper.Sqlite");
        adoPostgresMapping.TableName.ShouldBe("Entities", "ADO.PostgreSQL");
        dapperPostgresMapping.TableName.ShouldBe("Entities", "Dapper.PostgreSQL");

        adoSqliteMapping.IdColumnName.ShouldBe("Id", "ADO.Sqlite");
        dapperSqliteMapping.IdColumnName.ShouldBe("Id", "Dapper.Sqlite");
        adoPostgresMapping.IdColumnName.ShouldBe("Id", "ADO.PostgreSQL");
        dapperPostgresMapping.IdColumnName.ShouldBe("Id", "Dapper.PostgreSQL");
    }

    [Fact]
    public void Contract_AllProviders_EntityMappingBuilder_GetId_ExtractsIdCorrectly()
    {
        // Contract: GetId must correctly extract the ID from an entity
        var testEntity = new ContractTestEntity
        {
            Id = Guid.NewGuid(),
            Name = "Test"
        };

        var adoSqliteMapping = new ADOSqliteRepository.EntityMappingBuilder<ContractTestEntity, Guid>()
            .ToTable("Entities").HasId(e => e.Id).MapProperty(e => e.Name).Build();

        var dapperSqliteMapping = new DapperSqliteRepository.EntityMappingBuilder<ContractTestEntity, Guid>()
            .ToTable("Entities").HasId(e => e.Id).MapProperty(e => e.Name).Build();

        adoSqliteMapping.GetId(testEntity).ShouldBe(testEntity.Id, "ADO.Sqlite");
        dapperSqliteMapping.GetId(testEntity).ShouldBe(testEntity.Id, "Dapper.Sqlite");
    }

    [Fact]
    public void Contract_AllProviders_EntityMappingBuilder_IdIsExcludedFromUpdates()
    {
        // Contract: HasId must automatically exclude ID from updates
        var adoSqliteMapping = new ADOSqliteRepository.EntityMappingBuilder<ContractTestEntity, Guid>()
            .ToTable("Entities").HasId(e => e.Id).MapProperty(e => e.Name).Build();

        var dapperSqliteMapping = new DapperSqliteRepository.EntityMappingBuilder<ContractTestEntity, Guid>()
            .ToTable("Entities").HasId(e => e.Id).MapProperty(e => e.Name).Build();

        adoSqliteMapping.UpdateExcludedProperties.ShouldContain("Id", "ADO.Sqlite must exclude Id from updates");
        dapperSqliteMapping.UpdateExcludedProperties.ShouldContain("Id", "Dapper.Sqlite must exclude Id from updates");
    }

    [Fact]
    public void Contract_AllProviders_EntityMappingBuilder_ExcludeFromInsert_Works()
    {
        // Contract: ExcludeFromInsert must work consistently
        var adoSqliteMapping = new ADOSqliteRepository.EntityMappingBuilder<ContractTestEntity, Guid>()
            .ToTable("Entities").HasId(e => e.Id).MapProperty(e => e.Name)
            .ExcludeFromInsert(e => e.Id).Build();

        var dapperSqliteMapping = new DapperSqliteRepository.EntityMappingBuilder<ContractTestEntity, Guid>()
            .ToTable("Entities").HasId(e => e.Id).MapProperty(e => e.Name)
            .ExcludeFromInsert(e => e.Id).Build();

        adoSqliteMapping.InsertExcludedProperties.ShouldContain("Id", "ADO.Sqlite");
        dapperSqliteMapping.InsertExcludedProperties.ShouldContain("Id", "Dapper.Sqlite");
    }

    [Fact]
    public void Contract_AllProviders_EntityMappingBuilder_ExcludeFromUpdate_Works()
    {
        // Contract: ExcludeFromUpdate must work consistently
        var adoSqliteMapping = new ADOSqliteRepository.EntityMappingBuilder<ContractTestEntity, Guid>()
            .ToTable("Entities").HasId(e => e.Id).MapProperty(e => e.Name)
            .ExcludeFromUpdate(e => e.Name).Build();

        var dapperSqliteMapping = new DapperSqliteRepository.EntityMappingBuilder<ContractTestEntity, Guid>()
            .ToTable("Entities").HasId(e => e.Id).MapProperty(e => e.Name)
            .ExcludeFromUpdate(e => e.Name).Build();

        adoSqliteMapping.UpdateExcludedProperties.ShouldContain("Name", "ADO.Sqlite");
        dapperSqliteMapping.UpdateExcludedProperties.ShouldContain("Name", "Dapper.Sqlite");
    }

    #endregion

    #region FunctionalRepository Contract Tests

    [Fact]
    public void Contract_AllProviders_FunctionalRepository_ImplementCorrectInterface()
    {
        // Contract: All FunctionalRepository implementations must implement IFunctionalRepository
        typeof(ADOSqliteRepository.FunctionalRepositoryADO<,>).GetInterfaces()
            .ShouldContain(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IFunctionalRepository<,>),
            "ADO.Sqlite FunctionalRepositoryADO must implement IFunctionalRepository<,>");

        typeof(ADOSqlServerRepository.FunctionalRepositoryADO<,>).GetInterfaces()
            .ShouldContain(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IFunctionalRepository<,>),
            "ADO.SqlServer FunctionalRepositoryADO must implement IFunctionalRepository<,>");

        typeof(ADOPostgreSQLRepository.FunctionalRepositoryADO<,>).GetInterfaces()
            .ShouldContain(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IFunctionalRepository<,>),
            "ADO.PostgreSQL FunctionalRepositoryADO must implement IFunctionalRepository<,>");

        typeof(ADOMySQLRepository.FunctionalRepositoryADO<,>).GetInterfaces()
            .ShouldContain(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IFunctionalRepository<,>),
            "ADO.MySQL FunctionalRepositoryADO must implement IFunctionalRepository<,>");

        typeof(ADOOracleRepository.FunctionalRepositoryADO<,>).GetInterfaces()
            .ShouldContain(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IFunctionalRepository<,>),
            "ADO.Oracle FunctionalRepositoryADO must implement IFunctionalRepository<,>");

        typeof(DapperSqliteRepository.FunctionalRepositoryDapper<,>).GetInterfaces()
            .ShouldContain(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IFunctionalRepository<,>),
            "Dapper.Sqlite FunctionalRepositoryDapper must implement IFunctionalRepository<,>");

        typeof(DapperSqlServerRepository.FunctionalRepositoryDapper<,>).GetInterfaces()
            .ShouldContain(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IFunctionalRepository<,>),
            "Dapper.SqlServer FunctionalRepositoryDapper must implement IFunctionalRepository<,>");

        typeof(DapperPostgreSQLRepository.FunctionalRepositoryDapper<,>).GetInterfaces()
            .ShouldContain(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IFunctionalRepository<,>),
            "Dapper.PostgreSQL FunctionalRepositoryDapper must implement IFunctionalRepository<,>");

        typeof(DapperMySQLRepository.FunctionalRepositoryDapper<,>).GetInterfaces()
            .ShouldContain(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IFunctionalRepository<,>),
            "Dapper.MySQL FunctionalRepositoryDapper must implement IFunctionalRepository<,>");

        typeof(DapperOracleRepository.FunctionalRepositoryDapper<,>).GetInterfaces()
            .ShouldContain(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IFunctionalRepository<,>),
            "Dapper.Oracle FunctionalRepositoryDapper must implement IFunctionalRepository<,>");

        typeof(EfCoreRepository.FunctionalRepositoryEF<,>).GetInterfaces()
            .ShouldContain(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IFunctionalRepository<,>),
            "EfCore FunctionalRepositoryEF must implement IFunctionalRepository<,>");

        typeof(MongoDbRepository.FunctionalRepositoryMongoDB<,>).GetInterfaces()
            .ShouldContain(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IFunctionalRepository<,>),
            "MongoDB FunctionalRepositoryMongoDB must implement IFunctionalRepository<,>");
    }

    [Fact]
    public void Contract_AllProviders_FunctionalRepository_HaveConsistentGenericConstraints()
    {
        // Contract: All FunctionalRepository implementations must have the same generic constraints
        var adoSqliteConstraints = GetGenericConstraints(typeof(ADOSqliteRepository.FunctionalRepositoryADO<,>));
        var adoSqlServerConstraints = GetGenericConstraints(typeof(ADOSqlServerRepository.FunctionalRepositoryADO<,>));
        var dapperSqliteConstraints = GetGenericConstraints(typeof(DapperSqliteRepository.FunctionalRepositoryDapper<,>));
        var efCoreConstraints = GetGenericConstraints(typeof(EfCoreRepository.FunctionalRepositoryEF<,>));
        var mongoDbConstraints = GetGenericConstraints(typeof(MongoDbRepository.FunctionalRepositoryMongoDB<,>));

        // TEntity constraints: class (and potentially new() for ADO/Dapper)
        adoSqliteConstraints.EntityConstraints.ShouldContain("class", "ADO.Sqlite TEntity must be class");
        adoSqlServerConstraints.EntityConstraints.ShouldContain("class", "ADO.SqlServer TEntity must be class");
        dapperSqliteConstraints.EntityConstraints.ShouldContain("class", "Dapper.Sqlite TEntity must be class");
        efCoreConstraints.EntityConstraints.ShouldContain("class", "EfCore TEntity must be class");
        mongoDbConstraints.EntityConstraints.ShouldContain("class", "MongoDB TEntity must be class");

        // TId constraints: notnull
        adoSqliteConstraints.IdConstraints.ShouldContain("notnull", "ADO.Sqlite TId must be notnull");
        dapperSqliteConstraints.IdConstraints.ShouldContain("notnull", "Dapper.Sqlite TId must be notnull");
        efCoreConstraints.IdConstraints.ShouldContain("notnull", "EfCore TId must be notnull");
        mongoDbConstraints.IdConstraints.ShouldContain("notnull", "MongoDB TId must be notnull");
    }

    #endregion

    #region SpecificationSqlBuilder Contract Tests

    [Fact]
    public void Contract_AllADOProviders_SpecificationSqlBuilder_HaveIdenticalMethods()
    {
        // Contract: All ADO.NET providers must have identical SpecificationSqlBuilder methods
        var adoSqliteType = typeof(ADOSqliteRepository.SpecificationSqlBuilder<>);
        var adoSqlServerType = typeof(ADOSqlServerRepository.SpecificationSqlBuilder<>);
        var adoPostgresType = typeof(ADOPostgreSQLRepository.SpecificationSqlBuilder<>);
        var adoMySQLType = typeof(ADOMySQLRepository.SpecificationSqlBuilder<>);
        var adoOracleType = typeof(ADOOracleRepository.SpecificationSqlBuilder<>);

        // Verify all ADO providers have the same public methods
        VerifyPublicMethodsMatch(adoSqliteType, adoSqlServerType, "ADO.SqlServer SpecificationSqlBuilder");
        VerifyPublicMethodsMatch(adoSqliteType, adoPostgresType, "ADO.PostgreSQL SpecificationSqlBuilder");
        VerifyPublicMethodsMatch(adoSqliteType, adoMySQLType, "ADO.MySQL SpecificationSqlBuilder");
        VerifyPublicMethodsMatch(adoSqliteType, adoOracleType, "ADO.Oracle SpecificationSqlBuilder");

        // Verify required methods exist
        var methods = GetPublicMethods(adoSqliteType);
        methods.ShouldContain("BuildWhereClause", "SpecificationSqlBuilder must have BuildWhereClause method");
        methods.ShouldContain("BuildOrderByClause", "SpecificationSqlBuilder must have BuildOrderByClause method");
        methods.ShouldContain("BuildPaginationClause", "SpecificationSqlBuilder must have BuildPaginationClause method");
        methods.ShouldContain("BuildSelectStatement", "SpecificationSqlBuilder must have BuildSelectStatement method");
    }

    [Fact]
    public void Contract_AllDapperProviders_SpecificationSqlBuilder_HaveIdenticalMethods()
    {
        // Contract: All Dapper providers must have identical SpecificationSqlBuilder methods
        var dapperSqliteType = typeof(DapperSqliteRepository.SpecificationSqlBuilder<>);
        var dapperSqlServerType = typeof(DapperSqlServerRepository.SpecificationSqlBuilder<>);
        var dapperPostgresType = typeof(DapperPostgreSQLRepository.SpecificationSqlBuilder<>);
        var dapperMySQLType = typeof(DapperMySQLRepository.SpecificationSqlBuilder<>);
        var dapperOracleType = typeof(DapperOracleRepository.SpecificationSqlBuilder<>);

        // Verify all Dapper providers have the same public methods
        VerifyPublicMethodsMatch(dapperSqliteType, dapperSqlServerType, "Dapper.SqlServer SpecificationSqlBuilder");
        VerifyPublicMethodsMatch(dapperSqliteType, dapperPostgresType, "Dapper.PostgreSQL SpecificationSqlBuilder");
        VerifyPublicMethodsMatch(dapperSqliteType, dapperMySQLType, "Dapper.MySQL SpecificationSqlBuilder");
        VerifyPublicMethodsMatch(dapperSqliteType, dapperOracleType, "Dapper.Oracle SpecificationSqlBuilder");
    }

    [Fact]
    public void Contract_ADOAndDapper_SpecificationSqlBuilder_AreEquivalent()
    {
        // Contract: ADO and Dapper SpecificationSqlBuilder classes must have equivalent APIs
        var adoType = typeof(ADOSqliteRepository.SpecificationSqlBuilder<>);
        var dapperType = typeof(DapperSqliteRepository.SpecificationSqlBuilder<>);

        VerifyPublicMethodsMatch(adoType, dapperType, "Dapper.Sqlite vs ADO.Sqlite SpecificationSqlBuilder");
    }

    #endregion

    #region EF Core SpecificationEvaluator Contract Tests

    [Fact]
    public void Contract_EfCore_SpecificationEvaluator_HasRequiredMethods()
    {
        // Contract: EF Core SpecificationEvaluator must have required static methods
        var evaluatorType = typeof(EfCoreRepository.SpecificationEvaluator);

        var methods = evaluatorType.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Select(m => m.Name)
            .ToHashSet();

        methods.ShouldContain("GetQuery", "EF Core SpecificationEvaluator must have GetQuery method");
    }

    [Fact]
    public void Contract_EfCore_SpecificationEvaluatorEF_ImplementsInterface()
    {
        // Contract: EF Core SpecificationEvaluatorEF must implement ISpecificationEvaluator
        typeof(EfCoreRepository.SpecificationEvaluatorEF<>).GetInterfaces()
            .ShouldContain(i => i.IsGenericType && i.GetGenericTypeDefinition().Name.Contains("ISpecificationEvaluator"),
            "EfCore SpecificationEvaluatorEF must implement ISpecificationEvaluator");
    }

    #endregion

    #region MongoDB SpecificationFilterBuilder Contract Tests

    [Fact]
    public void Contract_MongoDB_SpecificationFilterBuilder_HasRequiredMethods()
    {
        // Contract: MongoDB SpecificationFilterBuilder must have required methods
        var builderType = typeof(MongoDbRepository.SpecificationFilterBuilder<>);

        var methods = builderType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => !m.IsSpecialName)
            .Select(m => m.Name)
            .ToHashSet();

        methods.ShouldContain("BuildFilter", "MongoDB SpecificationFilterBuilder must have BuildFilter method");
    }

    #endregion

    #region Naming Convention Contract Tests

    [Fact]
    public void Contract_AllADOProviders_UseConsistentNaming()
    {
        // Contract: All ADO.NET providers must use consistent class naming
        var providers = new[]
        {
            (typeof(ADOSqliteRepository.FunctionalRepositoryADO<,>).Assembly, "ADO.Sqlite"),
            (typeof(ADOSqlServerRepository.FunctionalRepositoryADO<,>).Assembly, "ADO.SqlServer"),
            (typeof(ADOPostgreSQLRepository.FunctionalRepositoryADO<,>).Assembly, "ADO.PostgreSQL"),
            (typeof(ADOMySQLRepository.FunctionalRepositoryADO<,>).Assembly, "ADO.MySQL"),
            (typeof(ADOOracleRepository.FunctionalRepositoryADO<,>).Assembly, "ADO.Oracle"),
        };

        foreach (var (assembly, providerName) in providers)
        {
            var repositoryNs = assembly.GetTypes()
                .Where(t => t.Namespace?.EndsWith(".Repository", StringComparison.Ordinal) == true)
                .ToList();

            repositoryNs.ShouldContain(t => t.Name.StartsWith("FunctionalRepositoryADO"),
                $"{providerName} must have FunctionalRepositoryADO class");
            repositoryNs.ShouldContain(t => t.Name.StartsWith("EntityMappingBuilder"),
                $"{providerName} must have EntityMappingBuilder class");
            repositoryNs.ShouldContain(t => t.Name.StartsWith("SpecificationSqlBuilder"),
                $"{providerName} must have SpecificationSqlBuilder class");
            repositoryNs.ShouldContain(t => t.Name == "IEntityMapping`2",
                $"{providerName} must have IEntityMapping interface");
        }
    }

    [Fact]
    public void Contract_AllDapperProviders_UseConsistentNaming()
    {
        // Contract: All Dapper providers must use consistent class naming
        var providers = new[]
        {
            (typeof(DapperSqliteRepository.FunctionalRepositoryDapper<,>).Assembly, "Dapper.Sqlite"),
            (typeof(DapperSqlServerRepository.FunctionalRepositoryDapper<,>).Assembly, "Dapper.SqlServer"),
            (typeof(DapperPostgreSQLRepository.FunctionalRepositoryDapper<,>).Assembly, "Dapper.PostgreSQL"),
            (typeof(DapperMySQLRepository.FunctionalRepositoryDapper<,>).Assembly, "Dapper.MySQL"),
            (typeof(DapperOracleRepository.FunctionalRepositoryDapper<,>).Assembly, "Dapper.Oracle"),
        };

        foreach (var (assembly, providerName) in providers)
        {
            var repositoryNs = assembly.GetTypes()
                .Where(t => t.Namespace?.EndsWith(".Repository", StringComparison.Ordinal) == true)
                .ToList();

            repositoryNs.ShouldContain(t => t.Name.StartsWith("FunctionalRepositoryDapper"),
                $"{providerName} must have FunctionalRepositoryDapper class");
            repositoryNs.ShouldContain(t => t.Name.StartsWith("EntityMappingBuilder"),
                $"{providerName} must have EntityMappingBuilder class");
            repositoryNs.ShouldContain(t => t.Name.StartsWith("SpecificationSqlBuilder"),
                $"{providerName} must have SpecificationSqlBuilder class");
            repositoryNs.ShouldContain(t => t.Name == "IEntityMapping`2",
                $"{providerName} must have IEntityMapping interface");
        }
    }

    [Fact]
    public void Contract_EfCore_UsesConsistentNaming()
    {
        // Contract: EF Core must use consistent class naming
        var assembly = typeof(EfCoreRepository.FunctionalRepositoryEF<,>).Assembly;
        var repositoryNs = assembly.GetTypes()
            .Where(t => t.Namespace?.EndsWith(".Repository", StringComparison.Ordinal) == true)
            .ToList();

        repositoryNs.ShouldContain(t => t.Name.StartsWith("FunctionalRepositoryEF"),
            "EfCore must have FunctionalRepositoryEF class");
        repositoryNs.ShouldContain(t => t.Name.StartsWith("SpecificationEvaluator"),
            "EfCore must have SpecificationEvaluator class");
    }

    [Fact]
    public void Contract_MongoDB_UsesConsistentNaming()
    {
        // Contract: MongoDB must use consistent class naming
        var assembly = typeof(MongoDbRepository.FunctionalRepositoryMongoDB<,>).Assembly;
        var repositoryNs = assembly.GetTypes()
            .Where(t => t.Namespace?.EndsWith(".Repository", StringComparison.Ordinal) == true)
            .ToList();

        repositoryNs.ShouldContain(t => t.Name.StartsWith("FunctionalRepositoryMongoDB"),
            "MongoDB must have FunctionalRepositoryMongoDB class");
        repositoryNs.ShouldContain(t => t.Name.StartsWith("SpecificationFilterBuilder"),
            "MongoDB must have SpecificationFilterBuilder class");
    }

    #endregion

    #region Helper Methods

    private static HashSet<string> GetInterfaceMembers(Type interfaceType)
    {
        return interfaceType.GetMembers(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => !m.Name.StartsWith("get_", StringComparison.Ordinal) && !m.Name.StartsWith("set_", StringComparison.Ordinal))
            .Select(m => m.Name)
            .ToHashSet();
    }

    private static void VerifyInterfaceMembersMatch(Type referenceType, Type comparisonType, string comparisonName)
    {
        var referenceMembers = GetInterfaceMembers(referenceType);
        var comparisonMembers = GetInterfaceMembers(comparisonType);

        foreach (var member in referenceMembers)
        {
            comparisonMembers.ShouldContain(member,
                $"{comparisonName} IEntityMapping is missing member '{member}' present in reference type");
        }

        foreach (var member in comparisonMembers)
        {
            referenceMembers.ShouldContain(member,
                $"{comparisonName} IEntityMapping has extra member '{member}' not in reference type");
        }
    }

    private static HashSet<string> GetPublicMethods(Type type)
    {
        return type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => !m.IsSpecialName)
            .Select(m => m.Name)
            .ToHashSet();
    }

    private static void VerifyPublicMethodsMatch(Type referenceType, Type comparisonType, string comparisonName)
    {
        var referenceMethods = GetPublicMethods(referenceType);
        var comparisonMethods = GetPublicMethods(comparisonType);

        foreach (var method in referenceMethods)
        {
            comparisonMethods.ShouldContain(method,
                $"{comparisonName} is missing method '{method}' present in reference type");
        }

        foreach (var method in comparisonMethods)
        {
            referenceMethods.ShouldContain(method,
                $"{comparisonName} has extra method '{method}' not in reference type");
        }
    }

    private static (HashSet<string> EntityConstraints, HashSet<string> IdConstraints) GetGenericConstraints(Type type)
    {
        var genericArgs = type.GetGenericArguments();
        var entityConstraints = new HashSet<string>();
        var idConstraints = new HashSet<string>();

        if (genericArgs.Length >= 1)
        {
            var entityArg = genericArgs[0];
            if (entityArg.GenericParameterAttributes.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint))
                entityConstraints.Add("class");
            if (entityArg.GenericParameterAttributes.HasFlag(GenericParameterAttributes.DefaultConstructorConstraint))
                entityConstraints.Add("new()");
            if (entityArg.GenericParameterAttributes.HasFlag(GenericParameterAttributes.NotNullableValueTypeConstraint))
                entityConstraints.Add("struct");
        }

        if (genericArgs.Length >= 2)
        {
            var idArg = genericArgs[1];
            if (idArg.GenericParameterAttributes.HasFlag(GenericParameterAttributes.NotNullableValueTypeConstraint))
                idConstraints.Add("struct");
            // Check for notnull constraint (available in .NET 6+)
            var constraints = idArg.GetGenericParameterConstraints();
            if (!idArg.GenericParameterAttributes.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint) &&
                !idArg.GenericParameterAttributes.HasFlag(GenericParameterAttributes.NotNullableValueTypeConstraint))
            {
                // This is likely a notnull constraint if it has no other constraints
                idConstraints.Add("notnull");
            }
        }

        return (entityConstraints, idConstraints);
    }

    #endregion
}

/// <summary>
/// Test entity for contract tests.
/// </summary>
public sealed class ContractTestEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
