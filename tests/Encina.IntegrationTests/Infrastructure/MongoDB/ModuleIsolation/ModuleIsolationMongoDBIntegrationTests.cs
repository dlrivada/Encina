using Encina.Modules.Isolation;
using Encina.MongoDB;
using Encina.MongoDB.Modules;
using Encina.TestInfrastructure.Fixtures;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Shouldly;
using Xunit;

namespace Encina.IntegrationTests.Infrastructure.MongoDB.ModuleIsolation;

/// <summary>
/// Integration tests for MongoDB module isolation via <see cref="ModuleAwareMongoCollectionFactory"/>.
/// </summary>
/// <remarks>
/// <para>
/// Tests verify that <see cref="ModuleAwareMongoCollectionFactory"/> correctly routes collections
/// to module-specific databases based on the current module execution context.
/// </para>
/// <para>
/// MongoDB module isolation uses a database-per-module strategy rather than schemas,
/// which is different from SQL databases that use schema-based isolation.
/// </para>
/// </remarks>
[Collection(ModuleIsolationMongoDbCollection.Name)]
[Trait("Category", "Integration")]
[Trait("Database", "MongoDB")]
public sealed class ModuleIsolationMongoDBIntegrationTests : IAsyncLifetime
{
    private readonly MongoDbFixture _fixture;
    private readonly ModuleExecutionContext _moduleContext;
    private readonly IOptions<EncinaMongoDbOptions> _mongoOptions;
    private readonly IOptions<MongoDbModuleIsolationOptions> _isolationOptions;

    private const string BaseDatabaseName = "encina_module_test";
    private const string CollectionName = "test_entities";

    public ModuleIsolationMongoDBIntegrationTests(MongoDbFixture fixture)
    {
        _fixture = fixture;
        _moduleContext = new ModuleExecutionContext();
        _mongoOptions = Options.Create(new EncinaMongoDbOptions
        {
            DatabaseName = BaseDatabaseName
        });
        _isolationOptions = Options.Create(new MongoDbModuleIsolationOptions
        {
            EnableDatabasePerModule = true,
            DatabaseNamePattern = "{baseName}_{moduleName}",
            ThrowOnMissingModuleContext = false,
            LogWarningOnFallback = false
        });
    }

    public async Task InitializeAsync()
    {
        if (_fixture.IsAvailable)
        {
            // Clean up test databases
            await CleanupDatabaseAsync(BaseDatabaseName);
            await CleanupDatabaseAsync($"{BaseDatabaseName}_orders");
            await CleanupDatabaseAsync($"{BaseDatabaseName}_inventory");
            await CleanupDatabaseAsync($"{BaseDatabaseName}_shared");
        }
    }

    public Task DisposeAsync() => Task.CompletedTask;

    #region Database Routing Tests

    [SkippableFact]
    public async Task GetCollectionAsync_WithModuleContext_ShouldRouteToModuleDatabase()
    {
        Skip.IfNot(_fixture.IsAvailable);

        // Arrange
        var factory = CreateFactory();

        // Act
        using (_moduleContext.CreateScope("Orders"))
        {
            var collection = await factory.GetCollectionAsync<BsonDocument>(CollectionName);

            // Assert
            collection.CollectionNamespace.DatabaseNamespace.DatabaseName
                .ShouldBe($"{BaseDatabaseName}_orders");
        }
    }

    [SkippableFact]
    public async Task GetCollectionAsync_WithDifferentModules_ShouldRouteToDifferentDatabases()
    {
        Skip.IfNot(_fixture.IsAvailable);

        // Arrange
        var factory = CreateFactory();

        // Act & Assert - Orders module
        using (_moduleContext.CreateScope("Orders"))
        {
            var ordersCollection = await factory.GetCollectionAsync<BsonDocument>(CollectionName);
            ordersCollection.CollectionNamespace.DatabaseNamespace.DatabaseName
                .ShouldBe($"{BaseDatabaseName}_orders");
        }

        // Act & Assert - Inventory module
        using (_moduleContext.CreateScope("Inventory"))
        {
            var inventoryCollection = await factory.GetCollectionAsync<BsonDocument>(CollectionName);
            inventoryCollection.CollectionNamespace.DatabaseNamespace.DatabaseName
                .ShouldBe($"{BaseDatabaseName}_inventory");
        }
    }

    [SkippableFact]
    public async Task GetCollectionAsync_WithoutModuleContext_ShouldRouteToBaseDatabase()
    {
        Skip.IfNot(_fixture.IsAvailable);

        // Arrange
        var factory = CreateFactory();

        // Act (no module context set)
        var collection = await factory.GetCollectionAsync<BsonDocument>(CollectionName);

        // Assert
        collection.CollectionNamespace.DatabaseNamespace.DatabaseName
            .ShouldBe(BaseDatabaseName);
    }

    [SkippableFact]
    public async Task GetCollectionAsync_WithThrowOnMissingContext_ShouldThrowWhenNoModuleSet()
    {
        Skip.IfNot(_fixture.IsAvailable);

        // Arrange
        var isolationOptions = Options.Create(new MongoDbModuleIsolationOptions
        {
            EnableDatabasePerModule = true,
            ThrowOnMissingModuleContext = true
        });

        var factory = new ModuleAwareMongoCollectionFactory(
            _fixture.Client!,
            _moduleContext,
            _mongoOptions,
            isolationOptions,
            NullLogger<ModuleAwareMongoCollectionFactory>.Instance);

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(async () =>
            await factory.GetCollectionAsync<BsonDocument>(CollectionName));

        exception.Message.ShouldContain("No module context is available");
    }

    [SkippableFact]
    public async Task GetCollectionForModuleAsync_ShouldRouteToSpecificModuleDatabase()
    {
        Skip.IfNot(_fixture.IsAvailable);

        // Arrange
        var factory = CreateFactory();

        // Act - Explicitly request Inventory module's collection
        var collection = await factory.GetCollectionForModuleAsync<BsonDocument>(CollectionName, "Inventory");

        // Assert
        collection.CollectionNamespace.DatabaseNamespace.DatabaseName
            .ShouldBe($"{BaseDatabaseName}_inventory");
    }

    [SkippableFact]
    public async Task GetDatabaseNameAsync_WithModuleContext_ShouldReturnModuleDatabaseName()
    {
        Skip.IfNot(_fixture.IsAvailable);

        // Arrange
        var factory = CreateFactory();

        // Act
        using (_moduleContext.CreateScope("Orders"))
        {
            var dbName = await factory.GetDatabaseNameAsync();

            // Assert
            dbName.ShouldBe($"{BaseDatabaseName}_orders");
        }
    }

