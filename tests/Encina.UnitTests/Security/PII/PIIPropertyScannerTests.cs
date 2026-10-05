using Encina.Security.PII;
using Encina.Security.PII.Attributes;
using Encina.Security.PII.Internal;

namespace Encina.UnitTests.Security.PII;

public sealed class PIIPropertyScannerTests : IDisposable
{
    public PIIPropertyScannerTests()
    {
        PIIPropertyScanner.ClearCache();
    }

    public void Dispose()
    {
        PIIPropertyScanner.ClearCache();
    }

    #region Test DTOs

    private sealed class DecoratedDto
    {
        [PII(PIIType.Email)]
        public string Email { get; set; } = "";

        [PII(PIIType.Phone)]
        public string Phone { get; set; } = "";

        public string Normal { get; set; } = "";
    }

    private sealed class UndecoratedDto
    {
        public string Name { get; set; } = "";
    }

    private sealed class SensitiveDataDto
    {
        [SensitiveData]
        public string ApiKey { get; set; } = "";

        [SensitiveData(MaskingMode.Redact)]
        public string SecretToken { get; set; } = "";

        public string PublicField { get; set; } = "";
    }

    private sealed class MaskInLogsDto
    {
        [MaskInLogs]
        public string TrackingId { get; set; } = "";

        [MaskInLogs(MaskingMode.Hash)]
        public string CorrelationKey { get; set; } = "";

        public string RegularField { get; set; } = "";
    }

    private sealed class MixedAttributesDto
    {
        [PII(PIIType.Email)]
        public string Email { get; set; } = "";

        [SensitiveData]
        public string Token { get; set; } = "";

        [MaskInLogs]
        public string InternalId { get; set; } = "";

        public string Plain { get; set; } = "";
    }

    private sealed class ReadOnlyPropertyDto
    {
        [PII(PIIType.Email)]
        public string Email { get; } = "";

        public string Normal { get; set; } = "";
    }

    [PII(PIIType.Name)]
    private sealed class ClassLevelPiiDto
    {
        public string First { get; set; } = "";

        public string Last { get; set; } = "";

        public int Age { get; set; }
    }

    [SensitiveData(MaskingMode.Redact)]
    private sealed class ClassLevelSensitiveDto
    {
        public string Note { get; set; } = "";
    }

    [Fact]
    public void GetProperties_ClassLevelPii_AppliesToEveryStringProperty()
    {
        var properties = PIIPropertyScanner.GetProperties(typeof(ClassLevelPiiDto));

        properties.Length.ShouldBe(2);
        properties.ShouldAllBe(p => p.Type == PIIType.Name && !p.LogOnly && p.Setter != null);
    }

    [Fact]
    public void GetProperties_ClassLevelSensitiveData_AppliesToEveryStringProperty()
    {
        var properties = PIIPropertyScanner.GetProperties(typeof(ClassLevelSensitiveDto));

        var note = properties.ShouldHaveSingleItem();
        note.Type.ShouldBe(PIIType.Custom);
        note.Mode.ShouldBe(MaskingMode.Redact);
    }

    private sealed class NonStringPropertyDto
    {
        [PII(PIIType.Custom)]
        public int Code { get; set; }

        public string Name { get; set; } = "";
    }

    #endregion

    [Fact]
    public void GetProperties_WithPIIAttributes_ReturnsMetadata()
    {
        // Act
        var properties = PIIPropertyScanner.GetProperties(typeof(DecoratedDto));

        // Assert
        properties.ShouldNotBeNull();
        properties.Length.ShouldBe(2);

        var emailProp = properties.First(p => p.Property.Name == "Email");
        emailProp.Type.ShouldBe(PIIType.Email);
        emailProp.LogOnly.ShouldBeFalse();

        var phoneProp = properties.First(p => p.Property.Name == "Phone");
        phoneProp.Type.ShouldBe(PIIType.Phone);
        phoneProp.LogOnly.ShouldBeFalse();
    }

    [Fact]
    public void GetProperties_WithoutAttributes_ReturnsEmpty()
    {
        // Act
        var properties = PIIPropertyScanner.GetProperties(typeof(UndecoratedDto));

        // Assert
        properties.ShouldNotBeNull();
        properties.ShouldBeEmpty();
    }

