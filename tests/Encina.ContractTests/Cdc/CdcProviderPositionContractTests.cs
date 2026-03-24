using Encina.Cdc.Abstractions;
using Encina.Cdc.MongoDb;
using Encina.Cdc.MySql;
using Encina.Cdc.PostgreSql;
using Encina.Cdc.SqlServer;
using MongoDB.Bson;
using NpgsqlTypes;

namespace Encina.ContractTests.Cdc;

/// <summary>
/// Contract tests verifying that all provider-specific <see cref="CdcPosition"/> subclasses
/// correctly satisfy the base class contract for serialization round-trips via
/// <see cref="CdcPosition.ToBytes"/> and static <c>FromBytes</c>, comparison ordering via
/// <see cref="CdcPosition.CompareTo"/>, and meaningful string representation via
/// <see cref="CdcPosition.ToString"/>.
/// Covers: <see cref="PostgresCdcPosition"/>, <see cref="MySqlCdcPosition"/> (GTID and Binlog modes),
/// <see cref="SqlServerCdcPosition"/>, and <see cref="MongoCdcPosition"/>.
/// </summary>
[Trait("Category", "Contract")]
public sealed class CdcProviderPositionContractTests
{
    #region PostgresCdcPosition Contract

    /// <summary>
    /// Contract: <see cref="PostgresCdcPosition.FromBytes"/> must round-trip with <see cref="PostgresCdcPosition.ToBytes"/>.
    /// </summary>
    [Fact]
    public void Contract_PostgresCdcPosition_FromBytesToBytes_RoundTrips()
    {
        // Arrange
        var original = new PostgresCdcPosition(new NpgsqlLogSequenceNumber(12345678UL));

        // Act
        var bytes = original.ToBytes();
        var restored = PostgresCdcPosition.FromBytes(bytes);

        // Assert
        restored.Lsn.ShouldBe(original.Lsn,
            "PostgresCdcPosition must round-trip through ToBytes/FromBytes preserving the LSN value");
    }

    /// <summary>
    /// Contract: <see cref="PostgresCdcPosition.CompareTo"/> must order positions by LSN.
    /// </summary>
    [Fact]
    public void Contract_PostgresCdcPosition_CompareTo_OrdersByLsn()
    {
        // Arrange
        var earlier = new PostgresCdcPosition(new NpgsqlLogSequenceNumber(100UL));
        var later = new PostgresCdcPosition(new NpgsqlLogSequenceNumber(200UL));

        // Act & Assert
        earlier.CompareTo(later).ShouldBeLessThan(0,
            "Earlier LSN must compare as less than later LSN");
        later.CompareTo(earlier).ShouldBeGreaterThan(0,
            "Later LSN must compare as greater than earlier LSN");
    }

    /// <summary>
    /// Contract: Equal LSN values must compare as zero.
    /// </summary>
    [Fact]
    public void Contract_PostgresCdcPosition_CompareTo_EqualLsn_ReturnsZero()
    {
        // Arrange
        var a = new PostgresCdcPosition(new NpgsqlLogSequenceNumber(999UL));
        var b = new PostgresCdcPosition(new NpgsqlLogSequenceNumber(999UL));

        // Act
        var result = a.CompareTo(b);

        // Assert
        result.ShouldBe(0,
            "Two PostgresCdcPositions with the same LSN must compare as equal");
    }

    /// <summary>
    /// Contract: <see cref="PostgresCdcPosition.CompareTo"/> with null must return positive.
    /// </summary>
    [Fact]
    public void Contract_PostgresCdcPosition_CompareTo_Null_ReturnsPositive()
    {
        // Arrange
        var position = new PostgresCdcPosition(new NpgsqlLogSequenceNumber(1UL));

        // Act
        var result = position.CompareTo(null);

        // Assert
        result.ShouldBeGreaterThan(0,
            "CompareTo(null) must return positive for PostgresCdcPosition");
    }

