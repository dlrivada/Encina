using Microsoft.EntityFrameworkCore;
using NBomber.CSharp;
using Encina.EntityFrameworkCore.Sagas;
using Xunit.Abstractions;

namespace Encina.EntityFrameworkCore.LoadTests.Sagas;

/// <summary>
/// Load tests for <see cref="SagaStoreEF"/>.
/// Verifies performance and concurrency handling under stress for saga pattern operations.
/// </summary>
[Trait("Category", "Load")]
public sealed class SagaStoreEFLoadTests
{
    private readonly ITestOutputHelper _output;

    public SagaStoreEFLoadTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void HighConcurrency_AddSagas_ShouldHandleLoad()
    {
        // Act
        var scenario = Scenario.Create("add_sagas", async context =>
        {
            // Create new DbContext per invocation for thread safety
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase($"SagaAdd_{context.InvocationNumber}")
                .Options;

            await using var dbContext = new TestDbContext(options);
            var store = new SagaStoreEF(dbContext);

            var saga = new SagaState
            {
                SagaId = Guid.NewGuid(),
                SagaType = "OrderProcessingSaga",
                Data = "{\"orderId\":123}",
                CurrentStep = 0,
                Status = SagaStatus.Running,
                StartedAtUtc = DateTime.UtcNow,
                LastUpdatedAtUtc = DateTime.UtcNow
            };

            await store.AddAsync(saga, CancellationToken.None);
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
        _output.WriteLine($"Add Sagas - OK: {scen.Ok.Request.Count}, Fail: {scen.Fail.Request.Count}, RPS: {scen.Ok.Request.RPS}");
        Assert.True(scen.Ok.Request.Count > 900, $"Expected > 900 successful adds, got {scen.Ok.Request.Count}");
    }

    [Fact]
    public void HighConcurrency_UpdateSagas_ShouldHandleLoad()
    {
        // Arrange - Seed database with sagas
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase("SagaUpdate")
            .Options;

        var sagaIds = new List<Guid>();
        using (var dbContext = new TestDbContext(options))
        {
            for (int i = 0; i < 100; i++)
            {
                var sagaId = Guid.NewGuid();
                sagaIds.Add(sagaId);
                dbContext.Set<SagaState>().Add(new SagaState
                {
                    SagaId = sagaId,
                    SagaType = "OrderProcessingSaga",
                    Data = "{\"orderId\":123}",
                    CurrentStep = 0,
                    Status = SagaStatus.Running,
                    StartedAtUtc = DateTime.UtcNow,
                    LastUpdatedAtUtc = DateTime.UtcNow
                });
            }
            dbContext.SaveChanges();
        }

        // Act
        var scenario = Scenario.Create("update_sagas", async context =>
        {
            await using var dbContext = new TestDbContext(options);
            var store = new SagaStoreEF(dbContext);

            var sagaId = sagaIds[(int)(context.InvocationNumber % sagaIds.Count)];
            var saga = await store.GetAsync(sagaId, CancellationToken.None);

            if (saga != null)
            {
                await store.UpdateAsync(saga, CancellationToken.None);
                await store.SaveChangesAsync(CancellationToken.None);
                return Response.Ok();
            }

            return Response.Fail<string>(statusCode: "not_found");
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
        _output.WriteLine($"Update Sagas - OK: {scen.Ok.Request.Count}, Fail: {scen.Fail.Request.Count}, RPS: {scen.Ok.Request.RPS}");
        Assert.True(scen.Ok.Request.Count > 900, $"Expected > 900 successful updates, got {scen.Ok.Request.Count}");
    }

    [Fact]
    public void MixedLoad_GetAndUpdate_ShouldHandleLoad()
    {
        // Arrange - Shared database
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase("SagaMixed")
            .Options;

        var sagaIds = new List<Guid>();
        using (var dbContext = new TestDbContext(options))
        {
            for (int i = 0; i < 100; i++)
            {
                var sagaId = Guid.NewGuid();
                sagaIds.Add(sagaId);
                dbContext.Set<SagaState>().Add(new SagaState
                {
                    SagaId = sagaId,
                    SagaType = "OrderProcessingSaga",
                    Data = "{\"orderId\":123}",
                    CurrentStep = 0,
                    Status = SagaStatus.Running,
                    StartedAtUtc = DateTime.UtcNow,
                    LastUpdatedAtUtc = DateTime.UtcNow
                });
            }
            dbContext.SaveChanges();
        }

        // Act - Read sagas
        var getScenario = Scenario.Create("get_sagas", async context =>
        {
            await using var dbContext = new TestDbContext(options);
            var store = new SagaStoreEF(dbContext);

            var sagaId = sagaIds[(int)(context.InvocationNumber % sagaIds.Count)];
            var saga = await store.GetAsync(sagaId, CancellationToken.None);

            return saga != null ? Response.Ok() : Response.Fail<string>(statusCode: "not_found");
        })
        .WithLoadSimulations(
            Simulation.Inject(rate: 50, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(10))
        );

        // Act - Update sagas
        var updateScenario = Scenario.Create("update_sagas", async context =>
        {
            await using var dbContext = new TestDbContext(options);
            var store = new SagaStoreEF(dbContext);

            var sagaId = sagaIds[(int)(context.InvocationNumber % sagaIds.Count)];
            var saga = await store.GetAsync(sagaId, CancellationToken.None);

            if (saga != null)
            {
                await store.UpdateAsync(saga, CancellationToken.None);
                await store.SaveChangesAsync(CancellationToken.None);
                return Response.Ok();
            }

            return Response.Fail<string>(statusCode: "not_found");
        })
        .WithLoadSimulations(
            Simulation.Inject(rate: 50, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(10))
        );

        var stats = NBomberRunner
            .RegisterScenarios(getScenario, updateScenario)
            .WithoutReports()
            .Run();

        // Assert
        var getScen = stats.ScenarioStats[0];
        var updateScen = stats.ScenarioStats[1];

        _output.WriteLine($"Get - OK: {getScen.Ok.Request.Count}, Fail: {getScen.Fail.Request.Count}");
        _output.WriteLine($"Update - OK: {updateScen.Ok.Request.Count}, Fail: {updateScen.Fail.Request.Count}");

        Assert.True(getScen.Ok.Request.Count > 450, $"Expected > 450 successful gets, got {getScen.Ok.Request.Count}");
        Assert.True(updateScen.Ok.Request.Count > 450, $"Expected > 450 successful updates, got {updateScen.Ok.Request.Count}");
    }
}
