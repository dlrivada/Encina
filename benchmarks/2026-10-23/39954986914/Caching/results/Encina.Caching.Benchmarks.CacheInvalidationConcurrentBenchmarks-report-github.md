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
| **Invalidation_ConcurrentCommands** | **10**               |   **413.10 μs** |  **12.259 μs** |  **0.672 μs** |     **?** |       **?** | **10.2539** | **173.79 KB** |           **?** |
|                                 |                  |             |            |           |       |         |         |           |             |
| **Invalidation_ConcurrentCommands** | **50**               | **2,052.33 μs** | **381.972 μs** | **20.937 μs** |     **?** |       **?** | **50.7813** | **868.45 KB** |           **?** |
|                                 |                  |             |            |           |       |         |         |           |             |
| **Invalidation_NoMatchingKeys**     | **?**                |    **39.38 μs** |   **3.099 μs** |  **0.170 μs** |  **1.00** |    **0.01** |  **1.0376** |  **17.17 KB** |        **1.00** |
