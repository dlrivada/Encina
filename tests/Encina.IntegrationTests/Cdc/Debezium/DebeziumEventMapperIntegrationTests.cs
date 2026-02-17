using System.Text.Json;
using Encina.Cdc;
using Encina.Cdc.Abstractions;
using Encina.Cdc.Debezium;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace Encina.IntegrationTests.Cdc.Debezium;

/// <summary>
/// Integration tests for <see cref="DebeziumEventMapper"/> using realistic Debezium JSON payloads.
/// These payloads simulate actual Debezium output from various database connectors.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Provider", "Debezium")]
public sealed class DebeziumEventMapperIntegrationTests
{
    private readonly ILogger _logger = Substitute.For<ILogger>();

    #region SQL Server Insert Event

    /// <summary>
    /// Verifies that a realistic SQL Server insert event from Debezium is correctly parsed.
    /// </summary>
    [Fact]
    public void MapEvent_SqlServerInsert_ShouldProduceCorrectChangeEvent()
    {
        // Arrange — realistic SQL Server Debezium event
        var json = ParseJson("""
        {
            "op": "c",
            "before": null,
            "after": {
                "id": 1001,
                "customer_name": "John Doe",
                "total_amount": 150.00,
                "created_at": "2026-01-15T10:30:00Z"
            },
            "source": {
                "version": "2.5.0.Final",
                "connector": "sqlserver",
                "name": "dbserver1",
                "db": "SalesDB",
                "schema": "dbo",
                "table": "Orders",
                "change_lsn": "00000027:00000ac0:0002",
                "commit_lsn": "00000027:00000ac0:0003"
            },
            "ts_ms": 1705312200000
        }
        """);

        // Act
        var result = DebeziumEventMapper.MapEvent(json, DebeziumEventFormat.Flat, _logger);

        // Assert
        result.IsRight.ShouldBeTrue();
        var evt = (ChangeEvent)result;
        evt.Operation.ShouldBe(ChangeOperation.Insert);
        evt.TableName.ShouldBe("dbo.Orders");
        evt.After.ShouldNotBeNull();
        evt.Before.ShouldBeNull();
        evt.Metadata.SourceDatabase.ShouldBe("SalesDB");
        evt.Metadata.SourceSchema.ShouldBe("dbo");
        evt.Metadata.Position.ShouldBeOfType<DebeziumCdcPosition>();
    }

    #endregion

    #region PostgreSQL Update Event

    /// <summary>
    /// Verifies that a realistic PostgreSQL update event correctly captures before and after.
    /// </summary>
    [Fact]
    public void MapEvent_PostgresUpdate_ShouldCaptureBeforeAndAfter()
    {
        // Arrange — realistic PostgreSQL Debezium event with before/after
        var json = ParseJson("""
        {
            "op": "u",
            "before": {
                "id": 42,
                "username": "old_name",
                "email": "old@example.com"
            },
            "after": {
                "id": 42,
                "username": "new_name",
                "email": "new@example.com"
            },
            "source": {
                "version": "2.5.0.Final",
                "connector": "postgresql",
                "name": "dbserver1",
                "db": "UserDB",
                "schema": "public",
                "table": "users",
                "lsn": 123456789
            },
            "ts_ms": 1705312300000
        }
        """);

        // Act
        var result = DebeziumEventMapper.MapEvent(json, DebeziumEventFormat.Flat, _logger);

        // Assert
        result.IsRight.ShouldBeTrue();
        var evt = (ChangeEvent)result;
        evt.Operation.ShouldBe(ChangeOperation.Update);
        evt.TableName.ShouldBe("public.users");
        evt.Before.ShouldNotBeNull();
        evt.After.ShouldNotBeNull();
        evt.Metadata.SourceDatabase.ShouldBe("UserDB");
    }

    #endregion

    #region MySQL Delete Event

    /// <summary>
    /// Verifies that a realistic MySQL delete event has before set and after null.
    /// </summary>
    [Fact]
    public void MapEvent_MySqlDelete_ShouldHaveBeforeSetAfterNull()
    {
        // Arrange — realistic MySQL Debezium delete event
        var json = ParseJson("""
        {
            "op": "d",
            "before": {
                "id": 77,
                "product_name": "Deleted Product",
                "price": 9.99
            },
            "after": null,
            "source": {
                "version": "2.5.0.Final",
                "connector": "mysql",
                "name": "dbserver1",
                "db": "InventoryDB",
                "table": "products",
                "file": "mysql-bin.000003",
                "pos": 456
            },
            "ts_ms": 1705312400000
        }
        """);

        // Act
        var result = DebeziumEventMapper.MapEvent(json, DebeziumEventFormat.Flat, _logger);

        // Assert
        result.IsRight.ShouldBeTrue();
        var evt = (ChangeEvent)result;
        evt.Operation.ShouldBe(ChangeOperation.Delete);
        evt.TableName.ShouldBe("products");
        evt.Before.ShouldNotBeNull();
        evt.After.ShouldBeNull();
    }

    #endregion

    #region CloudEvents Envelope

    /// <summary>
    /// Verifies that a CloudEvents envelope is correctly unwrapped.
    /// </summary>
    [Fact]
    public void MapEvent_CloudEventsEnvelope_ShouldExtractDataCorrectly()
    {
        // Arrange — CloudEvents envelope from Debezium Server
        var json = ParseJson("""
        {
            "id": "f81d4fae-7dec-11d0-a765-00a0c91e6bf6",
            "type": "io.debezium.connector.sqlserver.DataChangeEvent",
            "source": "/debezium/sqlserver/dbserver1",
            "specversion": "1.0",
            "time": "2026-01-15T10:30:00Z",
            "datacontenttype": "application/json",
            "data": {
                "op": "c",
                "after": {
                    "id": 500,
                    "status": "active"
                },
                "source": {
                    "db": "AppDB",
                    "schema": "dbo",
                    "table": "Accounts"
                }
            }
        }
        """);

        // Act
        var result = DebeziumEventMapper.MapEvent(json, DebeziumEventFormat.CloudEvents, _logger);

        // Assert
        result.IsRight.ShouldBeTrue();
        var evt = (ChangeEvent)result;
        evt.Operation.ShouldBe(ChangeOperation.Insert);
        evt.TableName.ShouldBe("dbo.Accounts");
    }

    #endregion

    #region Schema Change Event (No Op)

    /// <summary>
    /// Verifies that schema change events (missing op field) return a Left error.
    /// </summary>
    [Fact]
    public void MapEvent_SchemaChangeEvent_ShouldReturnLeftError()
    {
        // Arrange — Debezium schema change / DDL event (no "op" field)
        var json = ParseJson("""
        {
            "source": {
                "version": "2.5.0.Final",
                "connector": "sqlserver",
                "name": "dbserver1",
                "db": "SalesDB",
                "schema": "dbo"
            },
            "databaseName": "SalesDB",
            "schemaName": "dbo",
            "ddl": "ALTER TABLE Orders ADD COLUMN notes VARCHAR(500)",
            "tableChanges": []
        }
        """);

        // Act
        var result = DebeziumEventMapper.MapEvent(json, DebeziumEventFormat.Flat, _logger);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region Helpers

    private static JsonElement ParseJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    #endregion
}
