using System.Text.Json;
using Encina.Cdc;
using Encina.Cdc.Abstractions;
using Encina.Cdc.Debezium;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace Encina.UnitTests.Cdc.Debezium;

/// <summary>
/// Unit tests for <see cref="DebeziumEventMapper"/>.
/// Covers op code mapping, CloudEvents/Flat format parsing, metadata extraction,
/// before/after handling, position creation, and error cases.
/// </summary>
public sealed class DebeziumEventMapperTests
{
    private readonly ILogger _logger = Substitute.For<ILogger>();

    #region Flat Format — Op Code Mapping

    /// <summary>
    /// Verifies that op="c" (create) maps to Insert operation.
    /// </summary>
    [Fact]
    public void MapEvent_Flat_OpCreate_ShouldMapToInsert()
    {
        // Arrange
        var json = ParseJson("""{"op":"c","after":{"id":1},"source":{"db":"testdb","schema":"dbo","table":"Orders"}}""");

        // Act
        var result = DebeziumEventMapper.MapEvent(json, DebeziumEventFormat.Flat, _logger);

        // Assert
        result.IsRight.ShouldBeTrue();
        var evt = (ChangeEvent)result;
        evt.Operation.ShouldBe(ChangeOperation.Insert);
    }

    /// <summary>
    /// Verifies that op="r" (read/snapshot) maps to Snapshot operation.
    /// </summary>
    [Fact]
    public void MapEvent_Flat_OpRead_ShouldMapToSnapshot()
    {
        // Arrange
        var json = ParseJson("""{"op":"r","after":{"id":1},"source":{"db":"testdb","table":"Orders"}}""");

        // Act
        var result = DebeziumEventMapper.MapEvent(json, DebeziumEventFormat.Flat, _logger);

        // Assert
        result.IsRight.ShouldBeTrue();
        var evt = (ChangeEvent)result;
        evt.Operation.ShouldBe(ChangeOperation.Snapshot);
    }

    /// <summary>
    /// Verifies that op="u" (update) maps to Update operation.
    /// </summary>
    [Fact]
    public void MapEvent_Flat_OpUpdate_ShouldMapToUpdate()
    {
        // Arrange
        var json = ParseJson("""{"op":"u","before":{"id":1,"name":"old"},"after":{"id":1,"name":"new"},"source":{"db":"testdb","table":"Orders"}}""");

        // Act
        var result = DebeziumEventMapper.MapEvent(json, DebeziumEventFormat.Flat, _logger);

        // Assert
        result.IsRight.ShouldBeTrue();
        var evt = (ChangeEvent)result;
        evt.Operation.ShouldBe(ChangeOperation.Update);
    }

    /// <summary>
    /// Verifies that op="d" (delete) maps to Delete operation.
    /// </summary>
    [Fact]
    public void MapEvent_Flat_OpDelete_ShouldMapToDelete()
    {
        // Arrange
        var json = ParseJson("""{"op":"d","before":{"id":1},"source":{"db":"testdb","table":"Orders"}}""");

        // Act
        var result = DebeziumEventMapper.MapEvent(json, DebeziumEventFormat.Flat, _logger);

        // Assert
        result.IsRight.ShouldBeTrue();
        var evt = (ChangeEvent)result;
        evt.Operation.ShouldBe(ChangeOperation.Delete);
    }

    /// <summary>
    /// Verifies that unknown op codes default to Insert.
    /// </summary>
    [Fact]
    public void MapEvent_Flat_UnknownOp_ShouldDefaultToInsert()
    {
        // Arrange
        var json = ParseJson("""{"op":"x","source":{"db":"testdb","table":"Orders"}}""");

        // Act
        var result = DebeziumEventMapper.MapEvent(json, DebeziumEventFormat.Flat, _logger);

        // Assert
        result.IsRight.ShouldBeTrue();
        var evt = (ChangeEvent)result;
        evt.Operation.ShouldBe(ChangeOperation.Insert);
    }

    #endregion

    #region CloudEvents Format

    /// <summary>
    /// Verifies that CloudEvents format extracts payload from the "data" property.
    /// </summary>
    [Fact]
    public void MapEvent_CloudEvents_WithDataProperty_ShouldExtractPayload()
    {
        // Arrange
        var json = ParseJson("""
        {
            "type": "io.debezium.connector.sqlserver.DataChangeEvent",
            "source": "/debezium/sqlserver",
            "data": {
                "op": "c",
                "after": {"id": 1},
                "source": {"db": "testdb", "schema": "dbo", "table": "Orders"}
            }
        }
        """);

        // Act
        var result = DebeziumEventMapper.MapEvent(json, DebeziumEventFormat.CloudEvents, _logger);

        // Assert
        result.IsRight.ShouldBeTrue();
        var evt = (ChangeEvent)result;
        evt.Operation.ShouldBe(ChangeOperation.Insert);
        evt.TableName.ShouldBe("dbo.Orders");
    }

