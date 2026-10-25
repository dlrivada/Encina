```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | concurrencyLevel | Mean        | Error      | StdDev    | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|-------------------------------- |----------------- |------------:|-----------:|----------:|------:|--------:|--------:|----------:|------------:|
| **Invalidation_ConcurrentCommands** | **10**               |   **416.42 μs** |  **95.308 μs** |  **5.224 μs** |     **?** |       **?** | **10.2539** | **173.82 KB** |           **?** |
|                                 |                  |             |            |           |       |         |         |           |             |
| **Invalidation_ConcurrentCommands** | **50**               | **2,134.94 μs** | **362.045 μs** | **19.845 μs** |     **?** |       **?** | **50.7813** | **868.52 KB** |           **?** |
|                                 |                  |             |            |           |       |         |         |           |             |
| **Invalidation_NoMatchingKeys**     | **?**                |    **41.18 μs** |   **6.997 μs** |  **0.384 μs** |  **1.00** |    **0.01** |  **1.0376** |  **17.16 KB** |        **1.00** |
