using System.Buffers.Binary;
using Encina.Cdc.Abstractions;
using Encina.Cdc.SqlServer;
using Shouldly;

namespace Encina.UnitTests.Cdc.SqlServer;

/// <summary>
/// Unit tests for <see cref="SqlServerCdcPosition"/>.
/// </summary>
public sealed class SqlServerCdcPositionTests
{
    #region Constructor

    [Fact]
    public void Constructor_SetsVersion()
    {
        var position = new SqlServerCdcPosition(42);

        position.Version.ShouldBe(42);
    }

    [Fact]
    public void Constructor_ZeroVersion_IsValid()
    {
        var position = new SqlServerCdcPosition(0);

        position.Version.ShouldBe(0);
    }

    [Fact]
    public void Constructor_NegativeVersion_IsValid()
    {
        var position = new SqlServerCdcPosition(-1);

        position.Version.ShouldBe(-1);
    }

    [Fact]
    public void Constructor_MaxVersion_IsValid()
    {
        var position = new SqlServerCdcPosition(long.MaxValue);

        position.Version.ShouldBe(long.MaxValue);
    }

    [Fact]
    public void Constructor_MinVersion_IsValid()
    {
        var position = new SqlServerCdcPosition(long.MinValue);

        position.Version.ShouldBe(long.MinValue);
    }

    #endregion

    #region ToBytes / FromBytes Round-Trip

    [Fact]
    public void ToBytes_Returns8Bytes()
    {
        var position = new SqlServerCdcPosition(100);

        var bytes = position.ToBytes();

        bytes.Length.ShouldBe(8);
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(1L)]
    [InlineData(42L)]
    [InlineData(-1L)]
    [InlineData(1_000_000L)]
    [InlineData(long.MaxValue)]
    [InlineData(long.MinValue)]
    public void FromBytes_ToBytes_RoundTrip_PreservesValue(long version)
    {
        var original = new SqlServerCdcPosition(version);

        var bytes = original.ToBytes();
        var restored = SqlServerCdcPosition.FromBytes(bytes);

        restored.Version.ShouldBe(version);
    }

    [Fact]
    public void ToBytes_UsesBigEndianEncoding()
    {
        var version = 0x0102030405060708L;
        var position = new SqlServerCdcPosition(version);

        var bytes = position.ToBytes();

        var readBack = BinaryPrimitives.ReadInt64BigEndian(bytes);
        readBack.ShouldBe(version);
    }

    #endregion

    #region FromBytes Validation

    [Fact]
    public void FromBytes_NullBytes_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            SqlServerCdcPosition.FromBytes(null!));
    }

    [Fact]
    public void FromBytes_TooFewBytes_ThrowsArgumentException()
    {
        var bytes = new byte[7];

        var ex = Should.Throw<ArgumentException>(() =>
            SqlServerCdcPosition.FromBytes(bytes));

        ex.ParamName.ShouldBe("bytes");
    }

    [Fact]
    public void FromBytes_TooManyBytes_ThrowsArgumentException()
    {
        var bytes = new byte[9];

        var ex = Should.Throw<ArgumentException>(() =>
            SqlServerCdcPosition.FromBytes(bytes));

        ex.ParamName.ShouldBe("bytes");
    }

    [Fact]
    public void FromBytes_EmptyBytes_ThrowsArgumentException()
    {
        var bytes = Array.Empty<byte>();

        Should.Throw<ArgumentException>(() =>
            SqlServerCdcPosition.FromBytes(bytes));
    }

    #endregion

    #region CompareTo

    [Fact]
    public void CompareTo_Null_ReturnsPositive()
    {
        var position = new SqlServerCdcPosition(10);

        position.CompareTo(null).ShouldBeGreaterThan(0);
    }

    [Fact]
    public void CompareTo_SameVersion_ReturnsZero()
    {
        var a = new SqlServerCdcPosition(42);
        var b = new SqlServerCdcPosition(42);

        a.CompareTo(b).ShouldBe(0);
    }

    [Fact]
    public void CompareTo_SmallerVersion_ReturnsPositive()
    {
        var a = new SqlServerCdcPosition(100);
        var b = new SqlServerCdcPosition(50);

        a.CompareTo(b).ShouldBeGreaterThan(0);
    }

    [Fact]
    public void CompareTo_LargerVersion_ReturnsNegative()
    {
        var a = new SqlServerCdcPosition(50);
        var b = new SqlServerCdcPosition(100);

        a.CompareTo(b).ShouldBeLessThan(0);
    }

    [Fact]
    public void CompareTo_DifferentType_ThrowsArgumentException()
    {
        var sqlPosition = new SqlServerCdcPosition(10);
        var otherPosition = new TestCdcPosition(10);

        Should.Throw<ArgumentException>(() =>
            sqlPosition.CompareTo(otherPosition));
    }

    #endregion

    #region ToString

    [Fact]
    public void ToString_ContainsCtVersionPrefix()
    {
        var position = new SqlServerCdcPosition(42);

        var result = position.ToString();

        result.ShouldStartWith("CT-Version:");
    }

    [Fact]
    public void ToString_ContainsVersionValue()
    {
        var position = new SqlServerCdcPosition(12345);

        var result = position.ToString();

        result.ShouldBe("CT-Version:12345");
    }

    [Fact]
    public void ToString_ZeroVersion()
    {
        var position = new SqlServerCdcPosition(0);

        position.ToString().ShouldBe("CT-Version:0");
    }

    [Fact]
    public void ToString_NegativeVersion()
    {
        var position = new SqlServerCdcPosition(-1);

        position.ToString().ShouldBe("CT-Version:-1");
    }

    #endregion
}
