using System.Buffers.Binary;
using Encina.Cdc.Abstractions;
using Encina.Cdc.PostgreSql;
using NpgsqlTypes;
using Shouldly;

namespace Encina.UnitTests.Cdc.PostgreSql;

/// <summary>
/// Unit tests for <see cref="PostgresCdcPosition"/>.
/// </summary>
public sealed class PostgresCdcPositionTests
{
    #region Constructor

    [Fact]
    public void Constructor_SetsLsn()
    {
        var lsn = new NpgsqlLogSequenceNumber(42UL);

        var position = new PostgresCdcPosition(lsn);

        position.Lsn.ShouldBe(lsn);
    }

    [Fact]
    public void Constructor_ZeroLsn_IsValid()
    {
        var lsn = new NpgsqlLogSequenceNumber(0UL);

        var position = new PostgresCdcPosition(lsn);

        position.Lsn.ShouldBe(lsn);
    }

    [Fact]
    public void Constructor_MaxLsn_IsValid()
    {
        var lsn = new NpgsqlLogSequenceNumber(ulong.MaxValue);

        var position = new PostgresCdcPosition(lsn);

        position.Lsn.ShouldBe(lsn);
    }

    #endregion

    #region ToBytes / FromBytes Round-Trip

    [Fact]
    public void ToBytes_Returns8Bytes()
    {
        var position = new PostgresCdcPosition(new NpgsqlLogSequenceNumber(100UL));

        var bytes = position.ToBytes();

        bytes.Length.ShouldBe(8);
    }

    [Theory]
    [InlineData(0UL)]
    [InlineData(1UL)]
    [InlineData(42UL)]
    [InlineData(1_000_000UL)]
    [InlineData(ulong.MaxValue)]
    public void FromBytes_ToBytes_RoundTrip_PreservesValue(ulong lsnValue)
    {
        var original = new PostgresCdcPosition(new NpgsqlLogSequenceNumber(lsnValue));

        var bytes = original.ToBytes();
        var restored = PostgresCdcPosition.FromBytes(bytes);

        ((ulong)restored.Lsn).ShouldBe(lsnValue);
    }

    [Fact]
    public void ToBytes_UsesBigEndianEncoding()
    {
        var lsnValue = 0x0102030405060708UL;
        var position = new PostgresCdcPosition(new NpgsqlLogSequenceNumber(lsnValue));

        var bytes = position.ToBytes();

        var readBack = BinaryPrimitives.ReadUInt64BigEndian(bytes);
        readBack.ShouldBe(lsnValue);
    }

    #endregion

    #region FromBytes Validation

    [Fact]
    public void FromBytes_NullBytes_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            PostgresCdcPosition.FromBytes(null!));
    }

    [Fact]
    public void FromBytes_TooFewBytes_ThrowsArgumentException()
    {
        var bytes = new byte[7];

        var ex = Should.Throw<ArgumentException>(() =>
            PostgresCdcPosition.FromBytes(bytes));

        ex.ParamName.ShouldBe("bytes");
    }

    [Fact]
    public void FromBytes_TooManyBytes_ThrowsArgumentException()
    {
        var bytes = new byte[9];

        var ex = Should.Throw<ArgumentException>(() =>
            PostgresCdcPosition.FromBytes(bytes));

        ex.ParamName.ShouldBe("bytes");
    }

    [Fact]
    public void FromBytes_EmptyBytes_ThrowsArgumentException()
    {
        var bytes = Array.Empty<byte>();

        Should.Throw<ArgumentException>(() =>
            PostgresCdcPosition.FromBytes(bytes));
    }

    #endregion

    #region CompareTo

    [Fact]
    public void CompareTo_Null_ReturnsPositive()
    {
        var position = new PostgresCdcPosition(new NpgsqlLogSequenceNumber(10UL));

        position.CompareTo(null).ShouldBeGreaterThan(0);
    }

    [Fact]
    public void CompareTo_SameLsn_ReturnsZero()
    {
        var a = new PostgresCdcPosition(new NpgsqlLogSequenceNumber(42UL));
        var b = new PostgresCdcPosition(new NpgsqlLogSequenceNumber(42UL));

        a.CompareTo(b).ShouldBe(0);
    }

    [Fact]
    public void CompareTo_SmallerLsn_ReturnsPositive()
    {
        var a = new PostgresCdcPosition(new NpgsqlLogSequenceNumber(100UL));
        var b = new PostgresCdcPosition(new NpgsqlLogSequenceNumber(50UL));

        a.CompareTo(b).ShouldBeGreaterThan(0);
    }

    [Fact]
    public void CompareTo_LargerLsn_ReturnsNegative()
    {
        var a = new PostgresCdcPosition(new NpgsqlLogSequenceNumber(50UL));
        var b = new PostgresCdcPosition(new NpgsqlLogSequenceNumber(100UL));

        a.CompareTo(b).ShouldBeLessThan(0);
    }

    [Fact]
    public void CompareTo_DifferentType_ThrowsArgumentException()
    {
        var pgPosition = new PostgresCdcPosition(new NpgsqlLogSequenceNumber(10UL));
        var otherPosition = new TestCdcPosition(10);

        Should.Throw<ArgumentException>(() =>
            pgPosition.CompareTo(otherPosition));
    }

    #endregion

    #region ToString

    [Fact]
    public void ToString_ContainsLsnPrefix()
    {
        var position = new PostgresCdcPosition(new NpgsqlLogSequenceNumber(42UL));

        var result = position.ToString();

        result.ShouldStartWith("LSN:");
    }

    [Fact]
    public void ToString_ContainsLsnValue()
    {
        var lsn = new NpgsqlLogSequenceNumber(12345UL);
        var position = new PostgresCdcPosition(lsn);

        var result = position.ToString();

        result.ShouldBe($"LSN:{lsn}");
    }

    #endregion
}
