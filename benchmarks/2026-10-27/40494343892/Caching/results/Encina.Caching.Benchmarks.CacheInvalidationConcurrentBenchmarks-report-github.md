```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | concurrencyLevel | Mean        | Error      | StdDev   | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|-------------------------------- |----------------- |------------:|-----------:|---------:|------:|--------:|--------:|----------:|------------:|
| **Invalidation_ConcurrentCommands** | **10**               |   **422.89 μs** |  **58.756 μs** | **3.221 μs** |     **?** |       **?** | **10.2539** | **173.81 KB** |           **?** |
|                                 |                  |             |            |          |       |         |         |           |             |
| **Invalidation_ConcurrentCommands** | **50**               | **2,079.89 μs** | **142.154 μs** | **7.792 μs** |     **?** |       **?** | **50.7813** | **868.58 KB** |           **?** |
|                                 |                  |             |            |          |       |         |         |           |             |
| **Invalidation_NoMatchingKeys**     | **?**                |    **38.66 μs** |   **1.618 μs** | **0.089 μs** |  **1.00** |    **0.00** |  **1.0376** |  **17.02 KB** |        **1.00** |
