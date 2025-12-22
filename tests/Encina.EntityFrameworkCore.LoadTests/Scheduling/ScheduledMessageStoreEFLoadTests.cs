using Microsoft.EntityFrameworkCore;
using NBomber.CSharp;
using Encina.EntityFrameworkCore.Scheduling;
using Xunit.Abstractions;

namespace Encina.EntityFrameworkCore.LoadTests.Scheduling;

/// <summary>
/// Load tests for <see cref="ScheduledMessageStoreEF"/>.
/// Verifies performance and concurrency handling under stress for scheduling pattern operations.
/// </summary>
[Trait("Category", "Load")]
public sealed class ScheduledMessageStoreEFLoadTests
{
    private readonly ITestOutputHelper _output;

    public ScheduledMessageStoreEFLoadTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void HighConcurrency_AddScheduled_ShouldHandleLoad()
    {
        // Act
        var scenario = Scenario.Create("add_scheduled_messages", async context =>
        {
            // Create new DbContext per invocation for thread safety
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase($"ScheduledAdd_{context.InvocationNumber}")
                .Options;

            await using var dbContext = new TestDbContext(options);
            var store = new ScheduledMessageStoreEF(dbContext);

            var message = new ScheduledMessage
            {
                Id = Guid.NewGuid(),
                RequestType = "ScheduledCommand",
                Content = "{\"data\":\"test\"}",
                ScheduledAtUtc = DateTime.UtcNow.AddMinutes(5),
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
        _output.WriteLine($"Add Scheduled - OK: {scen.Ok.Request.Count}, Fail: {scen.Fail.Request.Count}, RPS: {scen.Ok.Request.RPS}");
        Assert.True(scen.Ok.Request.Count > 900, $"Expected > 900 successful adds, got {scen.Ok.Request.Count}");
    }

    [Fact]
    public void HighConcurrency_GetDue_ShouldHandleLoad()
    {
        // Arrange - Seed database with due messages
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase("ScheduledGetDue")
            .Options;

        using (var dbContext = new TestDbContext(options))
        {
            for (int i = 0; i < 100; i++)
            {
                dbContext.Set<ScheduledMessage>().Add(new ScheduledMessage
                {
                    Id = Guid.NewGuid(),
                    RequestType = "ScheduledCommand",
                    Content = "{\"data\":\"test\"}",
                    ScheduledAtUtc = DateTime.UtcNow.AddMinutes(-i), // Due now
                    CreatedAtUtc = DateTime.UtcNow.AddMinutes(-i)
                });
            }
            dbContext.SaveChanges();
        }

        // Act
        var scenario = Scenario.Create("get_due_messages", async context =>
        {
            await using var dbContext = new TestDbContext(options);
            var store = new ScheduledMessageStoreEF(dbContext);

            var dueMessages = await store.GetDueMessagesAsync(
                batchSize: 10,
                maxRetries: 3,
                CancellationToken.None);

            return dueMessages.Any() ? Response.Ok() : Response.Fail<string>(statusCode: "no_due_messages");
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
        _output.WriteLine($"Get Due - OK: {scen.Ok.Request.Count}, Fail: {scen.Fail.Request.Count}, RPS: {scen.Ok.Request.RPS}");
        Assert.True(scen.Ok.Request.Count > 900, $"Expected > 900 successful reads, got {scen.Ok.Request.Count}");
    }

    [Fact]
    public void MixedLoad_AddAndProcess_ShouldHandleLoad()
    {
        // Arrange - Shared database
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase("ScheduledMixed")
            .Options;

        // Seed initial due messages
        using (var dbContext = new TestDbContext(options))
        {
            for (int i = 0; i < 500; i++)
            {
                dbContext.Set<ScheduledMessage>().Add(new ScheduledMessage
                {
                    Id = Guid.NewGuid(),
                    RequestType = "ScheduledCommand",
                    Content = "{\"data\":\"test\"}",
                    ScheduledAtUtc = DateTime.UtcNow.AddMinutes(-i), // Due now
                    CreatedAtUtc = DateTime.UtcNow.AddMinutes(-i)
                });
            }
            dbContext.SaveChanges();
        }

        // Act - Add new scheduled messages
        var addScenario = Scenario.Create("add_messages", async context =>
        {
            await using var dbContext = new TestDbContext(options);
            var store = new ScheduledMessageStoreEF(dbContext);

            var message = new ScheduledMessage
            {
                Id = Guid.NewGuid(),
                RequestType = "ScheduledCommand",
                Content = "{\"data\":\"test\"}",
                ScheduledAtUtc = DateTime.UtcNow.AddMinutes(5),
                CreatedAtUtc = DateTime.UtcNow
            };

            await store.AddAsync(message, CancellationToken.None);
            await store.SaveChangesAsync(CancellationToken.None);

            return Response.Ok();
        })
        .WithLoadSimulations(
            Simulation.Inject(rate: 50, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(10))
        );

        // Act - Process due messages
        var processScenario = Scenario.Create("mark_processed", async context =>
        {
            await using var dbContext = new TestDbContext(options);
            var store = new ScheduledMessageStoreEF(dbContext);

            var dueMessages = await store.GetDueMessagesAsync(
                batchSize: 1,
                maxRetries: 3,
                CancellationToken.None);

            var message = dueMessages.FirstOrDefault();
            if (message != null)
            {
                await store.MarkAsProcessedAsync(message.Id, CancellationToken.None);
                await store.SaveChangesAsync(CancellationToken.None);
                return Response.Ok();
            }

            return Response.Fail<string>(statusCode: "no_due_messages");
        })
        .WithLoadSimulations(
            Simulation.Inject(rate: 50, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(10))
        );

        var stats = NBomberRunner
            .RegisterScenarios(addScenario, processScenario)
            .WithoutReports()
            .Run();

        // Assert
        var addScen = stats.ScenarioStats[0];
        var processScen = stats.ScenarioStats[1];

        _output.WriteLine($"Add - OK: {addScen.Ok.Request.Count}, Fail: {addScen.Fail.Request.Count}");
        _output.WriteLine($"Process - OK: {processScen.Ok.Request.Count}, Fail: {processScen.Fail.Request.Count}");

        Assert.True(addScen.Ok.Request.Count > 450, $"Expected > 450 successful adds, got {addScen.Ok.Request.Count}");
        Assert.True(processScen.Ok.Request.Count > 450, $"Expected > 450 successful processes, got {processScen.Ok.Request.Count}");
    }
}
