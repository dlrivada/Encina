using Encina.TestInfrastructure.Fixtures;

namespace Encina.Dapper.Sqlite.Tests;

/// <summary>
/// Collection definition for tests that share a <see cref="SqliteFixture"/>.
/// Tests in this collection will not run in parallel with each other.
/// </summary>
[CollectionDefinition("Database")]
public sealed class DatabaseTestFixtures : ICollectionFixture<SqliteFixture>;
