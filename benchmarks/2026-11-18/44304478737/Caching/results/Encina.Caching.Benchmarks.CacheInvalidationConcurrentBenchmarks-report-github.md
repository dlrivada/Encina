```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.22GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | concurrencyLevel | Mean        | Error      | StdDev    | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|-------------------------------- |----------------- |------------:|-----------:|----------:|------:|--------:|--------:|----------:|------------:|
| **Invalidation_ConcurrentCommands** | **10**               |   **408.36 μs** |  **17.938 μs** |  **0.983 μs** |     **?** |       **?** | **10.2539** | **173.82 KB** |           **?** |
|                                 |                  |             |            |           |       |         |         |           |             |
| **Invalidation_ConcurrentCommands** | **50**               | **2,069.28 μs** | **305.817 μs** | **16.763 μs** |     **?** |       **?** | **50.7813** | **868.57 KB** |           **?** |
|                                 |                  |             |            |           |       |         |         |           |             |
| **Invalidation_NoMatchingKeys**     | **?**                |    **39.29 μs** |   **4.361 μs** |  **0.239 μs** |  **1.00** |    **0.01** |  **1.0376** |  **17.23 KB** |        **1.00** |
