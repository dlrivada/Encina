```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]    : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                          | concurrencyLevel | Mean        | Error     | StdDev    | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|-------------------------------- |----------------- |------------:|----------:|----------:|------:|--------:|--------:|----------:|------------:|
| **Invalidation_ConcurrentCommands** | **10**               |   **416.36 μs** |  **2.284 μs** |  **3.275 μs** |     **?** |       **?** | **10.2539** | **173.83 KB** |           **?** |
|                                 |                  |             |           |           |       |         |         |           |             |
| **Invalidation_ConcurrentCommands** | **50**               | **2,060.30 μs** | **29.114 μs** | **41.755 μs** |     **?** |       **?** | **50.7813** | **868.49 KB** |           **?** |
|                                 |                  |             |           |           |       |         |         |           |             |
| **Invalidation_NoMatchingKeys**     | **?**                |    **39.43 μs** |  **0.187 μs** |  **0.274 μs** |  **1.00** |    **0.01** |  **1.0376** |  **17.14 KB** |        **1.00** |
