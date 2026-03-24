using System.Text;
using System.Text.Json;
using Encina.Cdc.Abstractions;
using Encina.Cdc.MySql;
using Shouldly;

namespace Encina.UnitTests.Cdc.MySql;

/// <summary>
/// Unit tests for <see cref="MySqlCdcPosition"/>.
/// </summary>
public sealed class MySqlCdcPositionTests
{
    #region Constructor - GTID Mode

    [Fact]
    public void Constructor_Gtid_SetsGtidSet()
    {
        var gtid = "3E11FA47-71CA-11E1-9E33-C80AA9429562:1-5";

        var position = new MySqlCdcPosition(gtid);

        position.GtidSet.ShouldBe(gtid);
        position.BinlogFileName.ShouldBeNull();
        position.BinlogPosition.ShouldBe(0);
    }

    [Fact]
    public void Constructor_Gtid_NullValue_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() =>
            new MySqlCdcPosition((string)null!));
    }

    [Fact]
    public void Constructor_Gtid_EmptyValue_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() =>
            new MySqlCdcPosition(string.Empty));
    }

    [Fact]
    public void Constructor_Gtid_WhitespaceValue_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() =>
            new MySqlCdcPosition("   "));
    }

    #endregion

    #region Constructor - File/Position Mode

    [Fact]
    public void Constructor_FilePosition_SetsProperties()
    {
        var position = new MySqlCdcPosition("mysql-bin.000003", 154);

        position.BinlogFileName.ShouldBe("mysql-bin.000003");
        position.BinlogPosition.ShouldBe(154);
        position.GtidSet.ShouldBeNull();
    }

    [Fact]
    public void Constructor_FilePosition_NullFileName_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() =>
            new MySqlCdcPosition(null!, 100));
    }

    [Fact]
    public void Constructor_FilePosition_EmptyFileName_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() =>
            new MySqlCdcPosition(string.Empty, 100));
    }

    [Fact]
    public void Constructor_FilePosition_WhitespaceFileName_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() =>
            new MySqlCdcPosition("   ", 100));
    }

    [Fact]
    public void Constructor_FilePosition_ZeroPosition_IsValid()
    {
        var position = new MySqlCdcPosition("mysql-bin.000001", 0);

        position.BinlogPosition.ShouldBe(0);
    }

    #endregion

    #region ToBytes / FromBytes Round-Trip - GTID Mode

    [Fact]
    public void FromBytes_ToBytes_RoundTrip_GtidMode_PreservesValue()
    {
        var gtid = "3E11FA47-71CA-11E1-9E33-C80AA9429562:1-5";
        var original = new MySqlCdcPosition(gtid);

        var bytes = original.ToBytes();
        var restored = MySqlCdcPosition.FromBytes(bytes);

        restored.GtidSet.ShouldBe(gtid);
    }

    [Fact]
    public void ToBytes_GtidMode_ProducesValidJson()
    {
        var position = new MySqlCdcPosition("abc:1-3");

        var bytes = position.ToBytes();
        var json = Encoding.UTF8.GetString(bytes);

        var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("gtid").GetString().ShouldBe("abc:1-3");
    }

    #endregion

    #region ToBytes / FromBytes Round-Trip - File/Position Mode

    [Fact]
    public void FromBytes_ToBytes_RoundTrip_FilePositionMode_PreservesValue()
    {
        var original = new MySqlCdcPosition("mysql-bin.000003", 12345);

        var bytes = original.ToBytes();
        var restored = MySqlCdcPosition.FromBytes(bytes);

        restored.BinlogFileName.ShouldBe("mysql-bin.000003");
        restored.BinlogPosition.ShouldBe(12345);
        restored.GtidSet.ShouldBeNull();
    }

    [Fact]
    public void ToBytes_FilePositionMode_ProducesValidJson()
    {
        var position = new MySqlCdcPosition("mysql-bin.000001", 4);

        var bytes = position.ToBytes();
        var json = Encoding.UTF8.GetString(bytes);

        var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("file").GetString().ShouldBe("mysql-bin.000001");
        doc.RootElement.GetProperty("pos").GetInt64().ShouldBe(4);
    }

    #endregion

    #region FromBytes Validation

    [Fact]
    public void FromBytes_NullBytes_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            MySqlCdcPosition.FromBytes(null!));
    }

    [Fact]
    public void FromBytes_InvalidJson_ThrowsJsonException()
    {
        var bytes = Encoding.UTF8.GetBytes("not json");

        Should.Throw<JsonException>(() =>
            MySqlCdcPosition.FromBytes(bytes));
    }

    #endregion

    #region CompareTo

    [Fact]
    public void CompareTo_Null_ReturnsPositive()
    {
        var position = new MySqlCdcPosition("abc:1-5");

        position.CompareTo(null).ShouldBeGreaterThan(0);
    }

    [Fact]
    public void CompareTo_SameGtid_ReturnsZero()
    {
        var a = new MySqlCdcPosition("abc:1-5");
        var b = new MySqlCdcPosition("abc:1-5");

        a.CompareTo(b).ShouldBe(0);
    }

    [Fact]
    public void CompareTo_Gtid_OrdinalComparison()
    {
        var a = new MySqlCdcPosition("abc:1-5");
        var b = new MySqlCdcPosition("abd:1-5");

        a.CompareTo(b).ShouldBeLessThan(0);
        b.CompareTo(a).ShouldBeGreaterThan(0);
    }

    [Fact]
    public void CompareTo_SameFile_ComparesPosition()
    {
        var a = new MySqlCdcPosition("mysql-bin.000001", 100);
        var b = new MySqlCdcPosition("mysql-bin.000001", 200);

        a.CompareTo(b).ShouldBeLessThan(0);
        b.CompareTo(a).ShouldBeGreaterThan(0);
    }

    [Fact]
    public void CompareTo_DifferentFile_ComparesFileName()
    {
        var a = new MySqlCdcPosition("mysql-bin.000001", 999);
        var b = new MySqlCdcPosition("mysql-bin.000002", 1);

        a.CompareTo(b).ShouldBeLessThan(0);
    }

    [Fact]
    public void CompareTo_SameFileAndPosition_ReturnsZero()
    {
        var a = new MySqlCdcPosition("mysql-bin.000001", 100);
        var b = new MySqlCdcPosition("mysql-bin.000001", 100);

        a.CompareTo(b).ShouldBe(0);
    }

    [Fact]
    public void CompareTo_GtidVsFilePosition_ReturnsZero()
    {
        // When one is GTID and the other is file/position, comparison returns 0
        var gtid = new MySqlCdcPosition("abc:1-5");
        var filePosBytes = new MySqlCdcPosition("mysql-bin.000001", 100);

        gtid.CompareTo(filePosBytes).ShouldBe(0);
    }

    [Fact]
    public void CompareTo_DifferentType_ThrowsArgumentException()
    {
        var mysqlPosition = new MySqlCdcPosition("abc:1-5");
        var otherPosition = new TestCdcPosition(10);

        Should.Throw<ArgumentException>(() =>
            mysqlPosition.CompareTo(otherPosition));
    }

    #endregion

    #region ToString

    [Fact]
    public void ToString_GtidMode_ContainsGtidPrefix()
    {
        var position = new MySqlCdcPosition("abc:1-5");

        position.ToString().ShouldBe("GTID:abc:1-5");
    }

    [Fact]
    public void ToString_FilePositionMode_ContainsBinlogPrefix()
    {
        var position = new MySqlCdcPosition("mysql-bin.000003", 154);

        position.ToString().ShouldBe("Binlog:mysql-bin.000003:154");
    }

    #endregion
}
