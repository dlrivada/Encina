```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.90GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | concurrencyLevel | Mean        | Error      | StdDev    | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|-------------------------------- |----------------- |------------:|-----------:|----------:|------:|--------:|--------:|----------:|------------:|
| **Invalidation_ConcurrentCommands** | **10**               |   **417.74 μs** |  **78.386 μs** |  **4.297 μs** |     **?** |       **?** | **10.2539** | **173.82 KB** |           **?** |
|                                 |                  |             |            |           |       |         |         |           |             |
| **Invalidation_ConcurrentCommands** | **50**               | **2,075.00 μs** | **355.481 μs** | **19.485 μs** |     **?** |       **?** | **50.7813** | **868.43 KB** |           **?** |
|                                 |                  |             |            |           |       |         |         |           |             |
| **Invalidation_NoMatchingKeys**     | **?**                |    **39.14 μs** |   **4.332 μs** |  **0.237 μs** |  **1.00** |    **0.01** |  **1.0376** |  **17.06 KB** |        **1.00** |
