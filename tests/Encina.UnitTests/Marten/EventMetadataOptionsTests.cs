using Encina.Marten;

namespace Encina.UnitTests.Marten;

public sealed class EventMetadataOptionsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var options = new EventMetadataOptions();

        // Core tracking enabled by default
        options.CorrelationIdEnabled.ShouldBeTrue();
        options.CausationIdEnabled.ShouldBeTrue();
        options.CaptureUserId.ShouldBeTrue();
        options.CaptureTenantId.ShouldBeTrue();
        options.CaptureTimestamp.ShouldBeTrue();
        options.HeadersEnabled.ShouldBeTrue();

        // Optional features disabled by default
        options.CaptureCommitSha.ShouldBeFalse();
        options.CaptureSemanticVersion.ShouldBeFalse();

        // Optional values are null by default
        options.CommitSha.ShouldBeNull();
        options.SemanticVersion.ShouldBeNull();

        // CustomHeaders is empty but not null
        options.CustomHeaders.ShouldNotBeNull();
        options.CustomHeaders.ShouldBeEmpty();
    }

    [Fact]
    public void Properties_CanBeSet()
    {
        var options = new EventMetadataOptions
        {
            CorrelationIdEnabled = false,
            CausationIdEnabled = false,
            CaptureUserId = false,
            CaptureTenantId = false,
            CaptureTimestamp = false,
            HeadersEnabled = false,
            CaptureCommitSha = true,
            CommitSha = "abc123",
            CaptureSemanticVersion = true,
            SemanticVersion = "1.0.0"
        };

        options.CorrelationIdEnabled.ShouldBeFalse();
        options.CausationIdEnabled.ShouldBeFalse();
        options.CaptureUserId.ShouldBeFalse();
        options.CaptureTenantId.ShouldBeFalse();
        options.CaptureTimestamp.ShouldBeFalse();
        options.HeadersEnabled.ShouldBeFalse();
        options.CaptureCommitSha.ShouldBeTrue();
        options.CommitSha.ShouldBe("abc123");
        options.CaptureSemanticVersion.ShouldBeTrue();
        options.SemanticVersion.ShouldBe("1.0.0");
    }

    [Fact]
    public void CustomHeaders_CanBePopulated()
    {
        var options = new EventMetadataOptions();

        options.CustomHeaders["Environment"] = "Production";
        options.CustomHeaders["ServiceName"] = "OrderService";
        options.CustomHeaders["Region"] = "eu-west-1";

        options.CustomHeaders.Count.ShouldBe(3);
        options.CustomHeaders["Environment"].ShouldBe("Production");
        options.CustomHeaders["ServiceName"].ShouldBe("OrderService");
        options.CustomHeaders["Region"].ShouldBe("eu-west-1");
    }

    [Fact]
    public void IsAnyMetadataEnabled_WithDefaults_ReturnsTrue()
    {
        var options = new EventMetadataOptions();

        // Default options have correlation and causation enabled
        options.IsAnyMetadataEnabled().ShouldBeTrue();
    }

    [Fact]
    public void IsAnyMetadataEnabled_AllDisabled_ReturnsFalse()
    {
        var options = new EventMetadataOptions
        {
            CorrelationIdEnabled = false,
            CausationIdEnabled = false,
            CaptureUserId = false,
            CaptureTenantId = false,
            CaptureTimestamp = false,
            CaptureCommitSha = false,
            CaptureSemanticVersion = false,
            HeadersEnabled = false
        };

        options.IsAnyMetadataEnabled().ShouldBeFalse();
    }

    [Fact]
    public void IsAnyMetadataEnabled_OnlyCorrelationId_ReturnsTrue()
    {
        var options = new EventMetadataOptions
        {
            CorrelationIdEnabled = true,
            CausationIdEnabled = false,
            CaptureUserId = false,
            CaptureTenantId = false,
            CaptureTimestamp = false,
            CaptureCommitSha = false,
            CaptureSemanticVersion = false,
            HeadersEnabled = false
        };

        options.IsAnyMetadataEnabled().ShouldBeTrue();
    }

    [Fact]
    public void IsAnyMetadataEnabled_OnlyCausationId_ReturnsTrue()
    {
        var options = new EventMetadataOptions
        {
            CorrelationIdEnabled = false,
            CausationIdEnabled = true,
            CaptureUserId = false,
            CaptureTenantId = false,
            CaptureTimestamp = false,
            CaptureCommitSha = false,
            CaptureSemanticVersion = false,
            HeadersEnabled = false
        };

        options.IsAnyMetadataEnabled().ShouldBeTrue();
    }

    [Fact]
    public void IsAnyMetadataEnabled_OnlyCustomHeaders_ReturnsTrue()
    {
        var options = new EventMetadataOptions
        {
            CorrelationIdEnabled = false,
            CausationIdEnabled = false,
            CaptureUserId = false,
            CaptureTenantId = false,
            CaptureTimestamp = false,
            CaptureCommitSha = false,
            CaptureSemanticVersion = false,
            HeadersEnabled = true
        };

        // No custom headers yet, should be false
        options.IsAnyMetadataEnabled().ShouldBeFalse();

        // Add a custom header
        options.CustomHeaders["Test"] = "Value";
        options.IsAnyMetadataEnabled().ShouldBeTrue();
    }

    [Fact]
    public void RequiresHeaderStorage_WithUserIdEnabled_ReturnsTrue()
    {
        var options = new EventMetadataOptions
        {
            CaptureUserId = true,
            CaptureTenantId = false,
            CaptureCommitSha = false,
            CaptureSemanticVersion = false,
            CaptureTimestamp = false,
            HeadersEnabled = true
        };

        options.RequiresHeaderStorage().ShouldBeTrue();
    }

    [Fact]
    public void RequiresHeaderStorage_WithCustomHeaders_ReturnsTrue()
    {
        var options = new EventMetadataOptions
        {
            CaptureUserId = false,
            CaptureTenantId = false,
            CaptureCommitSha = false,
            CaptureSemanticVersion = false,
            CaptureTimestamp = false,
            HeadersEnabled = true
        };

        options.CustomHeaders["Test"] = "Value";

        options.RequiresHeaderStorage().ShouldBeTrue();
    }

    [Fact]
    public void RequiresHeaderStorage_HeadersDisabled_ReturnsFalse()
    {
        var options = new EventMetadataOptions
        {
            CaptureUserId = true,
            CaptureTimestamp = true,
            HeadersEnabled = false // Headers disabled
        };

        options.RequiresHeaderStorage().ShouldBeFalse();
    }

    [Fact]
    public void RequiresHeaderStorage_NothingToCapture_ReturnsFalse()
    {
        var options = new EventMetadataOptions
        {
            CaptureUserId = false,
            CaptureTenantId = false,
            CaptureCommitSha = false,
            CaptureSemanticVersion = false,
            CaptureTimestamp = false,
            HeadersEnabled = true
        };

        // No custom headers and all capture flags disabled
        options.RequiresHeaderStorage().ShouldBeFalse();
    }

    [Fact]
    public void RequiresHeaderStorage_WithCommitShaConfigured_ReturnsTrue()
    {
        var options = new EventMetadataOptions
        {
            CaptureUserId = false,
            CaptureTenantId = false,
            CaptureCommitSha = true,
            CommitSha = "abc123",
            CaptureSemanticVersion = false,
            CaptureTimestamp = false,
            HeadersEnabled = true
        };

        options.RequiresHeaderStorage().ShouldBeTrue();
    }

    [Fact]
    public void RequiresHeaderStorage_CommitShaEnabledButNull_ReturnsFalse()
    {
        var options = new EventMetadataOptions
        {
            CaptureUserId = false,
            CaptureTenantId = false,
            CaptureCommitSha = true,
            CommitSha = null, // Not set
            CaptureSemanticVersion = false,
            CaptureTimestamp = false,
            HeadersEnabled = true
        };

        // CommitSha is null, so nothing to capture
        options.RequiresHeaderStorage().ShouldBeFalse();
    }
}