    [SkippableFact]
    public void GetDatabaseNameForModule_ShouldReturnCorrectDatabaseName()
    {
        Skip.IfNot(_fixture.IsAvailable);

        // Arrange
        var factory = CreateFactory();

        // Act
        var ordersDbName = factory.GetDatabaseNameForModule("Orders");
        var inventoryDbName = factory.GetDatabaseNameForModule("Inventory");

        // Assert
        ordersDbName.ShouldBe($"{BaseDatabaseName}_orders");
        inventoryDbName.ShouldBe($"{BaseDatabaseName}_inventory");
    }

    [SkippableFact]
    public async Task GetCollectionAsync_WithDatabasePerModuleDisabled_ShouldAlwaysUseBaseDatabase()
    {
        Skip.IfNot(_fixture.IsAvailable);

        // Arrange
        var isolationOptions = Options.Create(new MongoDbModuleIsolationOptions
        {
            EnableDatabasePerModule = false
        });

        var factory = new ModuleAwareMongoCollectionFactory(
            _fixture.Client!,
            _moduleContext,
            _mongoOptions,
            isolationOptions,
            NullLogger<ModuleAwareMongoCollectionFactory>.Instance);

        // Act
        using (_moduleContext.CreateScope("Orders"))
        {
            var collection = await factory.GetCollectionAsync<BsonDocument>(CollectionName);

            // Assert - Should use base database even with module context
            collection.CollectionNamespace.DatabaseNamespace.DatabaseName
                .ShouldBe(BaseDatabaseName);
        }
    }

    #endregion

    #region Cross-Module Access Tests

    [SkippableFact]
    public async Task WriteAndRead_InSameModule_ShouldSucceed()
    {
        Skip.IfNot(_fixture.IsAvailable);

        // Arrange
        var factory = CreateFactory();
        var testDocument = new BsonDocument
        {
            ["_id"] = Guid.NewGuid().ToString(),
            ["name"] = "Test Entity",
            ["value"] = 42
        };

        // Act - Write in Orders module
        using (_moduleContext.CreateScope("Orders"))
        {
            var collection = await factory.GetCollectionAsync<BsonDocument>(CollectionName);
            await collection.InsertOneAsync(testDocument);
        }

        // Assert - Read from same module
        using (_moduleContext.CreateScope("Orders"))
        {
            var collection = await factory.GetCollectionAsync<BsonDocument>(CollectionName);
            var found = await collection.Find(d => d["_id"] == testDocument["_id"]).FirstOrDefaultAsync();
            found.ShouldNotBeNull();
            found["name"].AsString.ShouldBe("Test Entity");
        }
    }

    [SkippableFact]
    public async Task WriteInOneModule_ReadFromAnotherModule_ShouldNotFindDocument()
    {
        Skip.IfNot(_fixture.IsAvailable);

        // Arrange
        var factory = CreateFactory();
        var testDocument = new BsonDocument
        {
            ["_id"] = Guid.NewGuid().ToString(),
            ["name"] = "Orders Only Entity",
            ["value"] = 100
        };

        // Act - Write in Orders module
        using (_moduleContext.CreateScope("Orders"))
        {
            var collection = await factory.GetCollectionAsync<BsonDocument>(CollectionName);
            await collection.InsertOneAsync(testDocument);
        }

        // Assert - Try to read from Inventory module (should not find it)
        using (_moduleContext.CreateScope("Inventory"))
        {
            var collection = await factory.GetCollectionAsync<BsonDocument>(CollectionName);
            var found = await collection.Find(d => d["_id"] == testDocument["_id"]).FirstOrDefaultAsync();
            found.ShouldBeNull(); // Document is in Orders database, not Inventory
        }
    }

    [SkippableFact]
    public async Task EachModule_ShouldHaveIsolatedData()
    {
        Skip.IfNot(_fixture.IsAvailable);

        // Arrange
        var factory = CreateFactory();

        // Act - Insert documents in both modules
        using (_moduleContext.CreateScope("Orders"))
        {
            var collection = await factory.GetCollectionAsync<BsonDocument>(CollectionName);
            await collection.InsertOneAsync(new BsonDocument
            {
                ["_id"] = "order-1",
                ["type"] = "Order"
            });
        }

        using (_moduleContext.CreateScope("Inventory"))
        {
            var collection = await factory.GetCollectionAsync<BsonDocument>(CollectionName);
            await collection.InsertOneAsync(new BsonDocument
            {
                ["_id"] = "inventory-1",
                ["type"] = "Inventory"
            });
        }

        // Assert - Each module should only see its own data
        using (_moduleContext.CreateScope("Orders"))
        {
            var collection = await factory.GetCollectionAsync<BsonDocument>(CollectionName);
            var count = await collection.CountDocumentsAsync(Builders<BsonDocument>.Filter.Empty);
            count.ShouldBe(1);

            var doc = await collection.Find(Builders<BsonDocument>.Filter.Empty).FirstOrDefaultAsync();
            doc!["type"].AsString.ShouldBe("Order");
        }

        using (_moduleContext.CreateScope("Inventory"))
        {
            var collection = await factory.GetCollectionAsync<BsonDocument>(CollectionName);
            var count = await collection.CountDocumentsAsync(Builders<BsonDocument>.Filter.Empty);
            count.ShouldBe(1);

            var doc = await collection.Find(Builders<BsonDocument>.Filter.Empty).FirstOrDefaultAsync();
            doc!["type"].AsString.ShouldBe("Inventory");
        }
    }

    [SkippableFact]
    public async Task GetCollectionForModuleAsync_ShouldBypassCurrentModuleContext()
    {
        Skip.IfNot(_fixture.IsAvailable);

        // Arrange
        var factory = CreateFactory();

        // Insert into Inventory database explicitly
        var inventoryCollection = await factory.GetCollectionForModuleAsync<BsonDocument>(CollectionName, "Inventory");
        await inventoryCollection.InsertOneAsync(new BsonDocument
        {
            ["_id"] = "explicit-access-1",
            ["source"] = "explicit"
        });

        // Act - While in Orders context, access Inventory explicitly
        using (_moduleContext.CreateScope("Orders"))
        {
            var explicitInventoryCollection = await factory.GetCollectionForModuleAsync<BsonDocument>(CollectionName, "Inventory");
            var found = await explicitInventoryCollection.Find(d => d["_id"] == "explicit-access-1").FirstOrDefaultAsync();

            // Assert
            found.ShouldNotBeNull();
            found["source"].AsString.ShouldBe("explicit");
        }
    }

