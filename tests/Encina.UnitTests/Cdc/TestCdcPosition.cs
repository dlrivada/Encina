using System.Text;
using Encina.Cdc.Abstractions;

namespace Encina.UnitTests.Cdc;

/// <summary>
/// Concrete test implementation of <see cref="CdcPosition"/> for use in unit tests.
/// Uses a simple long value as the position.
/// </summary>
internal sealed class TestCdcPosition : CdcPosition
{
    public long Value { get; }

    public TestCdcPosition(long value)
    {
        Value = value;
    }

    public override byte[] ToBytes() => BitConverter.GetBytes(Value);

    public override int CompareTo(CdcPosition? other)
    {
        if (other is null) return 1;
        if (other is not TestCdcPosition otherPosition)
        {
            throw new ArgumentException(
                $"Cannot compare {GetType().Name} with {other.GetType().Name}",
                nameof(other));
        }
        return Value.CompareTo(otherPosition.Value);
    }

    public override string ToString() => $"TestPosition({Value})";

    public override bool Equals(object? obj) =>
        obj is TestCdcPosition other && Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();
}
