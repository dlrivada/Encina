using Encina.Marten;
using global::Marten;
using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.UnitTests.Marten;

/// <summary>
/// Unit tests for <see cref="ConfigureMartenEventMetadata"/>.
/// </summary>
public sealed class ConfigureMartenEventMetadataTests
{
    #region Configure — Metadata Disabled

    [Fact]
    public void Configure_AllMetadataDisabled_DoesNotEnableAnything()
    {
        var opts = new EncinaMartenOptions();
        opts.Metadata.CorrelationIdEnabled = false;
        opts.Metadata.CausationIdEnabled = false;
        opts.Metadata.CaptureUserId = false;
        opts.Metadata.CaptureTenantId = false;
        opts.Metadata.CaptureCommitSha = false;
        opts.Metadata.CaptureSemanticVersion = false;
        opts.Metadata.CaptureTimestamp = false;
        opts.Metadata.HeadersEnabled = false;

        var sut = new ConfigureMartenEventMetadata(
            Options.Create(opts), NullLogger<ConfigureMartenEventMetadata>.Instance);

        var storeOpts = new StoreOptions();
        sut.Configure(storeOpts);

        storeOpts.Events.MetadataConfig.CorrelationIdEnabled.ShouldBeFalse();
        storeOpts.Events.MetadataConfig.CausationIdEnabled.ShouldBeFalse();
    }

    #endregion

    #region Configure — CorrelationId

    [Fact]
    public void Configure_CorrelationIdEnabled_EnablesInMarten()
    {
        var opts = new EncinaMartenOptions();
        opts.Metadata.CorrelationIdEnabled = true;
        opts.Metadata.CausationIdEnabled = false;
        opts.Metadata.CaptureUserId = false;
        opts.Metadata.CaptureTenantId = false;
        opts.Metadata.CaptureTimestamp = false;

        var sut = new ConfigureMartenEventMetadata(
            Options.Create(opts), NullLogger<ConfigureMartenEventMetadata>.Instance);

        var storeOpts = new StoreOptions();
        sut.Configure(storeOpts);

        storeOpts.Events.MetadataConfig.CorrelationIdEnabled.ShouldBeTrue();
    }

    #endregion

    #region Configure — CausationId

    [Fact]
    public void Configure_CausationIdEnabled_EnablesInMarten()
    {
        var opts = new EncinaMartenOptions();
        opts.Metadata.CorrelationIdEnabled = false;
        opts.Metadata.CausationIdEnabled = true;
        opts.Metadata.CaptureUserId = false;
        opts.Metadata.CaptureTenantId = false;
        opts.Metadata.CaptureTimestamp = false;

        var sut = new ConfigureMartenEventMetadata(
            Options.Create(opts), NullLogger<ConfigureMartenEventMetadata>.Instance);

        var storeOpts = new StoreOptions();
        sut.Configure(storeOpts);

        storeOpts.Events.MetadataConfig.CausationIdEnabled.ShouldBeTrue();
    }

    #endregion

    #region Configure — Headers

    [Fact]
    public void Configure_CaptureUserId_EnablesHeaders()
    {
        var opts = new EncinaMartenOptions();
        opts.Metadata.CorrelationIdEnabled = false;
        opts.Metadata.CausationIdEnabled = false;
        opts.Metadata.CaptureUserId = true;
        opts.Metadata.CaptureTenantId = false;
        opts.Metadata.CaptureTimestamp = false;

        var sut = new ConfigureMartenEventMetadata(
            Options.Create(opts), NullLogger<ConfigureMartenEventMetadata>.Instance);

        var storeOpts = new StoreOptions();
        sut.Configure(storeOpts);

        storeOpts.Events.MetadataConfig.HeadersEnabled.ShouldBeTrue();
    }

    [Fact]
    public void Configure_CustomHeaders_EnablesHeaders()
    {
        var opts = new EncinaMartenOptions();
        opts.Metadata.CorrelationIdEnabled = false;
        opts.Metadata.CausationIdEnabled = false;
        opts.Metadata.CaptureUserId = false;
        opts.Metadata.CaptureTenantId = false;
        opts.Metadata.CaptureTimestamp = false;
        opts.Metadata.CustomHeaders["env"] = "test";

        var sut = new ConfigureMartenEventMetadata(
            Options.Create(opts), NullLogger<ConfigureMartenEventMetadata>.Instance);

        var storeOpts = new StoreOptions();
        sut.Configure(storeOpts);

        storeOpts.Events.MetadataConfig.HeadersEnabled.ShouldBeTrue();
    }

    [Fact]
    public void Configure_AllDefaults_EnablesCorrelationCausationAndHeaders()
    {
        var opts = new EncinaMartenOptions(); // all defaults = correlation+causation+userId+tenantId+timestamp enabled

        var sut = new ConfigureMartenEventMetadata(
            Options.Create(opts), NullLogger<ConfigureMartenEventMetadata>.Instance);

        var storeOpts = new StoreOptions();
        sut.Configure(storeOpts);

        storeOpts.Events.MetadataConfig.CorrelationIdEnabled.ShouldBeTrue();
        storeOpts.Events.MetadataConfig.CausationIdEnabled.ShouldBeTrue();
        storeOpts.Events.MetadataConfig.HeadersEnabled.ShouldBeTrue();
    }

    [Fact]
    public void Configure_CommitShaConfigured_CountsAsHeader()
    {
        var opts = new EncinaMartenOptions();
        opts.Metadata.CaptureCommitSha = true;
        opts.Metadata.CommitSha = "abc123";

        var sut = new ConfigureMartenEventMetadata(
            Options.Create(opts), NullLogger<ConfigureMartenEventMetadata>.Instance);

        var storeOpts = new StoreOptions();
        sut.Configure(storeOpts);

        storeOpts.Events.MetadataConfig.HeadersEnabled.ShouldBeTrue();
    }

    [Fact]
    public void Configure_SemanticVersionConfigured_CountsAsHeader()
    {
        var opts = new EncinaMartenOptions();
        opts.Metadata.CaptureSemanticVersion = true;
        opts.Metadata.SemanticVersion = "1.0.0";

        var sut = new ConfigureMartenEventMetadata(
            Options.Create(opts), NullLogger<ConfigureMartenEventMetadata>.Instance);

        var storeOpts = new StoreOptions();
        sut.Configure(storeOpts);

        storeOpts.Events.MetadataConfig.HeadersEnabled.ShouldBeTrue();
    }

    #endregion

    #region CountConfiguredHeaders via Configure path

    [Fact]
    public void Configure_WithAllHeaderTypes_CountsAll()
    {
        var opts = new EncinaMartenOptions();
        opts.Metadata.CaptureUserId = true;
        opts.Metadata.CaptureTenantId = true;
        opts.Metadata.CaptureTimestamp = true;
        opts.Metadata.CaptureCommitSha = true;
        opts.Metadata.CommitSha = "abc";
        opts.Metadata.CaptureSemanticVersion = true;
        opts.Metadata.SemanticVersion = "1.0";
        opts.Metadata.CustomHeaders["key1"] = "val1";
        opts.Metadata.CustomHeaders["key2"] = "val2";

        var sut = new ConfigureMartenEventMetadata(
            Options.Create(opts), NullLogger<ConfigureMartenEventMetadata>.Instance);

        var storeOpts = new StoreOptions();
        // Should not throw — exercises the full Configure + CountConfiguredHeaders path
        sut.Configure(storeOpts);

        storeOpts.Events.MetadataConfig.HeadersEnabled.ShouldBeTrue();
    }

    #endregion
}
