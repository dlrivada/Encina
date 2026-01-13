using Encina.EntityFrameworkCore.Outbox;
using Microsoft.EntityFrameworkCore;
using NBomber.CSharp;
using Xunit.Abstractions;

namespace Encina.EntityFrameworkCore.LoadTests.Outbox;

/// <summary>
/// Load tests for <see cref="OutboxStoreEF"/>.
/// Verifies performance and concurrency handling under stress for outbox pattern operations.
/// </summary>
[Trait("Category", "Load")]
public sealed class OutboxStoreEFLoadTests
{
    private readonly ITestOutputHelper _output;

    public OutboxStoreEFLoadTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void HighConcurrency_AddMessages_ShouldHandleLoad()
    {
        // Act
        var scenario = Scenario.Create("add_outbox_messages", async context =>
        {
            // Create new DbContext per invocation for thread safety
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase($"OutboxAdd_{context.InvocationNumber}")
                .Options;

            await using var dbContext = new TestDbContext(options);
            var store = new OutboxStoreEF(dbContext);

            var message = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                NotificationType = "TestNotification",
                Content = "{\"data\":\"test\"}",
                CreatedAtUtc = DateTime.UtcNow
            };

            await store.AddAsync(message, CancellationToken.None);
            await store.SaveChangesAsync(CancellationToken.None);

            return Response.Ok();
        })
        .WithLoadSimulations(
            Simulation.Inject(rate: 100, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(10))
        );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .WithoutReports()
            .Run();

        // Assert
        var scen = stats.ScenarioStats[0];
        _output.WriteLine($"Add Messages - OK: {scen.Ok.Request.Count}, Fail: {scen.Fail.Request.Count}, RPS: {scen.Ok.Request.RPS}");
        Assert.True(scen.Ok.Request.Count > 900, $"Expected > 900 successful adds, got {scen.Ok.Request.Count}");
    }

    [Fact]
    public void HighConcurrency_GetPending_ShouldHandleLoad()
    {
        // Arrange - Seed database with pending messages
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase("OutboxGetPending")
            .Options;

        using (var dbContext = new TestDbContext(options))
        {
            for (int i = 0; i < 100; i++)
            {
                dbContext.Set<OutboxMessage>().Add(new OutboxMessage
                {
                    Id = Guid.NewGuid(),
                    NotificationType = "TestNotification",
                    Content = "{\"data\":\"test\"}",
                    CreatedAtUtc = DateTime.UtcNow.AddMinutes(-i)
                });
            }
            dbContext.SaveChanges();
        }

        // Act
        var scenario = Scenario.Create("get_pending_messages", async context =>
        {
            await using var dbContext = new TestDbContext(options);
            var store = new OutboxStoreEF(dbContext);

            var pending = await store.GetPendingMessagesAsync(
                batchSize: 10,
                maxRetries: 3,
                CancellationToken.None);

            return pending.Any() ? Response.Ok() : Response.Fail<string>(statusCode: "no_messages");
        })
        .WithLoadSimulations(
            Simulation.Inject(rate: 100, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(10))
        );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .WithoutReports()
            .Run();

        // Assert
        var scen = stats.ScenarioStats[0];
        _output.WriteLine($"Get Pending - OK: {scen.Ok.Request.Count}, Fail: {scen.Fail.Request.Count}, RPS: {scen.Ok.Request.RPS}");
        Assert.True(scen.Ok.Request.Count > 900, $"Expected > 900 successful reads, got {scen.Ok.Request.Count}");
    }

    [Fact]
    public void MixedLoad_AddAndMark_ShouldHandleLoad()
    {
        // Arrange - Shared database
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase("OutboxMixed")
            .Options;

        // Seed initial messages for marking
        using (var dbContext = new TestDbContext(options))
        {
            for (int i = 0; i < 500; i++)
            {
                dbContext.Set<OutboxMessage>().Add(new OutboxMessage
                {
                    Id = Guid.NewGuid(),
                    NotificationType = "TestNotification",
                    Content = "{\"data\":\"test\"}",
                    CreatedAtUtc = DateTime.UtcNow.AddMinutes(-i)
                });
            }
            dbContext.SaveChanges();
        }

        // Act - Add new messages
        var addScenario = Scenario.Create("add_messages", async context =>
        {
            await using var dbContext = new TestDbContext(options);
            var store = new OutboxStoreEF(dbContext);

            var message = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                NotificationType = "TestNotification",
                Content = "{\"data\":\"test\"}",
                CreatedAtUtc = DateTime.UtcNow
            };

            await store.AddAsync(message, CancellationToken.None);
            await store.SaveChangesAsync(CancellationToken.None);

            return Response.Ok();
        })
        .WithLoadSimulations(
            Simulation.Inject(rate: 50, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(10))
        );

        // Act - Mark messages as processed
        var markScenario = Scenario.Create("mark_processed", async context =>
        {
            await using var dbContext = new TestDbContext(options);
            var store = new OutboxStoreEF(dbContext);

            // Get a pending message
            var pending = await store.GetPendingMessagesAsync(
                batchSize: 1,
                maxRetries: 3,
                CancellationToken.None);

            var message = pending.FirstOrDefault();
            if (message != null)
            {
                await store.MarkAsProcessedAsync(message.Id, CancellationToken.None);
                await store.SaveChangesAsync(CancellationToken.None);
                return Response.Ok();
            }

            return Response.Fail<string>(statusCode: "no_pending");
        })
        .WithLoadSimulations(
            Simulation.Inject(rate: 50, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(10))
        );

        var stats = NBomberRunner
            .RegisterScenarios(addScenario, markScenario)
            .WithoutReports()
            .Run();

        // Assert
        var addScen = stats.ScenarioStats[0];
        var markScen = stats.ScenarioStats[1];

        _output.WriteLine($"Add - OK: {addScen.Ok.Request.Count}, Fail: {addScen.Fail.Request.Count}");
        _output.WriteLine($"Mark - OK: {markScen.Ok.Request.Count}, Fail: {markScen.Fail.Request.Count}");

        Assert.True(addScen.Ok.Request.Count > 450, $"Expected > 450 successful adds, got {addScen.Ok.Request.Count}");
        Assert.True(markScen.Ok.Request.Count > 450, $"Expected > 450 successful marks, got {markScen.Ok.Request.Count}");
    }
}
