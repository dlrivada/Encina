using System.Diagnostics.CodeAnalysis;

using Encina.TestInfrastructure.Fixtures.Sharding;

using Xunit;

namespace Encina.IntegrationTests.Sharding;

/// <summary>
/// xUnit collection definitions for sharding integration tests.
/// Each collection shares a single sharding fixture (3 shard databases per container).
/// </summary>

#region ADO.NET Sharding Collections

/// <summary>
/// Collection for ADO.NET SQL Server sharding integration tests.
/// </summary>
[CollectionDefinition("Sharding-ADO-SqlServer")]
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "xUnit requires collection types to end with 'Collection'")]
public class ShardingAdoSqlServerCollection : ICollectionFixture<ShardedSqlServerFixture>
{
}

/// <summary>
/// Collection for ADO.NET PostgreSQL sharding integration tests.
/// </summary>
[CollectionDefinition("Sharding-ADO-PostgreSQL")]
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "xUnit requires collection types to end with 'Collection'")]
public class ShardingAdoPostgreSqlCollection : ICollectionFixture<ShardedPostgreSqlFixture>
{
}

/// <summary>
/// Collection for ADO.NET MySQL sharding integration tests.
/// </summary>
[CollectionDefinition("Sharding-ADO-MySQL")]
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "xUnit requires collection types to end with 'Collection'")]
public class ShardingAdoMySqlCollection : ICollectionFixture<ShardedMySqlFixture>
{
}

/// <summary>
/// Collection for ADO.NET SQLite sharding integration tests.
/// </summary>
[CollectionDefinition("Sharding-ADO-Sqlite", DisableParallelization = true)]
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "xUnit requires collection types to end with 'Collection'")]
public class ShardingAdoSqliteCollection : ICollectionFixture<ShardedSqliteFixture>
{
}

#endregion

#region Dapper Sharding Collections

/// <summary>
/// Collection for Dapper SQL Server sharding integration tests.
/// </summary>
[CollectionDefinition("Sharding-Dapper-SqlServer")]
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "xUnit requires collection types to end with 'Collection'")]
public class ShardingDapperSqlServerCollection : ICollectionFixture<ShardedSqlServerFixture>
{
}

/// <summary>
/// Collection for Dapper PostgreSQL sharding integration tests.
/// </summary>
[CollectionDefinition("Sharding-Dapper-PostgreSQL")]
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "xUnit requires collection types to end with 'Collection'")]
public class ShardingDapperPostgreSqlCollection : ICollectionFixture<ShardedPostgreSqlFixture>
{
}

/// <summary>
/// Collection for Dapper MySQL sharding integration tests.
/// </summary>
[CollectionDefinition("Sharding-Dapper-MySQL")]
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "xUnit requires collection types to end with 'Collection'")]
public class ShardingDapperMySqlCollection : ICollectionFixture<ShardedMySqlFixture>
{
}

/// <summary>
/// Collection for Dapper SQLite sharding integration tests.
/// </summary>
[CollectionDefinition("Sharding-Dapper-Sqlite", DisableParallelization = true)]
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "xUnit requires collection types to end with 'Collection'")]
public class ShardingDapperSqliteCollection : ICollectionFixture<ShardedSqliteFixture>
{
}

#endregion

#region EF Core Sharding Collections

/// <summary>
/// Collection for EF Core SQL Server sharding integration tests.
/// </summary>
[CollectionDefinition("Sharding-EFCore-SqlServer")]
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "xUnit requires collection types to end with 'Collection'")]
public class ShardingEFCoreSqlServerCollection : ICollectionFixture<ShardedSqlServerFixture>
{
}

/// <summary>
/// Collection for EF Core PostgreSQL sharding integration tests.
/// </summary>
[CollectionDefinition("Sharding-EFCore-PostgreSQL")]
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "xUnit requires collection types to end with 'Collection'")]
public class ShardingEFCorePostgreSqlCollection : ICollectionFixture<ShardedPostgreSqlFixture>
{
}

/// <summary>
/// Collection for EF Core MySQL sharding integration tests.
/// </summary>
[CollectionDefinition("Sharding-EFCore-MySQL")]
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "xUnit requires collection types to end with 'Collection'")]
public class ShardingEFCoreMySqlCollection : ICollectionFixture<ShardedMySqlFixture>
{
}

/// <summary>
/// Collection for EF Core SQLite sharding integration tests.
/// </summary>
[CollectionDefinition("Sharding-EFCore-Sqlite", DisableParallelization = true)]
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "xUnit requires collection types to end with 'Collection'")]
public class ShardingEFCoreSqliteCollection : ICollectionFixture<ShardedSqliteFixture>
{
}

#endregion

#region MongoDB Sharding Collection

/// <summary>
/// Collection for MongoDB sharding integration tests.
/// </summary>
[CollectionDefinition("Sharding-MongoDB")]
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "xUnit requires collection types to end with 'Collection'")]
public class ShardingMongoDbCollection : ICollectionFixture<ShardedMongoDbFixture>
{
}

#endregion
