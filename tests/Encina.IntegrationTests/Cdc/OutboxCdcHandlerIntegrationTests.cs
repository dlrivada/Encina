using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Encina.Cdc;
using Encina.Cdc.Abstractions;
using Encina.Cdc.Messaging;
using Encina.IntegrationTests.Cdc.Helpers;
using Encina.Testing.Fakes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace Encina.IntegrationTests.Cdc;

/// <summary>
/// Integration tests for <see cref="OutboxCdcHandler"/> verifying that
/// outbox messages captured by CDC are correctly republished as notifications.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Feature", "CDC")]
[SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "Integration test assertions on ValueTask results")]
public sealed class OutboxCdcHandlerIntegrationTests
{
    /// <summary>
    /// Simple notification used for testing outbox CDC republishing.
    /// </summary>
    public sealed record TestNotification(string Message) : INotification;

    #region Insert Processing

    [Fact]
    public async Task HandleInsert_ValidOutboxRow_RepublishesNotification()
    {
        // Arrange
        var fakeEncina = new FakeEncina();
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddProvider(NullLoggerProvider.Instance));
        services.AddSingleton<IEncina>(fakeEncina);
        services.AddEncinaCdc(config =>
        {
            config.UseOutboxCdc("OutboxMessages");
        });

        using var sp = services.BuildServiceProvider();
        using var scope = sp.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<OutboxCdcHandler>();

        var notification = new TestNotification("Hello from Outbox");
        var outboxRow = JsonSerializer.SerializeToElement(new
        {
            Id = Guid.NewGuid().ToString(),
            NotificationType = typeof(TestNotification).AssemblyQualifiedName,
            Content = JsonSerializer.Serialize(notification),
            CreatedAtUtc = DateTime.UtcNow.ToString("O"),
            ProcessedAtUtc = (string?)null,
            ErrorMessage = (string?)null,
            RetryCount = 0
        });

        var position = new TestCdcPosition(1);
        var metadata = new ChangeMetadata(position, DateTime.UtcNow, null, null, null);
        var context = new ChangeContext("OutboxMessages", metadata, CancellationToken.None);

        // Act
        var result = await handler.HandleInsertAsync(outboxRow, context);

