using System.Reflection;
using System.Text.Json.Serialization;
using Encina.Security.PII;

namespace Encina.ContractTests.Security.PII;

/// <summary>
/// Contract of the Hash mode key configuration (#858): fail closed by default, secrets never serialized.
/// </summary>
public sealed class PIIHashKeyContractTests
{
    [Fact]
    public void PIIOptions_HashDefaults_FailClosed()
    {
        var options = new PIIOptions();

        options.HashKey.ShouldBeNull();
        options.AllowUnkeyedHash.ShouldBeFalse();
    }

    [Theory]
    [InlineData(typeof(PIIOptions))]
    [InlineData(typeof(MaskingOptions))]
    public void HashKey_IsMarkedJsonIgnore(Type type)
    {
        var property = type.GetProperty("HashKey", BindingFlags.Public | BindingFlags.Instance);

        property.ShouldNotBeNull();
        property.GetCustomAttribute<JsonIgnoreAttribute>().ShouldNotBeNull();
    }

    [Theory]
    [InlineData(typeof(PIIOptions))]
    [InlineData(typeof(MaskingOptions))]
    public void SecretCarryingOptions_OverrideToString(Type type)
    {
        var toString = type.GetMethod(
            nameof(ToString), BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly, Type.EmptyTypes);

        toString.ShouldNotBeNull();
    }

    [Fact]
    public void MaskingOptions_HasNoLegacyHashSaltMember()
    {
        typeof(MaskingOptions).GetProperty("HashSalt").ShouldBeNull();
    }
}