    [SkippableFact]
    public async Task ModuleDatabaseMappings_ShouldOverridePattern()
    {
        Skip.IfNot(_fixture.IsAvailable);

        // Arrange - Configure explicit mapping for Shared module
        var isolationOptions = Options.Create(new MongoDbModuleIsolationOptions
        {
            EnableDatabasePerModule = true,
            DatabaseNamePattern = "{baseName}_{moduleName}"
        });
        isolationOptions.Value.ModuleDatabaseMappings["Shared"] = "shared_global_db";

        var factory = new ModuleAwareMongoCollectionFactory(
            _fixture.Client!,
            _moduleContext,
            _mongoOptions,
            isolationOptions,
            NullLogger<ModuleAwareMongoCollectionFactory>.Instance);

        // Act
        using (_moduleContext.CreateScope("Shared"))
        {
            var collection = await factory.GetCollectionAsync<BsonDocument>(CollectionName);

            // Assert - Should use explicit mapping, not pattern
            collection.CollectionNamespace.DatabaseNamespace.DatabaseName
                .ShouldBe("shared_global_db");
        }
    }

    [SkippableFact]
    public async Task ConcurrentModuleOperations_ShouldMaintainIsolation()
    {
        Skip.IfNot(_fixture.IsAvailable);

        // Arrange
        var factory = CreateFactory();
        var tasks = new List<Task>();

        // Act - Multiple concurrent operations in different modules
        for (int i = 0; i < 5; i++)
        {
            var moduleIndex = i;
            var moduleName = moduleIndex % 2 == 0 ? "Orders" : "Inventory";

            tasks.Add(Task.Run(async () =>
            {
                using (_moduleContext.CreateScope(moduleName))
                {
                    var collection = await factory.GetCollectionAsync<BsonDocument>(CollectionName);
                    await collection.InsertOneAsync(new BsonDocument
                    {
                        ["_id"] = $"concurrent-{moduleName}-{moduleIndex}",
                        ["module"] = moduleName,
                        ["index"] = moduleIndex
                    });
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert - Verify each module has correct documents
        using (_moduleContext.CreateScope("Orders"))
        {
            var collection = await factory.GetCollectionAsync<BsonDocument>(CollectionName);
            var docs = await collection.Find(Builders<BsonDocument>.Filter.Empty).ToListAsync();
            docs.ShouldAllBe(d => d["module"].AsString == "Orders");
        }

        using (_moduleContext.CreateScope("Inventory"))
        {
            var collection = await factory.GetCollectionAsync<BsonDocument>(CollectionName);
            var docs = await collection.Find(Builders<BsonDocument>.Filter.Empty).ToListAsync();
            docs.ShouldAllBe(d => d["module"].AsString == "Inventory");
        }
    }

    #endregion

    #region Configuration Option Tests

    [SkippableFact]
    public async Task CustomDatabaseNamePattern_ModuleNameOnly_ShouldGenerateCorrectDatabaseName()
    {
        Skip.IfNot(_fixture.IsAvailable);

        // Arrange
        var isolationOptions = Options.Create(new MongoDbModuleIsolationOptions
        {
            EnableDatabasePerModule = true,
            DatabaseNamePattern = "{moduleName}_db"
        });

        var factory = new ModuleAwareMongoCollectionFactory(
            _fixture.Client!,
            _moduleContext,
            _mongoOptions,
            isolationOptions,
            NullLogger<ModuleAwareMongoCollectionFactory>.Instance);

        // Act
        using (_moduleContext.CreateScope("Orders"))
        {
            var collection = await factory.GetCollectionAsync<BsonDocument>(CollectionName);

            // Assert - Pattern uses only moduleName
            collection.CollectionNamespace.DatabaseNamespace.DatabaseName
                .ShouldBe("orders_db");
        }
    }

    [SkippableFact]
    public async Task CustomDatabaseNamePattern_BaseNameModuleName_ShouldGenerateCorrectDatabaseName()
    {
        Skip.IfNot(_fixture.IsAvailable);

        // Arrange
        var isolationOptions = Options.Create(new MongoDbModuleIsolationOptions
        {
            EnableDatabasePerModule = true,
            DatabaseNamePattern = "{baseName}_{moduleName}"
        });

        var factory = new ModuleAwareMongoCollectionFactory(
            _fixture.Client!,
            _moduleContext,
            _mongoOptions,
            isolationOptions,
            NullLogger<ModuleAwareMongoCollectionFactory>.Instance);

        // Act
        using (_moduleContext.CreateScope("Orders"))
        {
            var collection = await factory.GetCollectionAsync<BsonDocument>(CollectionName);

            // Assert
            collection.CollectionNamespace.DatabaseNamespace.DatabaseName
                .ShouldBe($"{BaseDatabaseName}_orders");
        }
    }

    [SkippableFact]
    public async Task ModuleDatabaseMappings_MultipleModules_ShouldOverridePatternForMappedModules()
    {
        Skip.IfNot(_fixture.IsAvailable);

        // Arrange
        var isolationOptions = Options.Create(new MongoDbModuleIsolationOptions
        {
            EnableDatabasePerModule = true,
            DatabaseNamePattern = "{baseName}_{moduleName}"
        });
        isolationOptions.Value.ModuleDatabaseMappings["Orders"] = "custom_orders_database";
        isolationOptions.Value.ModuleDatabaseMappings["Shared"] = "shared_global_db";

        var factory = new ModuleAwareMongoCollectionFactory(
            _fixture.Client!,
            _moduleContext,
            _mongoOptions,
            isolationOptions,
            NullLogger<ModuleAwareMongoCollectionFactory>.Instance);

        // Act & Assert - Orders uses explicit mapping
        using (_moduleContext.CreateScope("Orders"))
        {
            var collection = await factory.GetCollectionAsync<BsonDocument>(CollectionName);
            collection.CollectionNamespace.DatabaseNamespace.DatabaseName
                .ShouldBe("custom_orders_database");
        }

        // Act & Assert - Shared uses explicit mapping
        using (_moduleContext.CreateScope("Shared"))
        {
            var collection = await factory.GetCollectionAsync<BsonDocument>(CollectionName);
            collection.CollectionNamespace.DatabaseNamespace.DatabaseName
                .ShouldBe("shared_global_db");
        }

        // Act & Assert - Inventory uses pattern (no mapping)
        using (_moduleContext.CreateScope("Inventory"))
        {
            var collection = await factory.GetCollectionAsync<BsonDocument>(CollectionName);
            collection.CollectionNamespace.DatabaseNamespace.DatabaseName
                .ShouldBe($"{BaseDatabaseName}_inventory");
        }
    }

    [SkippableFact]
    public async Task EnableDatabasePerModuleFalse_AllModules_ShouldRouteToDefaultDatabase()
    {
        Skip.IfNot(_fixture.IsAvailable);

        // Arrange
        var isolationOptions = Options.Create(new MongoDbModuleIsolationOptions
        {
            EnableDatabasePerModule = false
        });

        var factory = new ModuleAwareMongoCollectionFactory(
            _fixture.Client!,
            _moduleContext,
            _mongoOptions,
            isolationOptions,
            NullLogger<ModuleAwareMongoCollectionFactory>.Instance);

        // Act & Assert - All modules should use base database
        foreach (var moduleName in new[] { "Orders", "Inventory", "Shared" })
        {
            using (_moduleContext.CreateScope(moduleName))
            {
                var collection = await factory.GetCollectionAsync<BsonDocument>(CollectionName);
                collection.CollectionNamespace.DatabaseNamespace.DatabaseName
                    .ShouldBe(BaseDatabaseName);
            }
        }
    }

    [SkippableFact]
    public async Task ModuleDatabaseMappings_CaseInsensitiveLookup_ShouldFindMapping()
    {
        Skip.IfNot(_fixture.IsAvailable);

        // Arrange - Add mapping with uppercase key
        var isolationOptions = Options.Create(new MongoDbModuleIsolationOptions
        {
            EnableDatabasePerModule = true
        });
        isolationOptions.Value.ModuleDatabaseMappings["ORDERS"] = "uppercase_orders_db";

        var factory = new ModuleAwareMongoCollectionFactory(
            _fixture.Client!,
            _moduleContext,
            _mongoOptions,
            isolationOptions,
            NullLogger<ModuleAwareMongoCollectionFactory>.Instance);

        // Act - Use lowercase module name
        using (_moduleContext.CreateScope("orders"))
        {
            var collection = await factory.GetCollectionAsync<BsonDocument>(CollectionName);

            // Assert - Should find mapping case-insensitively
            collection.CollectionNamespace.DatabaseNamespace.DatabaseName
                .ShouldBe("uppercase_orders_db");
        }
    }

    [SkippableFact]
    public async Task ModuleDatabaseMappings_MixedCaseLookup_ShouldFindMapping()
    {
        Skip.IfNot(_fixture.IsAvailable);

        // Arrange - Add mapping with mixed case key
        var isolationOptions = Options.Create(new MongoDbModuleIsolationOptions
        {
            EnableDatabasePerModule = true
        });
        isolationOptions.Value.ModuleDatabaseMappings["OrDeRs"] = "mixed_case_orders_db";

        var factory = new ModuleAwareMongoCollectionFactory(
            _fixture.Client!,
            _moduleContext,
            _mongoOptions,
            isolationOptions,
            NullLogger<ModuleAwareMongoCollectionFactory>.Instance);

        // Act - Use PascalCase module name
        using (_moduleContext.CreateScope("Orders"))
        {
            var collection = await factory.GetCollectionAsync<BsonDocument>(CollectionName);

            // Assert - Should find mapping case-insensitively
            collection.CollectionNamespace.DatabaseNamespace.DatabaseName
                .ShouldBe("mixed_case_orders_db");
        }
    }

    #endregion

    #region Module Context Behavior Tests

    [SkippableFact]
    public async Task ThrowOnMissingModuleContextTrue_NoModuleSet_ShouldThrowInvalidOperationException()
    {
        Skip.IfNot(_fixture.IsAvailable);

        // Arrange
        var isolationOptions = Options.Create(new MongoDbModuleIsolationOptions
        {
            EnableDatabasePerModule = true,
            ThrowOnMissingModuleContext = true
        });

        var factory = new ModuleAwareMongoCollectionFactory(
            _fixture.Client!,
            _moduleContext,
            _mongoOptions,
            isolationOptions,
            NullLogger<ModuleAwareMongoCollectionFactory>.Instance);

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(async () =>
            await factory.GetCollectionAsync<BsonDocument>(CollectionName));

        exception.Message.ShouldContain("No module context is available");
    }

    [SkippableFact]
    public async Task ThrowOnMissingModuleContextFalse_NoModuleSet_ShouldFallbackToDefaultDatabase()
    {
        Skip.IfNot(_fixture.IsAvailable);

        // Arrange
        var isolationOptions = Options.Create(new MongoDbModuleIsolationOptions
        {
            EnableDatabasePerModule = true,
            ThrowOnMissingModuleContext = false
        });

        var factory = new ModuleAwareMongoCollectionFactory(
            _fixture.Client!,
            _moduleContext,
            _mongoOptions,
            isolationOptions,
            NullLogger<ModuleAwareMongoCollectionFactory>.Instance);

        // Act - No module context set
        var collection = await factory.GetCollectionAsync<BsonDocument>(CollectionName);

        // Assert - Should fallback to base database
        collection.CollectionNamespace.DatabaseNamespace.DatabaseName
            .ShouldBe(BaseDatabaseName);
    }

    [SkippableFact]
    public async Task LogWarningOnFallbackTrue_NoModuleContext_ShouldLogWarning()
    {
        Skip.IfNot(_fixture.IsAvailable);

        // Arrange
        var fakeLogger = new FakeLogger<ModuleAwareMongoCollectionFactory>();
        var isolationOptions = Options.Create(new MongoDbModuleIsolationOptions
        {
            EnableDatabasePerModule = true,
            ThrowOnMissingModuleContext = false,
            LogWarningOnFallback = true
        });

        var factory = new ModuleAwareMongoCollectionFactory(
            _fixture.Client!,
            _moduleContext,
            _mongoOptions,
            isolationOptions,
            fakeLogger);

        // Act - No module context set
        await factory.GetCollectionAsync<BsonDocument>(CollectionName);

        // Assert - Should have logged a warning
        fakeLogger.Collector.Count.ShouldBeGreaterThan(0);
        var logEntry = fakeLogger.Collector.GetSnapshot()[0];
        logEntry.Level.ShouldBe(LogLevel.Warning);
        logEntry.Message.ShouldContain(CollectionName);
    }

    [SkippableFact]
    public async Task LogWarningOnFallbackFalse_NoModuleContext_ShouldNotLogWarning()
    {
        Skip.IfNot(_fixture.IsAvailable);

        // Arrange
        var fakeLogger = new FakeLogger<ModuleAwareMongoCollectionFactory>();
        var isolationOptions = Options.Create(new MongoDbModuleIsolationOptions
        {
            EnableDatabasePerModule = true,
            ThrowOnMissingModuleContext = false,
            LogWarningOnFallback = false
        });

        var factory = new ModuleAwareMongoCollectionFactory(
            _fixture.Client!,
            _moduleContext,
            _mongoOptions,
            isolationOptions,
            fakeLogger);

        // Act - No module context set
        await factory.GetCollectionAsync<BsonDocument>(CollectionName);

        // Assert - Should NOT have logged anything
        fakeLogger.Collector.Count.ShouldBe(0);
    }

    [SkippableFact]
    public async Task CreateScope_ScopedContexts_ShouldNotInterfere()
    {
        Skip.IfNot(_fixture.IsAvailable);

        // Arrange
        var factory = CreateFactory();
        var moduleContext = new ModuleExecutionContext();

        // Create factory with fresh context
        var scopedFactory = new ModuleAwareMongoCollectionFactory(
            _fixture.Client!,
            moduleContext,
            _mongoOptions,
            _isolationOptions,
            NullLogger<ModuleAwareMongoCollectionFactory>.Instance);

        // Act - Create nested scopes
        using (moduleContext.CreateScope("Orders"))
        {
            var ordersCollection = await scopedFactory.GetCollectionAsync<BsonDocument>(CollectionName);
            ordersCollection.CollectionNamespace.DatabaseNamespace.DatabaseName
                .ShouldBe($"{BaseDatabaseName}_orders");

            // Nested scope with different module
            using (moduleContext.CreateScope("Inventory"))
            {
                var inventoryCollection = await scopedFactory.GetCollectionAsync<BsonDocument>(CollectionName);
                inventoryCollection.CollectionNamespace.DatabaseNamespace.DatabaseName
                    .ShouldBe($"{BaseDatabaseName}_inventory");
            }

            // After nested scope ends, should be back to Orders
            var ordersCollectionAfter = await scopedFactory.GetCollectionAsync<BsonDocument>(CollectionName);
            ordersCollectionAfter.CollectionNamespace.DatabaseNamespace.DatabaseName
                .ShouldBe($"{BaseDatabaseName}_orders");
        }

        // After all scopes end, should be no module context
        moduleContext.CurrentModule.ShouldBeNull();
    }

    [SkippableFact]
    public async Task GetDatabaseNameAsync_ThrowOnMissingContextTrue_ShouldThrowWhenNoModule()
    {
        Skip.IfNot(_fixture.IsAvailable);

        // Arrange
        var isolationOptions = Options.Create(new MongoDbModuleIsolationOptions
        {
            EnableDatabasePerModule = true,
            ThrowOnMissingModuleContext = true
        });

        var factory = new ModuleAwareMongoCollectionFactory(
            _fixture.Client!,
            _moduleContext,
            _mongoOptions,
            isolationOptions,
            NullLogger<ModuleAwareMongoCollectionFactory>.Instance);

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(async () =>
            await factory.GetDatabaseNameAsync());

        exception.Message.ShouldContain("No module context is available");
    }

    #endregion

    #region Input Validation Tests

    [SkippableFact]
    public async Task GetCollectionAsync_NullCollectionName_ShouldThrowArgumentException()
    {
        Skip.IfNot(_fixture.IsAvailable);

        // Arrange
        var factory = CreateFactory();

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await factory.GetCollectionAsync<BsonDocument>(null!));
    }

    [SkippableFact]
    public async Task GetCollectionAsync_EmptyCollectionName_ShouldThrowArgumentException()
    {
        Skip.IfNot(_fixture.IsAvailable);

        // Arrange
        var factory = CreateFactory();

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await factory.GetCollectionAsync<BsonDocument>(string.Empty));
    }

    [SkippableFact]
    public async Task GetCollectionForModuleAsync_NullCollectionName_ShouldThrowArgumentException()
    {
        Skip.IfNot(_fixture.IsAvailable);

        // Arrange
        var factory = CreateFactory();

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await factory.GetCollectionForModuleAsync<BsonDocument>(null!, "Orders"));
    }

    [SkippableFact]
    public async Task GetCollectionForModuleAsync_EmptyCollectionName_ShouldThrowArgumentException()
    {
        Skip.IfNot(_fixture.IsAvailable);

        // Arrange
        var factory = CreateFactory();

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await factory.GetCollectionForModuleAsync<BsonDocument>(string.Empty, "Orders"));
    }

    [SkippableFact]
    public async Task GetCollectionForModuleAsync_NullModuleName_ShouldThrowArgumentException()
    {
        Skip.IfNot(_fixture.IsAvailable);

        // Arrange
        var factory = CreateFactory();

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await factory.GetCollectionForModuleAsync<BsonDocument>(CollectionName, null!));
    }

