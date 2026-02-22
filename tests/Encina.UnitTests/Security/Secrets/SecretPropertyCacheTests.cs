using Encina.Security.Secrets;
using Encina.Security.Secrets.Injection;
using FluentAssertions;

namespace Encina.UnitTests.Security.Secrets;

public sealed class SecretPropertyCacheTests : IDisposable
{
    public void Dispose()
    {
        SecretPropertyCache.ClearCache();
    }

    #region Test Fixtures

    private sealed class PlainRequest
    {
        public string Name { get; set; } = "";
    }

    private sealed class SingleSecretRequest
    {
        [InjectSecret("my-secret")]
        public string ApiKey { get; set; } = "";
    }

    private sealed class MultiSecretRequest
    {
        [InjectSecret("api-key")]
        public string ApiKey { get; set; } = "";

        [InjectSecret("db-connection")]
        public string ConnectionString { get; set; } = "";
    }

    private sealed class NonStringSecretRequest
    {
        [InjectSecret("my-int-secret")]
        public int Value { get; set; }
    }

    private sealed class ReadOnlySecretRequest
    {
        [InjectSecret("readonly-secret")]
        public string ReadOnlyProp { get; } = "";
    }

    private sealed class VersionedSecretRequest
    {
        [InjectSecret("key", Version = "v2")]
        public string Secret { get; set; } = "";
    }

    #endregion

    #region GetProperties

    [Fact]
    public void GetProperties_TypeWithNoInjectAttributes_ReturnsEmptyArray()
    {
        var properties = SecretPropertyCache.GetProperties(typeof(PlainRequest));

        properties.Should().BeEmpty();
    }

    [Fact]
    public void GetProperties_TypeWithOneInjectAttribute_ReturnsOneEntry()
    {
        var properties = SecretPropertyCache.GetProperties(typeof(SingleSecretRequest));

        properties.Should().HaveCount(1);
        properties[0].Attribute.SecretName.Should().Be("my-secret");
        properties[0].Property.Name.Should().Be("ApiKey");
    }

    [Fact]
    public void GetProperties_TypeWithMultipleInjectAttributes_ReturnsAll()
    {
        var properties = SecretPropertyCache.GetProperties(typeof(MultiSecretRequest));

        properties.Should().HaveCount(2);
        properties.Select(p => p.Attribute.SecretName)
            .Should().BeEquivalentTo("api-key", "db-connection");
    }

    [Fact]
    public void GetProperties_NonStringProperty_IsSkipped()
    {
        var properties = SecretPropertyCache.GetProperties(typeof(NonStringSecretRequest));

        properties.Should().BeEmpty();
    }

    [Fact]
    public void GetProperties_ReadOnlyProperty_IsSkipped()
    {
        var properties = SecretPropertyCache.GetProperties(typeof(ReadOnlySecretRequest));

        properties.Should().BeEmpty();
    }

    [Fact]
    public void GetProperties_CachesResultOnSecondCall()
    {
        var first = SecretPropertyCache.GetProperties(typeof(SingleSecretRequest));
        var second = SecretPropertyCache.GetProperties(typeof(SingleSecretRequest));

        ReferenceEquals(first, second).Should().BeTrue();
    }

    #endregion

    #region Compiled Setter

    [Fact]
    public void CompiledSetter_SetsPropertyValue()
    {
        var properties = SecretPropertyCache.GetProperties(typeof(SingleSecretRequest));
        var request = new SingleSecretRequest();

        properties[0].SetValue(request, "injected-value");

        request.ApiKey.Should().Be("injected-value");
    }

    #endregion

    #region ClearCache

    [Fact]
    public void ClearCache_ResetsCache()
    {
        var first = SecretPropertyCache.GetProperties(typeof(SingleSecretRequest));

        SecretPropertyCache.ClearCache();

        var second = SecretPropertyCache.GetProperties(typeof(SingleSecretRequest));
        ReferenceEquals(first, second).Should().BeFalse();
    }

    #endregion

    #region Version Attribute

    [Fact]
    public void GetProperties_PreservesVersionAttribute()
    {
        var properties = SecretPropertyCache.GetProperties(typeof(VersionedSecretRequest));

        properties.Should().HaveCount(1);
        properties[0].Attribute.Version.Should().Be("v2");
    }

    #endregion
}
