using System.Diagnostics.CodeAnalysis;
using Encina.Cdc;
using Encina.Cdc.Messaging;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Cdc.Messaging;

/// <summary>
/// Unit tests for <see cref="CdcMessagingBridge"/>.
/// </summary>
[SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "Mock setup pattern for NSubstitute")]
public sealed class CdcMessagingBridgeTests
{
    private static readonly DateTime FixedUtcNow = new(2026, 2, 1, 12, 0, 0, DateTimeKind.Utc);

    #region Test Helpers

    private sealed record TestFixture(
        IEncina Encina,
        CdcMessagingOptions Options,
        ILogger<CdcMessagingBridge> Logger,
        CdcMessagingBridge Bridge);

    private static TestFixture CreateTestFixture(CdcMessagingOptions? options = null)
    {
        var encina = Substitute.For<IEncina>();
        encina.Publish(Arg.Any<CdcChangeNotification>(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, Unit>>(Right(unit)));

        var opts = options ?? new CdcMessagingOptions();
        var logger = NullLogger<CdcMessagingBridge>.Instance;
        var bridge = new CdcMessagingBridge(encina, opts, logger);

        return new TestFixture(encina, opts, logger, bridge);
    }

    private static ChangeEvent CreateChangeEvent(
        string tableName = "Orders",
        ChangeOperation operation = ChangeOperation.Insert)
    {
        var position = new TestCdcPosition(1);
        var metadata = new ChangeMetadata(position, FixedUtcNow, null, null, null);
        return new ChangeEvent(tableName, operation, null, new { Id = 1 }, metadata);
    }

    #endregion

    #region Constructor

    [Fact]
    public void Constructor_NullEncina_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new CdcMessagingBridge(null!, new CdcMessagingOptions(), NullLogger<CdcMessagingBridge>.Instance));
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new CdcMessagingBridge(Substitute.For<IEncina>(), null!, NullLogger<CdcMessagingBridge>.Instance));
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new CdcMessagingBridge(Substitute.For<IEncina>(), new CdcMessagingOptions(), null!));
    }

    #endregion

    #region OnEventDispatchedAsync

    [Fact]
    public async Task OnEventDispatchedAsync_PublishesNotification()
    {
        var fixture = CreateTestFixture();
        var evt = CreateChangeEvent();

        var result = await fixture.Bridge.OnEventDispatchedAsync(evt);

        result.IsRight.ShouldBeTrue();
        await fixture.Encina.Received(1).Publish(
            Arg.Is<CdcChangeNotification>(n => n.TableName == "Orders"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OnEventDispatchedAsync_NullEvent_ThrowsArgumentNullException()
    {
        var fixture = CreateTestFixture();

        await Should.ThrowAsync<ArgumentNullException>(
            () => fixture.Bridge.OnEventDispatchedAsync(null!).AsTask());
    }

    [Fact]
    public async Task OnEventDispatchedAsync_FilteredOut_DoesNotPublish()
    {
        var options = new CdcMessagingOptions
        {
            ExcludeTables = ["AuditLog"]
        };
        var fixture = CreateTestFixture(options);
        var evt = CreateChangeEvent("AuditLog");

        var result = await fixture.Bridge.OnEventDispatchedAsync(evt);

        result.IsRight.ShouldBeTrue();
        await fixture.Encina.DidNotReceive().Publish(
            Arg.Any<CdcChangeNotification>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OnEventDispatchedAsync_UsesTopicPattern()
    {
        var options = new CdcMessagingOptions
        {
            TopicPattern = "cdc.{tableName}.{operation}"
        };
        var fixture = CreateTestFixture(options);
        var evt = CreateChangeEvent("Products", ChangeOperation.Update);

        await fixture.Bridge.OnEventDispatchedAsync(evt);

        await fixture.Encina.Received(1).Publish(
            Arg.Is<CdcChangeNotification>(n => n.TopicName == "cdc.Products.update"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OnEventDispatchedAsync_PublishFails_ReturnsError()
    {
        var encina = Substitute.For<IEncina>();
        encina.Publish(Arg.Any<CdcChangeNotification>(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, Unit>>(
                Left<EncinaError, Unit>(EncinaError.New("publish failed"))));

        var bridge = new CdcMessagingBridge(encina, new CdcMessagingOptions(), NullLogger<CdcMessagingBridge>.Instance);
        var evt = CreateChangeEvent();

        var result = await bridge.OnEventDispatchedAsync(evt);

        result.IsLeft.ShouldBeTrue();
    }

    #endregion
}
