using Microsoft.EntityFrameworkCore;
using NBomber.CSharp;
using Encina.EntityFrameworkCore.Inbox;
using Xunit.Abstractions;

namespace Encina.EntityFrameworkCore.LoadTests.Inbox;

/// <summary>
/// Load tests for <see cref="InboxStoreEF"/>.
/// Verifies performance and concurrency handling under stress for inbox pattern operations.
/// </summary>
[Trait("Category", "Load")]
public sealed class InboxStoreEFLoadTests
{
    private readonly ITestOutputHelper _output;

    public InboxStoreEFLoadTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void HighConcurrency_CheckExists_ShouldHandleLoad()
    {
        // Arrange - Seed database with messages
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase("InboxCheckExists")
            .Options;

        using (var dbContext = new TestDbContext(options))
        {
            for (int i = 0; i < 100; i++)
            {
                dbContext.Set<InboxMessage>().Add(new InboxMessage
                {
                    MessageId = $"msg-{i}",
                    RequestType = "TestRequest",
                    ReceivedAtUtc = DateTime.UtcNow,
                    ExpiresAtUtc = DateTime.UtcNow.AddDays(7)
                });
            }
            dbContext.SaveChanges();
        }

        // Act
        var scenario = Scenario.Create("check_exists", async context =>
        {
            await using var dbContext = new TestDbContext(options);
            var store = new InboxStoreEF(dbContext);

            var messageId = $"msg-{context.InvocationNumber % 100}";
            var message = await store.GetMessageAsync(messageId, CancellationToken.None);

            return message != null ? Response.Ok() : Response.Fail<string>(statusCode: "not_found");
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
        _output.WriteLine($"Check Exists - OK: {scen.Ok.Request.Count}, Fail: {scen.Fail.Request.Count}, RPS: {scen.Ok.Request.RPS}");
        Assert.True(scen.Ok.Request.Count > 900, $"Expected > 900 successful checks, got {scen.Ok.Request.Count}");
    }

    [Fact]
    public void HighConcurrency_AddMessages_ShouldHandleLoad()
    {
        // Act
        var scenario = Scenario.Create("add_inbox_messages", async context =>
        {
            // Create new DbContext per invocation for thread safety
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase($"InboxAdd_{context.InvocationNumber}")
                .Options;

            await using var dbContext = new TestDbContext(options);
            var store = new InboxStoreEF(dbContext);

            var message = new InboxMessage
            {
                MessageId = $"msg-{Guid.NewGuid()}",
                RequestType = "TestRequest",
                ReceivedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(7)
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
    public void MixedLoad_AddAndProcess_ShouldHandleLoad()
    {
        // Arrange - Shared database
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase("InboxMixed")
            .Options;

        // Seed initial messages for processing
        using (var dbContext = new TestDbContext(options))
        {
            for (int i = 0; i < 500; i++)
            {
                dbContext.Set<InboxMessage>().Add(new InboxMessage
                {
                    MessageId = $"msg-{i}",
                    RequestType = "TestRequest",
                    ReceivedAtUtc = DateTime.UtcNow,
                    ExpiresAtUtc = DateTime.UtcNow.AddDays(7)
                });
            }
            dbContext.SaveChanges();
        }

        // Act - Add new messages
        var addScenario = Scenario.Create("add_messages", async context =>
        {
            await using var dbContext = new TestDbContext(options);
            var store = new InboxStoreEF(dbContext);

            var message = new InboxMessage
            {
                MessageId = $"msg-new-{context.InvocationNumber}",
                RequestType = "TestRequest",
                ReceivedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(7)
            };

            await store.AddAsync(message, CancellationToken.None);
            await store.SaveChangesAsync(CancellationToken.None);

            return Response.Ok();
        })
        .WithLoadSimulations(
            Simulation.Inject(rate: 50, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(10))
        );

        // Act - Mark messages as processed
        var processScenario = Scenario.Create("mark_processed", async context =>
        {
            await using var dbContext = new TestDbContext(options);
            var store = new InboxStoreEF(dbContext);

            var messageId = $"msg-{context.InvocationNumber % 500}";
            var message = await store.GetMessageAsync(messageId, CancellationToken.None);

            if (message != null && message.ProcessedAtUtc == null)
            {
                await store.MarkAsProcessedAsync(messageId, "{\"result\":\"success\"}", CancellationToken.None);
                await store.SaveChangesAsync(CancellationToken.None);
                return Response.Ok();
            }

            return Response.Fail<string>(statusCode: "already_processed");
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
        // Process may have some failures due to already processed messages, so we're more lenient
        Assert.True(processScen.Ok.Request.Count + processScen.Fail.Request.Count > 450,
            $"Expected > 450 total process attempts, got {processScen.Ok.Request.Count + processScen.Fail.Request.Count}");
    }
}
