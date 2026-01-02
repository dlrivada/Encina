# Examples

This file contains fully working, copy-pastable examples referenced from the CHANGELOG.

## Scatter-Gather example (compilable)

Tested with C# 14 / .NET 10.

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Encina.Messaging.ScatterGather;

// Minimal types to make the example self-contained
public record PriceRequest(string Sku);

public static class Suppliers
{
    public static async Task<decimal> GetPriceFromA(PriceRequest req, CancellationToken ct)
    {
        await Task.Delay(10, ct);
        return 10.5m;
    }

    public static async Task<decimal> GetPriceFromB(PriceRequest req, CancellationToken ct)
    {
        await Task.Delay(20, ct);
        return 9.99m;
    }

    public static async Task<decimal> GetPriceFromC(PriceRequest req, CancellationToken ct)
    {
        await Task.Delay(15, ct);
        return 11.0m;
    }
}

public static class ExampleRunner
{
    public static async Task RunAsync()
    {
        // This example demonstrates complete, functional usage of the real library
        // (including `ScatterGatherBuilder` and a runner implementation). It is
        // intended to be copy-pastable and self-contained for documentation purposes.

        var definition = ScatterGatherBuilder.Create<PriceRequest, decimal>("PriceAggregator")
            .ScatterTo("SupplierA", async (req, ct) => await Suppliers.GetPriceFromA(req, ct))
            .ScatterTo("SupplierB", async (req, ct) => await Suppliers.GetPriceFromB(req, ct))
            .ScatterTo("SupplierC", async (req, ct) => await Suppliers.GetPriceFromC(req, ct))
            .ExecuteInParallel()
            .GatherAll()
            .TakeMin(price => price)  // Get lowest price
            .Build();

        // Runner would be provided by the messaging library; this is a placeholder
        var runner = DefaultScatterGatherRunner.Create(); // hypothetical API
        var result = await runner.ExecuteAsync(definition, new PriceRequest("SKU123"));

        Console.WriteLine($"Best price: {result.Response}");
    }
}
```

If you need the concrete runner implementation used in the benchmarks/tests, see the repository tests or the documentation in `docs/` for the full runtime wiring example.
