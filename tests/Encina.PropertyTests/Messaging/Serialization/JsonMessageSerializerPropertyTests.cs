using Encina.Messaging.Serialization;
using FsCheck;
using FsCheck.Xunit;
using Shouldly;

namespace Encina.PropertyTests.Messaging.Serialization;

/// <summary>
/// Property-based tests for <see cref="JsonMessageSerializer"/> round-trip invariants.
/// </summary>
[Trait("Category", "Property")]
public sealed class JsonMessageSerializerPropertyTests
{
    [Property(MaxTest = 50)]
    public bool Serialize_Deserialize_RoundTrip_String(NonEmptyString value)
    {
        var serializer = new JsonMessageSerializer();
        var json = serializer.Serialize(value.Get);
        var result = serializer.Deserialize<string>(json);
        return result == value.Get;
    }

    [Property(MaxTest = 50)]
    public bool Serialize_Deserialize_RoundTrip_Int(int value)
    {
        var serializer = new JsonMessageSerializer();
        var json = serializer.Serialize(value);
        var result = serializer.Deserialize<int>(json);
        return result == value;
    }

    [Property(MaxTest = 50)]
    public bool Serialize_Deserialize_RoundTrip_Record(NonEmptyString name, int age)
    {
        var serializer = new JsonMessageSerializer();
        var original = new TestRecord(name.Get, age);
        var json = serializer.Serialize(original);
        var result = serializer.Deserialize<TestRecord>(json);
        return result != null && result.Name == original.Name && result.Age == original.Age;
    }

    [Property(MaxTest = 50)]
    public bool Deserialize_WithType_RoundTrip(NonEmptyString value)
    {
        var serializer = new JsonMessageSerializer();
        var json = serializer.Serialize(value.Get);
#pragma warning disable CA2263 // Testing the non-generic overload intentionally
        var result = serializer.Deserialize(json, typeof(string));
#pragma warning restore CA2263
        return result is string s && s == value.Get;
    }

    public sealed record TestRecord(string Name, int Age);
}