    [Fact]
    public void GetProperties_CachesResults()
    {
        // Act
        var first = PIIPropertyScanner.GetProperties(typeof(DecoratedDto));
        var second = PIIPropertyScanner.GetProperties(typeof(DecoratedDto));

        // Assert - should return the same reference (cached)
        ReferenceEquals(first, second).ShouldBeTrue();
    }

    [Fact]
    public void GetProperties_SensitiveDataAttribute_ReturnsFullMaskMetadata()
    {
        // Act
        var properties = PIIPropertyScanner.GetProperties(typeof(SensitiveDataDto));

        // Assert
        properties.ShouldNotBeNull();
        properties.Length.ShouldBe(2);

        var apiKeyProp = properties.First(p => p.Property.Name == "ApiKey");
        apiKeyProp.Type.ShouldBe(PIIType.Custom);
        apiKeyProp.Mode.ShouldBe(MaskingMode.Full);
        apiKeyProp.LogOnly.ShouldBeFalse();

        var secretTokenProp = properties.First(p => p.Property.Name == "SecretToken");
        secretTokenProp.Type.ShouldBe(PIIType.Custom);
        secretTokenProp.Mode.ShouldBe(MaskingMode.Redact);
        secretTokenProp.LogOnly.ShouldBeFalse();
    }

    [Fact]
    public void GetProperties_MaskInLogsAttribute_ReturnsLogOnlyMetadata()
    {
        // Act
        var properties = PIIPropertyScanner.GetProperties(typeof(MaskInLogsDto));

        // Assert
        properties.ShouldNotBeNull();
        properties.Length.ShouldBe(2);

        var trackingIdProp = properties.First(p => p.Property.Name == "TrackingId");
        trackingIdProp.Type.ShouldBe(PIIType.Custom);
        trackingIdProp.Mode.ShouldBe(MaskingMode.Partial);
        trackingIdProp.LogOnly.ShouldBeTrue();

        var correlationProp = properties.First(p => p.Property.Name == "CorrelationKey");
        correlationProp.Type.ShouldBe(PIIType.Custom);
        correlationProp.Mode.ShouldBe(MaskingMode.Hash);
        correlationProp.LogOnly.ShouldBeTrue();
    }

    [Fact]
    public void GetProperties_MixedAttributes_ReturnsAllDecoratedProperties()
    {
        // Act
        var properties = PIIPropertyScanner.GetProperties(typeof(MixedAttributesDto));

        // Assert
        properties.ShouldNotBeNull();
        properties.Length.ShouldBe(3);

        properties.ShouldContain(p => p.Property.Name == "Email");
        properties.ShouldContain(p => p.Property.Name == "Token");
        properties.ShouldContain(p => p.Property.Name == "InternalId");
    }

    [Fact]
    public void GetProperties_SkipsNonStringProperties()
    {
        // Act
        var properties = PIIPropertyScanner.GetProperties(typeof(NonStringPropertyDto));

        // Assert - int property with [PII] should be skipped (only string properties supported)
        properties.ShouldNotBeNull();
        properties.ShouldBeEmpty();
    }

    [Fact]
    public void GetProperties_ReadOnlyProperty_IsKeptWithoutSetter()
    {
        // Act
        var properties = PIIPropertyScanner.GetProperties(typeof(ReadOnlyPropertyDto));

        // Assert - a get-only PII property is never skipped (skipping it would leak it unmasked);
        // the masker fails closed when the masked value does not take effect on the copy
        var email = properties.ShouldHaveSingleItem();
        email.Property.Name.ShouldBe(nameof(ReadOnlyPropertyDto.Email));
        email.Setter.ShouldBeNull();
        Should.Throw<InvalidOperationException>(() => email.SetValue(new ReadOnlyPropertyDto(), "x"));
    }

    [Fact]
    public void ClearCache_AllowsFreshDiscovery()
    {
        // Arrange - populate cache
        var first = PIIPropertyScanner.GetProperties(typeof(DecoratedDto));

        // Act - clear and re-fetch
        PIIPropertyScanner.ClearCache();
        var second = PIIPropertyScanner.GetProperties(typeof(DecoratedDto));

        // Assert - contents should be equivalent but different array references
        second.Length.ShouldBe(first.Length);
        ReferenceEquals(first, second).ShouldBeFalse();
    }
}