    [SkippableFact]
    public async Task GetCollectionForModuleAsync_EmptyModuleName_ShouldThrowArgumentException()
    {
        Skip.IfNot(_fixture.IsAvailable);

        // Arrange
        var factory = CreateFactory();

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await factory.GetCollectionForModuleAsync<BsonDocument>(CollectionName, string.Empty));
    }

    [SkippableFact]
    public void GetDatabaseNameForModule_NullModuleName_ShouldThrowArgumentException()
    {
        Skip.IfNot(_fixture.IsAvailable);

        // Arrange
        var factory = CreateFactory();

        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            factory.GetDatabaseNameForModule(null!));
    }

    [SkippableFact]
    public void GetDatabaseNameForModule_EmptyModuleName_ShouldThrowArgumentException()
    {
        Skip.IfNot(_fixture.IsAvailable);

        // Arrange
        var factory = CreateFactory();

        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            factory.GetDatabaseNameForModule(string.Empty));
    }

    [SkippableFact]
    public async Task GetCollectionAsync_MissingDatabaseNameConfiguration_ShouldThrowInvalidOperationException()
    {
        Skip.IfNot(_fixture.IsAvailable);

        // Arrange - No DatabaseName configured
        var mongoOptions = Options.Create(new EncinaMongoDbOptions
        {
            DatabaseName = string.Empty
        });

        var isolationOptions = Options.Create(new MongoDbModuleIsolationOptions
        {
            EnableDatabasePerModule = false,
            ThrowOnMissingModuleContext = false
        });

        var factory = new ModuleAwareMongoCollectionFactory(
            _fixture.Client!,
            _moduleContext,
            mongoOptions,
            isolationOptions,
            NullLogger<ModuleAwareMongoCollectionFactory>.Instance);

        // Act & Assert - Without module context, should try to use default database
        var exception = await Should.ThrowAsync<InvalidOperationException>(async () =>
            await factory.GetCollectionAsync<BsonDocument>(CollectionName));

        exception.Message.ShouldContain("No default database name configured");
    }

    #endregion

    #region Multi-Module Scenario Tests

    [SkippableFact]
    public async Task MultiModule_InsertInModuleA_ShouldNotBeVisibleInModuleB()
    {
        Skip.IfNot(_fixture.IsAvailable);

        // Arrange
        var factory = CreateFactory();
        var testDoc = new BsonDocument
        {
            ["_id"] = Guid.NewGuid().ToString(),
            ["moduleId"] = "ModuleA",
            ["name"] = "ModuleA-Only-Document",
            ["value"] = 100
        };

        // Act - Insert in ModuleA
        using (_moduleContext.CreateScope("ModuleA"))
        {
            var collection = await factory.GetCollectionAsync<BsonDocument>(CollectionName);
            await collection.InsertOneAsync(testDoc);
        }

        // Assert - Document should not be visible in ModuleB
        using (_moduleContext.CreateScope("ModuleB"))
        {
            var collection = await factory.GetCollectionAsync<BsonDocument>(CollectionName);
            var found = await collection.Find(d => d["_id"] == testDoc["_id"]).FirstOrDefaultAsync();
            found.ShouldBeNull();
        }

        // Assert - Document should still be visible in ModuleA
        using (_moduleContext.CreateScope("ModuleA"))
        {
            var collection = await factory.GetCollectionAsync<BsonDocument>(CollectionName);
            var found = await collection.Find(d => d["_id"] == testDoc["_id"]).FirstOrDefaultAsync();
            found.ShouldNotBeNull();
            found["moduleId"].AsString.ShouldBe("ModuleA");
        }
    }

    [SkippableFact]
    public async Task MultiModule_InsertInModuleB_ShouldNotBeVisibleInModuleA()
    {
        Skip.IfNot(_fixture.IsAvailable);

        // Arrange
        var factory = CreateFactory();
        var testDoc = new BsonDocument
        {
            ["_id"] = Guid.NewGuid().ToString(),
            ["moduleId"] = "ModuleB",
            ["name"] = "ModuleB-Only-Document",
            ["value"] = 200
        };

        // Act - Insert in ModuleB
        using (_moduleContext.CreateScope("ModuleB"))
        {
            var collection = await factory.GetCollectionAsync<BsonDocument>(CollectionName);
            await collection.InsertOneAsync(testDoc);
        }

        // Assert - Document should not be visible in ModuleA
        using (_moduleContext.CreateScope("ModuleA"))
        {
            var collection = await factory.GetCollectionAsync<BsonDocument>(CollectionName);
            var found = await collection.Find(d => d["_id"] == testDoc["_id"]).FirstOrDefaultAsync();
            found.ShouldBeNull();
        }

        // Assert - Document should still be visible in ModuleB
        using (_moduleContext.CreateScope("ModuleB"))
        {
            var collection = await factory.GetCollectionAsync<BsonDocument>(CollectionName);
            var found = await collection.Find(d => d["_id"] == testDoc["_id"]).FirstOrDefaultAsync();
            found.ShouldNotBeNull();
            found["moduleId"].AsString.ShouldBe("ModuleB");
        }
    }

    [SkippableFact]
    public async Task MultiModule_ContextSwitching_ShouldMaintainIsolation()
    {
        Skip.IfNot(_fixture.IsAvailable);

        // Arrange - Use unique module names to avoid conflicts with other tests
        var factory = CreateFactory();
        var testId = Guid.NewGuid().ToString("N")[..8]; // Unique test run ID
        var moduleAlpha = $"Alpha_{testId}";
        var moduleBeta = $"Beta_{testId}";
        var moduleGamma = $"Gamma_{testId}";

        // Act - Insert documents while switching contexts multiple times
        var moduleAlphaDocId = Guid.NewGuid().ToString();
        var moduleBetaDocId = Guid.NewGuid().ToString();
        var moduleGammaDocId = Guid.NewGuid().ToString();

        // Insert in ModuleAlpha
        using (_moduleContext.CreateScope(moduleAlpha))
        {
            var collection = await factory.GetCollectionAsync<BsonDocument>(CollectionName);
            await collection.InsertOneAsync(new BsonDocument
            {
                ["_id"] = moduleAlphaDocId,
                ["moduleId"] = moduleAlpha,
                ["sequence"] = 1
            });
        }

        // Switch to ModuleBeta and insert
        using (_moduleContext.CreateScope(moduleBeta))
        {
            var collection = await factory.GetCollectionAsync<BsonDocument>(CollectionName);
            await collection.InsertOneAsync(new BsonDocument
            {
                ["_id"] = moduleBetaDocId,
                ["moduleId"] = moduleBeta,
                ["sequence"] = 2
            });
        }

        // Switch back to ModuleAlpha, verify previous document, then switch to ModuleGamma
        using (_moduleContext.CreateScope(moduleAlpha))
        {
            var collection = await factory.GetCollectionAsync<BsonDocument>(CollectionName);
            var found = await collection.Find(d => d["_id"] == moduleAlphaDocId).FirstOrDefaultAsync();
            found.ShouldNotBeNull();
            found["sequence"].AsInt32.ShouldBe(1);
        }

        // Insert in ModuleGamma
        using (_moduleContext.CreateScope(moduleGamma))
        {
            var collection = await factory.GetCollectionAsync<BsonDocument>(CollectionName);
            await collection.InsertOneAsync(new BsonDocument
            {
                ["_id"] = moduleGammaDocId,
                ["moduleId"] = moduleGamma,
                ["sequence"] = 3
            });
        }

        // Assert - Verify each module only sees its own document
        using (_moduleContext.CreateScope(moduleAlpha))
        {
            var collection = await factory.GetCollectionAsync<BsonDocument>(CollectionName);
            var count = await collection.CountDocumentsAsync(Builders<BsonDocument>.Filter.Empty);
            count.ShouldBe(1);
        }

        using (_moduleContext.CreateScope(moduleBeta))
        {
            var collection = await factory.GetCollectionAsync<BsonDocument>(CollectionName);
            var count = await collection.CountDocumentsAsync(Builders<BsonDocument>.Filter.Empty);
            count.ShouldBe(1);
        }

        using (_moduleContext.CreateScope(moduleGamma))
        {
            var collection = await factory.GetCollectionAsync<BsonDocument>(CollectionName);
            var count = await collection.CountDocumentsAsync(Builders<BsonDocument>.Filter.Empty);
            count.ShouldBe(1);
        }
    }

    [SkippableFact]
    public async Task MultiModule_AfterAllOperations_EachDatabaseContainsOnlyOwnData()
    {
        Skip.IfNot(_fixture.IsAvailable);

        // Arrange
        var factory = CreateFactory();
        var modules = new[] { "Sales", "Marketing", "Support" };
        var docsPerModule = 3;

        // Act - Insert multiple documents per module
        foreach (var module in modules)
        {
            using (_moduleContext.CreateScope(module))
            {
                var collection = await factory.GetCollectionAsync<BsonDocument>(CollectionName);
                for (int i = 0; i < docsPerModule; i++)
                {
                    await collection.InsertOneAsync(new BsonDocument
                    {
                        ["_id"] = $"{module}-doc-{i}",
                        ["moduleId"] = module,
                        ["index"] = i,
                        ["timestamp"] = DateTime.UtcNow
                    });
                }
            }
        }

        // Assert - Each module database contains exactly its own documents
        foreach (var module in modules)
        {
            using (_moduleContext.CreateScope(module))
            {
                var collection = await factory.GetCollectionAsync<BsonDocument>(CollectionName);

                // Count should match docsPerModule
                var count = await collection.CountDocumentsAsync(Builders<BsonDocument>.Filter.Empty);
                count.ShouldBe(docsPerModule);

                // All documents should belong to this module
                var docs = await collection.Find(Builders<BsonDocument>.Filter.Empty).ToListAsync();
                docs.ShouldAllBe(d => d["moduleId"].AsString == module);

                // Verify document IDs
                for (int i = 0; i < docsPerModule; i++)
                {
                    var expectedId = $"{module}-doc-{i}";
                    docs.ShouldContain(d => d["_id"].AsString == expectedId);
                }
            }
        }
    }

    [SkippableFact]
    public async Task MultiModule_SameEntityType_RemainsIndependent()
    {
        Skip.IfNot(_fixture.IsAvailable);

        // Arrange
        var factory = CreateFactory();
        const string ordersCollection = "orders";

        // Create orders with the same ID in different modules
        var orderId = "ORDER-001";

        // Act - Insert order in Sales module
        using (_moduleContext.CreateScope("Sales"))
        {
            var collection = await factory.GetCollectionAsync<BsonDocument>(ordersCollection);
            await collection.InsertOneAsync(new BsonDocument
            {
                ["_id"] = orderId,
                ["moduleId"] = "Sales",
                ["amount"] = 1000.00,
                ["status"] = "Completed"
            });
        }

        // Act - Insert order with same ID in Marketing module (should not conflict)
        using (_moduleContext.CreateScope("Marketing"))
        {
            var collection = await factory.GetCollectionAsync<BsonDocument>(ordersCollection);
            await collection.InsertOneAsync(new BsonDocument
            {
                ["_id"] = orderId, // Same ID, different database
                ["moduleId"] = "Marketing",
                ["amount"] = 2500.00,
                ["status"] = "Pending"
            });
        }

        // Assert - Each module has its own version of the order
        using (_moduleContext.CreateScope("Sales"))
        {
            var collection = await factory.GetCollectionAsync<BsonDocument>(ordersCollection);
            var order = await collection.Find(d => d["_id"] == orderId).FirstOrDefaultAsync();
            order.ShouldNotBeNull();
            order["amount"].AsDouble.ShouldBe(1000.00);
            order["status"].AsString.ShouldBe("Completed");
        }

        using (_moduleContext.CreateScope("Marketing"))
        {
            var collection = await factory.GetCollectionAsync<BsonDocument>(ordersCollection);
            var order = await collection.Find(d => d["_id"] == orderId).FirstOrDefaultAsync();
            order.ShouldNotBeNull();
            order["amount"].AsDouble.ShouldBe(2500.00);
            order["status"].AsString.ShouldBe("Pending");
        }
    }

    [SkippableFact]
    public async Task MultiModule_UpdateInOneModule_ShouldNotAffectOtherModules()
    {
        Skip.IfNot(_fixture.IsAvailable);

        // Arrange
        var factory = CreateFactory();
        var docId = Guid.NewGuid().ToString();

        // Insert same document structure in both modules
        foreach (var module in new[] { "ModuleA", "ModuleB" })
        {
            using (_moduleContext.CreateScope(module))
            {
                var collection = await factory.GetCollectionAsync<BsonDocument>(CollectionName);
                await collection.InsertOneAsync(new BsonDocument
                {
                    ["_id"] = docId,
                    ["moduleId"] = module,
                    ["version"] = 1,
                    ["data"] = "initial"
                });
            }
        }

        // Act - Update document in ModuleA only
        using (_moduleContext.CreateScope("ModuleA"))
        {
            var collection = await factory.GetCollectionAsync<BsonDocument>(CollectionName);
            await collection.UpdateOneAsync(
                Builders<BsonDocument>.Filter.Eq("_id", docId),
                Builders<BsonDocument>.Update
                    .Set("version", 2)
                    .Set("data", "updated"));
        }

        // Assert - ModuleA document is updated
        using (_moduleContext.CreateScope("ModuleA"))
        {
            var collection = await factory.GetCollectionAsync<BsonDocument>(CollectionName);
            var doc = await collection.Find(d => d["_id"] == docId).FirstOrDefaultAsync();
            doc!["version"].AsInt32.ShouldBe(2);
            doc["data"].AsString.ShouldBe("updated");
        }

        // Assert - ModuleB document remains unchanged
        using (_moduleContext.CreateScope("ModuleB"))
        {
            var collection = await factory.GetCollectionAsync<BsonDocument>(CollectionName);
            var doc = await collection.Find(d => d["_id"] == docId).FirstOrDefaultAsync();
            doc!["version"].AsInt32.ShouldBe(1);
            doc["data"].AsString.ShouldBe("initial");
        }
    }

    [SkippableFact]
    public async Task MultiModule_DeleteInOneModule_ShouldNotAffectOtherModules()
    {
        Skip.IfNot(_fixture.IsAvailable);

        // Arrange
        var factory = CreateFactory();
        var docId = Guid.NewGuid().ToString();

        // Insert same document structure in both modules
        foreach (var module in new[] { "ModuleA", "ModuleB" })
        {
            using (_moduleContext.CreateScope(module))
            {
                var collection = await factory.GetCollectionAsync<BsonDocument>(CollectionName);
                await collection.InsertOneAsync(new BsonDocument
                {
                    ["_id"] = docId,
                    ["moduleId"] = module,
                    ["toDelete"] = true
                });
            }
        }

        // Act - Delete document in ModuleA only
        using (_moduleContext.CreateScope("ModuleA"))
        {
            var collection = await factory.GetCollectionAsync<BsonDocument>(CollectionName);
            await collection.DeleteOneAsync(Builders<BsonDocument>.Filter.Eq("_id", docId));
        }

        // Assert - ModuleA document is deleted
        using (_moduleContext.CreateScope("ModuleA"))
        {
            var collection = await factory.GetCollectionAsync<BsonDocument>(CollectionName);
            var doc = await collection.Find(d => d["_id"] == docId).FirstOrDefaultAsync();
            doc.ShouldBeNull();
        }

        // Assert - ModuleB document still exists
        using (_moduleContext.CreateScope("ModuleB"))
        {
            var collection = await factory.GetCollectionAsync<BsonDocument>(CollectionName);
            var doc = await collection.Find(d => d["_id"] == docId).FirstOrDefaultAsync();
            doc.ShouldNotBeNull();
            doc["moduleId"].AsString.ShouldBe("ModuleB");
        }
    }

    [SkippableFact]
    public async Task MultiModule_ConcurrentOperations_ShouldMaintainDataIntegrity()
    {
        Skip.IfNot(_fixture.IsAvailable);

        // Arrange
        var factory = CreateFactory();
        var modules = new[] { "Concurrent1", "Concurrent2", "Concurrent3" };
        var docsPerModule = 10;

        // Act - Concurrent inserts across modules
        var tasks = modules.SelectMany(module =>
            Enumerable.Range(0, docsPerModule).Select(i =>
                Task.Run(async () =>
                {
                    using (_moduleContext.CreateScope(module))
                    {
                        var collection = await factory.GetCollectionAsync<BsonDocument>(CollectionName);
                        await collection.InsertOneAsync(new BsonDocument
                        {
                            ["_id"] = $"{module}-concurrent-{i}",
                            ["moduleId"] = module,
                            ["index"] = i,
                            ["threadId"] = Environment.CurrentManagedThreadId
                        });
                    }
                }))).ToList();

        await Task.WhenAll(tasks);

        // Assert - Each module has exactly its documents
        foreach (var module in modules)
        {
            using (_moduleContext.CreateScope(module))
            {
                var collection = await factory.GetCollectionAsync<BsonDocument>(CollectionName);

                var count = await collection.CountDocumentsAsync(Builders<BsonDocument>.Filter.Empty);
                count.ShouldBe(docsPerModule);

                var docs = await collection.Find(Builders<BsonDocument>.Filter.Empty).ToListAsync();
                docs.ShouldAllBe(d => d["moduleId"].AsString == module);
            }
        }
    }

    [SkippableFact]
    public async Task MultiModule_MixedReadWriteOperations_ShouldMaintainIsolation()
    {
        Skip.IfNot(_fixture.IsAvailable);

        // Arrange
        var factory = CreateFactory();

        // Pre-populate modules with data
        foreach (var module in new[] { "Reader", "Writer" })
        {
            using (_moduleContext.CreateScope(module))
            {
                var collection = await factory.GetCollectionAsync<BsonDocument>(CollectionName);
                for (int i = 0; i < 5; i++)
                {
                    await collection.InsertOneAsync(new BsonDocument
                    {
                        ["_id"] = $"{module}-initial-{i}",
                        ["moduleId"] = module,
                        ["initial"] = true
                    });
                }
            }
        }

        // Act - Concurrent reads and writes
        var tasks = new List<Task>();

        // Reader module: perform reads
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                using (_moduleContext.CreateScope("Reader"))
                {
                    var collection = await factory.GetCollectionAsync<BsonDocument>(CollectionName);
                    var docs = await collection.Find(Builders<BsonDocument>.Filter.Empty).ToListAsync();
                    docs.ShouldAllBe(d => d["moduleId"].AsString == "Reader");
                }
            }));
        }

        // Writer module: perform writes
        for (int i = 0; i < 10; i++)
        {
            var index = i;
            tasks.Add(Task.Run(async () =>
            {
                using (_moduleContext.CreateScope("Writer"))
                {
                    var collection = await factory.GetCollectionAsync<BsonDocument>(CollectionName);
                    await collection.InsertOneAsync(new BsonDocument
                    {
                        ["_id"] = $"Writer-new-{index}",
                        ["moduleId"] = "Writer",
                        ["initial"] = false
                    });
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert - Reader module unchanged (5 docs)
        using (_moduleContext.CreateScope("Reader"))
        {
            var collection = await factory.GetCollectionAsync<BsonDocument>(CollectionName);
            var count = await collection.CountDocumentsAsync(Builders<BsonDocument>.Filter.Empty);
            count.ShouldBe(5);
        }

        // Assert - Writer module has new docs (5 initial + 10 new = 15)
        using (_moduleContext.CreateScope("Writer"))
        {
            var collection = await factory.GetCollectionAsync<BsonDocument>(CollectionName);
            var count = await collection.CountDocumentsAsync(Builders<BsonDocument>.Filter.Empty);
            count.ShouldBe(15);
        }
    }

    #endregion

    #region Helper Methods

    private ModuleAwareMongoCollectionFactory CreateFactory()
    {
        return new ModuleAwareMongoCollectionFactory(
            _fixture.Client!,
            _moduleContext,
            _mongoOptions,
            _isolationOptions,
            NullLogger<ModuleAwareMongoCollectionFactory>.Instance);
    }

    private async Task CleanupDatabaseAsync(string databaseName)
    {
        try
        {
            await _fixture.Client!.DropDatabaseAsync(databaseName);
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    #endregion
}