    /// <summary>
    /// Verifies that CloudEvents format without "data" uses root element as payload.
    /// </summary>
    [Fact]
    public void MapEvent_CloudEvents_WithoutDataProperty_ShouldUseRootAsPayload()
    {
        // Arrange
        var json = ParseJson("""{"op":"u","before":{"id":1},"after":{"id":1},"source":{"db":"testdb","table":"Items"}}""");

        // Act
        var result = DebeziumEventMapper.MapEvent(json, DebeziumEventFormat.CloudEvents, _logger);

        // Assert
        result.IsRight.ShouldBeTrue();
        var evt = (ChangeEvent)result;
        evt.Operation.ShouldBe(ChangeOperation.Update);
    }

    #endregion

    #region Metadata Extraction

    /// <summary>
    /// Verifies that source db/schema/table are extracted correctly into metadata.
    /// </summary>
    [Fact]
    public void MapEvent_WithSourceMetadata_ShouldExtractDbSchemaTable()
    {
        // Arrange
        var json = ParseJson("""{"op":"c","after":{"id":1},"source":{"db":"mydb","schema":"public","table":"users"}}""");

        // Act
        var result = DebeziumEventMapper.MapEvent(json, DebeziumEventFormat.Flat, _logger);

        // Assert
        result.IsRight.ShouldBeTrue();
        var evt = (ChangeEvent)result;
        evt.Metadata.SourceDatabase.ShouldBe("mydb");
        evt.Metadata.SourceSchema.ShouldBe("public");
    }

    /// <summary>
    /// Verifies that table name includes schema prefix when schema is present.
    /// </summary>
    [Fact]
    public void MapEvent_WithSchema_ShouldPrefixTableNameWithSchema()
    {
        // Arrange
        var json = ParseJson("""{"op":"c","after":{"id":1},"source":{"db":"mydb","schema":"public","table":"users"}}""");

        // Act
        var result = DebeziumEventMapper.MapEvent(json, DebeziumEventFormat.Flat, _logger);

        // Assert
        result.IsRight.ShouldBeTrue();
        var evt = (ChangeEvent)result;
        evt.TableName.ShouldBe("public.users");
    }

    /// <summary>
    /// Verifies that table name is just the table when no schema is present.
    /// </summary>
    [Fact]
    public void MapEvent_WithoutSchema_ShouldUseJustTableName()
    {
        // Arrange
        var json = ParseJson("""{"op":"c","after":{"id":1},"source":{"db":"mydb","table":"users"}}""");

        // Act
        var result = DebeziumEventMapper.MapEvent(json, DebeziumEventFormat.Flat, _logger);

        // Assert
        result.IsRight.ShouldBeTrue();
        var evt = (ChangeEvent)result;
        evt.TableName.ShouldBe("users");
    }

    /// <summary>
    /// Verifies that missing source results in "unknown" table name.
    /// </summary>
    [Fact]
    public void MapEvent_NoSource_ShouldUseUnknownTableName()
    {
        // Arrange
        var json = ParseJson("""{"op":"c","after":{"id":1}}""");

        // Act
        var result = DebeziumEventMapper.MapEvent(json, DebeziumEventFormat.Flat, _logger);

        // Assert
        result.IsRight.ShouldBeTrue();
        var evt = (ChangeEvent)result;
        evt.TableName.ShouldBe("unknown");
    }

    #endregion

    #region Before/After Handling

    /// <summary>
    /// Verifies that insert events have after set and before null.
    /// </summary>
    [Fact]
    public void MapEvent_Insert_ShouldHaveAfterSet_BeforeNull()
    {
        // Arrange
        var json = ParseJson("""{"op":"c","after":{"id":1,"name":"test"},"source":{"table":"Orders"}}""");

        // Act
        var result = DebeziumEventMapper.MapEvent(json, DebeziumEventFormat.Flat, _logger);

        // Assert
        result.IsRight.ShouldBeTrue();
        var evt = (ChangeEvent)result;
        evt.After.ShouldNotBeNull();
        evt.Before.ShouldBeNull();
    }

    /// <summary>
    /// Verifies that update events have both before and after set.
    /// </summary>
    [Fact]
    public void MapEvent_Update_ShouldHaveBothBeforeAndAfter()
    {
        // Arrange
        var json = ParseJson("""{"op":"u","before":{"id":1,"name":"old"},"after":{"id":1,"name":"new"},"source":{"table":"Orders"}}""");

        // Act
        var result = DebeziumEventMapper.MapEvent(json, DebeziumEventFormat.Flat, _logger);

        // Assert
        result.IsRight.ShouldBeTrue();
        var evt = (ChangeEvent)result;
        evt.Before.ShouldNotBeNull();
        evt.After.ShouldNotBeNull();
    }

