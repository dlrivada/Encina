```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.61GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | concurrencyLevel | Mean        | Error      | StdDev   | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|-------------------------------- |----------------- |------------:|-----------:|---------:|------:|--------:|--------:|----------:|------------:|
| **Invalidation_ConcurrentCommands** | **10**               |   **424.65 μs** |  **76.258 μs** | **4.180 μs** |     **?** |       **?** | **10.2539** | **173.82 KB** |           **?** |
|                                 |                  |             |            |          |       |         |         |           |             |
| **Invalidation_ConcurrentCommands** | **50**               | **2,105.68 μs** | **144.919 μs** | **7.943 μs** |     **?** |       **?** | **50.7813** | **868.47 KB** |           **?** |
|                                 |                  |             |            |          |       |         |         |           |             |
| **Invalidation_NoMatchingKeys**     | **?**                |    **40.04 μs** |   **6.105 μs** | **0.335 μs** |  **1.00** |    **0.01** |  **0.9766** |   **16.9 KB** |        **1.00** |