    /// <summary>
    /// Contract: <see cref="PostgresCdcPosition.ToString"/> must include LSN information.
    /// </summary>
    [Fact]
    public void Contract_PostgresCdcPosition_ToString_ContainsLsnInfo()
    {
        // Arrange
        var position = new PostgresCdcPosition(new NpgsqlLogSequenceNumber(42UL));

        // Act
        var result = position.ToString();

        // Assert
        result.ShouldNotBeNullOrWhiteSpace(
            "PostgresCdcPosition.ToString must return a meaningful string");
        result.ShouldContain("LSN");
    }

    /// <summary>
    /// Contract: <see cref="PostgresCdcPosition.FromBytes"/> with wrong byte count must throw.
    /// </summary>
    [Fact]
    public void Contract_PostgresCdcPosition_FromBytes_WrongLength_Throws()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => PostgresCdcPosition.FromBytes(new byte[4]))
            .Message.ShouldContain("8 bytes");
    }

    #endregion

    #region MySqlCdcPosition Contract (GTID Mode)

    /// <summary>
    /// Contract: <see cref="MySqlCdcPosition.FromBytes"/> must round-trip with
    /// <see cref="MySqlCdcPosition.ToBytes"/> in GTID mode.
    /// </summary>
    [Fact]
    public void Contract_MySqlCdcPosition_Gtid_FromBytesToBytes_RoundTrips()
    {
        // Arrange
        var original = new MySqlCdcPosition("3E11FA47-71CA-11E1-9E33-C80AA9429562:1-5");

        // Act
        var bytes = original.ToBytes();
        var restored = MySqlCdcPosition.FromBytes(bytes);

        // Assert
        restored.GtidSet.ShouldBe(original.GtidSet,
            "MySqlCdcPosition (GTID) must round-trip through ToBytes/FromBytes preserving the GTID set");
    }

    /// <summary>
    /// Contract: <see cref="MySqlCdcPosition.CompareTo"/> must order GTID positions.
    /// </summary>
    [Fact]
    public void Contract_MySqlCdcPosition_Gtid_CompareTo_OrdersByGtid()
    {
        // Arrange
        var earlier = new MySqlCdcPosition("3E11FA47-71CA-11E1-9E33-C80AA9429562:1-5");
        var later = new MySqlCdcPosition("3E11FA47-71CA-11E1-9E33-C80AA9429562:1-9");

        // Act & Assert
        earlier.CompareTo(later).ShouldBeLessThan(0,
            "Earlier GTID must compare as less than later GTID");
        later.CompareTo(earlier).ShouldBeGreaterThan(0,
            "Later GTID must compare as greater than earlier GTID");
    }

    /// <summary>
    /// Contract: <see cref="MySqlCdcPosition.ToString"/> must contain GTID info.
    /// </summary>
    [Fact]
    public void Contract_MySqlCdcPosition_Gtid_ToString_ContainsGtidInfo()
    {
        // Arrange
        var position = new MySqlCdcPosition("3E11FA47-71CA-11E1-9E33-C80AA9429562:1-5");

        // Act
        var result = position.ToString();

        // Assert
        result.ShouldNotBeNullOrWhiteSpace(
            "MySqlCdcPosition (GTID) ToString must return a meaningful string");
        result.ShouldContain("GTID");
    }

    #endregion

    #region MySqlCdcPosition Contract (Binlog Mode)

    /// <summary>
    /// Contract: <see cref="MySqlCdcPosition.FromBytes"/> must round-trip with
    /// <see cref="MySqlCdcPosition.ToBytes"/> in Binlog file/position mode.
    /// </summary>
    [Fact]
    public void Contract_MySqlCdcPosition_Binlog_FromBytesToBytes_RoundTrips()
    {
        // Arrange
        var original = new MySqlCdcPosition("mysql-bin.000003", 4567);

        // Act
        var bytes = original.ToBytes();
        var restored = MySqlCdcPosition.FromBytes(bytes);

        // Assert
        restored.BinlogFileName.ShouldBe(original.BinlogFileName,
            "MySqlCdcPosition (Binlog) must round-trip preserving the binlog file name");
        restored.BinlogPosition.ShouldBe(original.BinlogPosition,
            "MySqlCdcPosition (Binlog) must round-trip preserving the binlog position");
    }

    /// <summary>
    /// Contract: <see cref="MySqlCdcPosition.CompareTo"/> must order binlog positions by file then position.
    /// </summary>
    [Fact]
    public void Contract_MySqlCdcPosition_Binlog_CompareTo_OrdersByFileAndPosition()
    {
        // Arrange
        var earlier = new MySqlCdcPosition("mysql-bin.000003", 100);
        var later = new MySqlCdcPosition("mysql-bin.000003", 200);

        // Act & Assert
        earlier.CompareTo(later).ShouldBeLessThan(0,
            "Earlier binlog position must compare as less than later binlog position");
        later.CompareTo(earlier).ShouldBeGreaterThan(0,
            "Later binlog position must compare as greater than earlier binlog position");
    }

    /// <summary>
    /// Contract: <see cref="MySqlCdcPosition.CompareTo"/> must order by file name first.
    /// </summary>
    [Fact]
    public void Contract_MySqlCdcPosition_Binlog_CompareTo_OrdersByFileName()
    {
        // Arrange
        var earlier = new MySqlCdcPosition("mysql-bin.000003", 9999);
        var later = new MySqlCdcPosition("mysql-bin.000004", 1);

        // Act & Assert
        earlier.CompareTo(later).ShouldBeLessThan(0,
            "Position in earlier binlog file must compare as less regardless of position offset");
    }

    /// <summary>
    /// Contract: <see cref="MySqlCdcPosition.ToString"/> must contain binlog file info.
    /// </summary>
    [Fact]
    public void Contract_MySqlCdcPosition_Binlog_ToString_ContainsBinlogInfo()
    {
        // Arrange
        var position = new MySqlCdcPosition("mysql-bin.000003", 4567);

        // Act
        var result = position.ToString();

        // Assert
        result.ShouldNotBeNullOrWhiteSpace(
            "MySqlCdcPosition (Binlog) ToString must return a meaningful string");
        result.ShouldContain("Binlog");
    }

    /// <summary>
    /// Contract: <see cref="MySqlCdcPosition.CompareTo"/> with null must return positive.
    /// </summary>
    [Fact]
    public void Contract_MySqlCdcPosition_CompareTo_Null_ReturnsPositive()
    {
        // Arrange
        var position = new MySqlCdcPosition("mysql-bin.000001", 1);

        // Act
        var result = position.CompareTo(null);

        // Assert
        result.ShouldBeGreaterThan(0,
            "CompareTo(null) must return positive for MySqlCdcPosition");
    }

    #endregion

    #region SqlServerCdcPosition Contract

    /// <summary>
    /// Contract: <see cref="SqlServerCdcPosition.FromBytes"/> must round-trip with
    /// <see cref="SqlServerCdcPosition.ToBytes"/>.
    /// </summary>
    [Fact]
    public void Contract_SqlServerCdcPosition_FromBytesToBytes_RoundTrips()
    {
        // Arrange
        var original = new SqlServerCdcPosition(98765);

        // Act
        var bytes = original.ToBytes();
        var restored = SqlServerCdcPosition.FromBytes(bytes);

        // Assert
        restored.Version.ShouldBe(original.Version,
            "SqlServerCdcPosition must round-trip through ToBytes/FromBytes preserving the version");
    }

    /// <summary>
    /// Contract: <see cref="SqlServerCdcPosition.CompareTo"/> must order positions by version.
    /// </summary>
    [Fact]
    public void Contract_SqlServerCdcPosition_CompareTo_OrdersByVersion()
    {
        // Arrange
        var earlier = new SqlServerCdcPosition(10);
        var later = new SqlServerCdcPosition(20);

        // Act & Assert
        earlier.CompareTo(later).ShouldBeLessThan(0,
            "Earlier version must compare as less than later version");
        later.CompareTo(earlier).ShouldBeGreaterThan(0,
            "Later version must compare as greater than earlier version");
    }

    /// <summary>
    /// Contract: Equal version values must compare as zero.
    /// </summary>
    [Fact]
    public void Contract_SqlServerCdcPosition_CompareTo_EqualVersion_ReturnsZero()
    {
        // Arrange
        var a = new SqlServerCdcPosition(42);
        var b = new SqlServerCdcPosition(42);

        // Act
        var result = a.CompareTo(b);

        // Assert
        result.ShouldBe(0,
            "Two SqlServerCdcPositions with the same version must compare as equal");
    }

    /// <summary>
    /// Contract: <see cref="SqlServerCdcPosition.CompareTo"/> with null must return positive.
    /// </summary>
    [Fact]
    public void Contract_SqlServerCdcPosition_CompareTo_Null_ReturnsPositive()
    {
        // Arrange
        var position = new SqlServerCdcPosition(1);

        // Act
        var result = position.CompareTo(null);

        // Assert
        result.ShouldBeGreaterThan(0,
            "CompareTo(null) must return positive for SqlServerCdcPosition");
    }

    /// <summary>
    /// Contract: <see cref="SqlServerCdcPosition.ToString"/> must contain version info.
    /// </summary>
    [Fact]
    public void Contract_SqlServerCdcPosition_ToString_ContainsVersionInfo()
    {
        // Arrange
        var position = new SqlServerCdcPosition(42);

        // Act
        var result = position.ToString();

        // Assert
        result.ShouldNotBeNullOrWhiteSpace(
            "SqlServerCdcPosition.ToString must return a meaningful string");
        result.ShouldContain("42");
    }

    /// <summary>
    /// Contract: <see cref="SqlServerCdcPosition.FromBytes"/> with wrong byte count must throw.
    /// </summary>
    [Fact]
    public void Contract_SqlServerCdcPosition_FromBytes_WrongLength_Throws()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => SqlServerCdcPosition.FromBytes(new byte[4]))
            .Message.ShouldContain("8 bytes");
    }

    #endregion

    #region MongoCdcPosition Contract

    /// <summary>
    /// Contract: <see cref="MongoCdcPosition.FromBytes"/> must round-trip with
    /// <see cref="MongoCdcPosition.ToBytes"/>.
    /// </summary>
    [Fact]
    public void Contract_MongoCdcPosition_FromBytesToBytes_RoundTrips()
    {
        // Arrange
        var token = new BsonDocument
        {
            { "_data", "8263ABC123" },
            { "ns", new BsonDocument { { "db", "testdb" }, { "coll", "orders" } } }
        };
        var original = new MongoCdcPosition(token);

        // Act
        var bytes = original.ToBytes();
        var restored = MongoCdcPosition.FromBytes(bytes);

        // Assert
        restored.ResumeToken.ShouldBe(original.ResumeToken,
            "MongoCdcPosition must round-trip through ToBytes/FromBytes preserving the resume token");
    }

    /// <summary>
    /// Contract: <see cref="MongoCdcPosition.CompareTo"/> with null must return positive.
    /// </summary>
    [Fact]
    public void Contract_MongoCdcPosition_CompareTo_Null_ReturnsPositive()
    {
        // Arrange
        var token = new BsonDocument { { "_data", "token123" } };
        var position = new MongoCdcPosition(token);

        // Act
        var result = position.CompareTo(null);

        // Assert
        result.ShouldBeGreaterThan(0,
            "CompareTo(null) must return positive for MongoCdcPosition");
    }

    /// <summary>
    /// Contract: <see cref="MongoCdcPosition.CompareTo"/> with the same token must return zero.
    /// </summary>
    [Fact]
    public void Contract_MongoCdcPosition_CompareTo_EqualToken_ReturnsZero()
    {
        // Arrange
        var token1 = new BsonDocument { { "_data", "abc123" } };
        var token2 = new BsonDocument { { "_data", "abc123" } };
        var a = new MongoCdcPosition(token1);
        var b = new MongoCdcPosition(token2);

        // Act
        var result = a.CompareTo(b);

        // Assert
        result.ShouldBe(0,
            "Two MongoCdcPositions with identical resume tokens must compare as equal");
    }

    /// <summary>
    /// Contract: <see cref="MongoCdcPosition.ToString"/> must contain resume token info.
    /// </summary>
    [Fact]
    public void Contract_MongoCdcPosition_ToString_ContainsResumeTokenInfo()
    {
        // Arrange
        var token = new BsonDocument { { "_data", "mytoken" } };
        var position = new MongoCdcPosition(token);

        // Act
        var result = position.ToString();

        // Assert
        result.ShouldNotBeNullOrWhiteSpace(
            "MongoCdcPosition.ToString must return a meaningful string");
        result.ShouldContain("ResumeToken");
    }

    #endregion

    #region Cross-Type Comparison Contract

    /// <summary>
    /// Contract: Comparing positions of different concrete types must throw <see cref="ArgumentException"/>.
    /// This ensures type safety across provider boundaries.
    /// </summary>
    [Fact]
    public void Contract_CrossTypeComparison_Throws_ArgumentException()
    {
        // Arrange
        var postgres = new PostgresCdcPosition(new NpgsqlLogSequenceNumber(1UL));
        var sqlServer = new SqlServerCdcPosition(1);

        // Act & Assert
        Should.Throw<ArgumentException>(() => postgres.CompareTo(sqlServer));
    }

    /// <summary>
    /// Contract: All provider position subclasses must be sealed.
    /// </summary>
    [Theory]
    [InlineData(typeof(PostgresCdcPosition))]
    [InlineData(typeof(MySqlCdcPosition))]
    [InlineData(typeof(SqlServerCdcPosition))]
    [InlineData(typeof(MongoCdcPosition))]
    public void Contract_AllProviderPositions_AreSealed(Type positionType)
    {
        positionType.IsSealed.ShouldBeTrue(
            $"{positionType.Name} must be sealed to prevent inheritance");
    }

    /// <summary>
    /// Contract: All provider position subclasses must inherit from <see cref="CdcPosition"/>.
    /// </summary>
    [Theory]
    [InlineData(typeof(PostgresCdcPosition))]
    [InlineData(typeof(MySqlCdcPosition))]
    [InlineData(typeof(SqlServerCdcPosition))]
    [InlineData(typeof(MongoCdcPosition))]
    public void Contract_AllProviderPositions_InheritFromCdcPosition(Type positionType)
    {
        positionType.IsSubclassOf(typeof(CdcPosition)).ShouldBeTrue(
            $"{positionType.Name} must inherit from CdcPosition");
    }

    /// <summary>
    /// Contract: All provider position subclasses must have a static <c>FromBytes</c> method.
    /// </summary>
    [Theory]
    [InlineData(typeof(PostgresCdcPosition))]
    [InlineData(typeof(MySqlCdcPosition))]
    [InlineData(typeof(SqlServerCdcPosition))]
    [InlineData(typeof(MongoCdcPosition))]
    public void Contract_AllProviderPositions_HaveStaticFromBytes(Type positionType)
    {
        var fromBytes = positionType.GetMethod("FromBytes",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

        fromBytes.ShouldNotBeNull(
            $"{positionType.Name} must have a public static FromBytes method for deserialization");
        fromBytes.ReturnType.ShouldBe(positionType,
            $"{positionType.Name}.FromBytes must return the concrete position type");
    }

    #endregion
}