        // Assert
        result.IsRight.ShouldBeTrue();
        fakeEncina.PublishedNotifications.ShouldHaveSingleItem();
        var published = fakeEncina.PublishedNotifications[0].ShouldBeOfType<TestNotification>();
        published.Message.ShouldBe("Hello from Outbox");
    }

    [Fact]
    public async Task HandleInsert_AlreadyProcessedRow_SkipsRepublishing()
    {
        // Arrange
        var fakeEncina = new FakeEncina();
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddProvider(NullLoggerProvider.Instance));
        services.AddSingleton<IEncina>(fakeEncina);
        services.AddEncinaCdc(config =>
        {
            config.UseOutboxCdc("OutboxMessages");
        });

        using var sp = services.BuildServiceProvider();
        using var scope = sp.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<OutboxCdcHandler>();

        var outboxRow = JsonSerializer.SerializeToElement(new
        {
            Id = Guid.NewGuid().ToString(),
            NotificationType = typeof(TestNotification).AssemblyQualifiedName,
            Content = JsonSerializer.Serialize(new TestNotification("Already done")),
            CreatedAtUtc = DateTime.UtcNow.ToString("O"),
            ProcessedAtUtc = DateTime.UtcNow.ToString("O"), // Already processed!
            ErrorMessage = (string?)null,
            RetryCount = 0
        });

        var position = new TestCdcPosition(1);
        var metadata = new ChangeMetadata(position, DateTime.UtcNow, null, null, null);
        var context = new ChangeContext("OutboxMessages", metadata, CancellationToken.None);

        // Act
        var result = await handler.HandleInsertAsync(outboxRow, context);

        // Assert
        result.IsRight.ShouldBeTrue();
        fakeEncina.PublishedNotifications.ShouldBeEmpty();
    }

    [Fact]
    public async Task HandleInsert_MissingNotificationType_ReturnsError()
    {
        // Arrange
        var fakeEncina = new FakeEncina();
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddProvider(NullLoggerProvider.Instance));
        services.AddSingleton<IEncina>(fakeEncina);
        services.AddEncinaCdc(config =>
        {
            config.UseOutboxCdc("OutboxMessages");
        });

        using var sp = services.BuildServiceProvider();
        using var scope = sp.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<OutboxCdcHandler>();

        var outboxRow = JsonSerializer.SerializeToElement(new
        {
            Id = Guid.NewGuid().ToString(),
            // Missing NotificationType!
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow.ToString("O")
        });

        var position = new TestCdcPosition(1);
        var metadata = new ChangeMetadata(position, DateTime.UtcNow, null, null, null);
        var context = new ChangeContext("OutboxMessages", metadata, CancellationToken.None);

        // Act
        var result = await handler.HandleInsertAsync(outboxRow, context);

        // Assert
        result.IsLeft.ShouldBeTrue();
        fakeEncina.PublishedNotifications.ShouldBeEmpty();
    }

    #endregion

    #region Update and Delete (No-ops)

    [Fact]
    public async Task HandleUpdate_ReturnsSuccess_WithoutPublishing()
    {
        // Arrange
        var fakeEncina = new FakeEncina();
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddProvider(NullLoggerProvider.Instance));
        services.AddSingleton<IEncina>(fakeEncina);
        services.AddEncinaCdc(config =>
        {
            config.UseOutboxCdc("OutboxMessages");
        });

        using var sp = services.BuildServiceProvider();
        using var scope = sp.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<OutboxCdcHandler>();

        var before = JsonSerializer.SerializeToElement(new { Id = "1" });
        var after = JsonSerializer.SerializeToElement(new { Id = "1" });
        var position = new TestCdcPosition(1);
        var metadata = new ChangeMetadata(position, DateTime.UtcNow, null, null, null);
        var context = new ChangeContext("OutboxMessages", metadata, CancellationToken.None);

        // Act
        var result = await handler.HandleUpdateAsync(before, after, context);

        // Assert
        result.IsRight.ShouldBeTrue();
        fakeEncina.PublishedNotifications.ShouldBeEmpty();
    }

    [Fact]
    public async Task HandleDelete_ReturnsSuccess_WithoutPublishing()
    {
        // Arrange
        var fakeEncina = new FakeEncina();
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddProvider(NullLoggerProvider.Instance));
        services.AddSingleton<IEncina>(fakeEncina);
        services.AddEncinaCdc(config =>
        {
            config.UseOutboxCdc("OutboxMessages");
        });

        using var sp = services.BuildServiceProvider();
        using var scope = sp.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<OutboxCdcHandler>();

        var entity = JsonSerializer.SerializeToElement(new { Id = "1" });
        var position = new TestCdcPosition(1);
        var metadata = new ChangeMetadata(position, DateTime.UtcNow, null, null, null);
        var context = new ChangeContext("OutboxMessages", metadata, CancellationToken.None);

        // Act
        var result = await handler.HandleDeleteAsync(entity, context);

        // Assert
        result.IsRight.ShouldBeTrue();
        fakeEncina.PublishedNotifications.ShouldBeEmpty();
    }

    #endregion

    #region PascalCase Property Names

    [Fact]
    public async Task HandleInsert_PascalCaseProperties_StillProcessesCorrectly()
    {
        // Arrange
        var fakeEncina = new FakeEncina();
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddProvider(NullLoggerProvider.Instance));
        services.AddSingleton<IEncina>(fakeEncina);
        services.AddEncinaCdc(config =>
        {
            config.UseOutboxCdc("OutboxMessages");
        });

        using var sp = services.BuildServiceProvider();
        using var scope = sp.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<OutboxCdcHandler>();

        // Use PascalCase property names (as might come from SQL Server CDC)
        var notification = new TestNotification("PascalCase Test");
        var outboxRow = JsonDocument.Parse($$"""
        {
            "Id": "{{Guid.NewGuid()}}",
            "NotificationType": "{{typeof(TestNotification).AssemblyQualifiedName}}",
            "Content": "{{JsonSerializer.Serialize(notification).Replace("\"", "\\\"")}}",
            "CreatedAtUtc": "{{DateTime.UtcNow:O}}",
            "ProcessedAtUtc": null,
            "ErrorMessage": null,
            "RetryCount": 0
        }
        """).RootElement;

        var position = new TestCdcPosition(1);
        var metadata = new ChangeMetadata(position, DateTime.UtcNow, null, null, null);
        var context = new ChangeContext("OutboxMessages", metadata, CancellationToken.None);

        // Act
        var result = await handler.HandleInsertAsync(outboxRow, context);

        // Assert
        result.IsRight.ShouldBeTrue();
        fakeEncina.PublishedNotifications.ShouldHaveSingleItem();
    }

    #endregion
}
