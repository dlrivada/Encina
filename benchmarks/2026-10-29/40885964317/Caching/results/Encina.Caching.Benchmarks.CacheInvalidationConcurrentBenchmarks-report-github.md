```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.63GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | concurrencyLevel | Mean        | Error      | StdDev    | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|-------------------------------- |----------------- |------------:|-----------:|----------:|------:|--------:|--------:|----------:|------------:|
| **Invalidation_ConcurrentCommands** | **10**               |   **417.15 μs** |  **32.331 μs** |  **1.772 μs** |     **?** |       **?** | **10.2539** | **173.81 KB** |           **?** |
|                                 |                  |             |            |           |       |         |         |           |             |
| **Invalidation_ConcurrentCommands** | **50**               | **2,119.36 μs** | **216.517 μs** | **11.868 μs** |     **?** |       **?** | **50.7813** | **868.36 KB** |           **?** |
|                                 |                  |             |            |           |       |         |         |           |             |
| **Invalidation_NoMatchingKeys**     | **?**                |    **38.41 μs** |   **0.449 μs** |  **0.025 μs** |  **1.00** |    **0.00** |  **1.0376** |  **17.27 KB** |        **1.00** |