    /// <summary>
    /// Verifies that delete events have before set and after null.
    /// </summary>
    [Fact]
    public void MapEvent_Delete_ShouldHaveBeforeSet_AfterNull()
    {
        // Arrange
        var json = ParseJson("""{"op":"d","before":{"id":1,"name":"test"},"source":{"table":"Orders"}}""");

        // Act
        var result = DebeziumEventMapper.MapEvent(json, DebeziumEventFormat.Flat, _logger);

        // Assert
        result.IsRight.ShouldBeTrue();
        var evt = (ChangeEvent)result;
        evt.Before.ShouldNotBeNull();
        evt.After.ShouldBeNull();
    }

    /// <summary>
    /// Verifies that null before/after values in JSON result in null ChangeEvent fields.
    /// </summary>
    [Fact]
    public void MapEvent_NullBeforeAfter_ShouldResultInNullFields()
    {
        // Arrange
        var json = ParseJson("""{"op":"c","before":null,"after":null,"source":{"table":"Orders"}}""");

        // Act
        var result = DebeziumEventMapper.MapEvent(json, DebeziumEventFormat.Flat, _logger);

        // Assert
        result.IsRight.ShouldBeTrue();
        var evt = (ChangeEvent)result;
        evt.Before.ShouldBeNull();
        evt.After.ShouldBeNull();
    }

    #endregion

    #region Position Creation

    /// <summary>
    /// Verifies that when source is present, OffsetJson contains the source raw text.
    /// </summary>
    [Fact]
    public void MapEvent_WithSource_ShouldSetOffsetJsonToSourceRawText()
    {
        // Arrange
        var json = ParseJson("""{"op":"c","after":{"id":1},"source":{"db":"testdb","lsn":12345}}""");

        // Act
        var result = DebeziumEventMapper.MapEvent(json, DebeziumEventFormat.Flat, _logger);

        // Assert
        result.IsRight.ShouldBeTrue();
        var evt = (ChangeEvent)result;
        var position = evt.Metadata.Position.ShouldBeOfType<DebeziumCdcPosition>();
        position.OffsetJson.ShouldContain("\"lsn\"");
        position.OffsetJson.ShouldContain("12345");
    }

    /// <summary>
    /// Verifies that when source is absent, OffsetJson contains a fallback JSON.
    /// </summary>
    [Fact]
    public void MapEvent_WithoutSource_ShouldSetFallbackOffsetJson()
    {
        // Arrange
        var json = ParseJson("""{"op":"c","after":{"id":1}}""");

        // Act
        var result = DebeziumEventMapper.MapEvent(json, DebeziumEventFormat.Flat, _logger);

        // Assert
        result.IsRight.ShouldBeTrue();
        var evt = (ChangeEvent)result;
        var position = evt.Metadata.Position.ShouldBeOfType<DebeziumCdcPosition>();
        position.OffsetJson.ShouldContain("\"op\"");
    }

    #endregion

    #region Error Cases

    /// <summary>
    /// Verifies that missing op field returns a Left error.
    /// </summary>
    [Fact]
    public void MapEvent_MissingOpField_ShouldReturnLeftError()
    {
        // Arrange — no "op" field (e.g., schema change / DDL event)
        var json = ParseJson("""{"source":{"db":"testdb","table":"Orders"}}""");

        // Act
        var result = DebeziumEventMapper.MapEvent(json, DebeziumEventFormat.Flat, _logger);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that null op value returns a Left error.
    /// </summary>
    [Fact]
    public void MapEvent_NullOpValue_ShouldReturnLeftError()
    {
        // Arrange
        var json = ParseJson("""{"op":null,"source":{"table":"Orders"}}""");

        // Act
        var result = DebeziumEventMapper.MapEvent(json, DebeziumEventFormat.Flat, _logger);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that empty op string returns a Left error.
    /// </summary>
    [Fact]
    public void MapEvent_EmptyOpString_ShouldReturnLeftError()
    {
        // Arrange
        var json = ParseJson("""{"op":"","source":{"table":"Orders"}}""");

        // Act
        var result = DebeziumEventMapper.MapEvent(json, DebeziumEventFormat.Flat, _logger);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region Metadata Timestamp

    /// <summary>
    /// Verifies that the metadata timestamp is set to a recent UTC time.
    /// </summary>
    [Fact]
    public void MapEvent_ShouldSetMetadataTimestamp()
    {
        // Arrange
        var json = ParseJson("""{"op":"c","after":{"id":1},"source":{"table":"Orders"}}""");
        var before = DateTime.UtcNow;

        // Act
        var result = DebeziumEventMapper.MapEvent(json, DebeziumEventFormat.Flat, _logger);

        // Assert
        var after = DateTime.UtcNow;
        result.IsRight.ShouldBeTrue();
        var evt = (ChangeEvent)result;
        evt.Metadata.CapturedAtUtc.ShouldBeInRange(before, after);
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
