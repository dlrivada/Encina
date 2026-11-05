```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | concurrencyLevel | Mean        | Error      | StdDev   | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|-------------------------------- |----------------- |------------:|-----------:|---------:|------:|--------:|--------:|----------:|------------:|
| **Invalidation_ConcurrentCommands** | **10**               |   **418.14 μs** |  **24.464 μs** | **1.341 μs** |     **?** |       **?** | **10.2539** | **173.81 KB** |           **?** |
|                                 |                  |             |            |          |       |         |         |           |             |
| **Invalidation_ConcurrentCommands** | **50**               | **2,053.05 μs** | **104.435 μs** | **5.724 μs** |     **?** |       **?** | **50.7813** | **868.39 KB** |           **?** |
|                                 |                  |             |            |          |       |         |         |           |             |
| **Invalidation_NoMatchingKeys**     | **?**                |    **39.19 μs** |   **3.618 μs** | **0.198 μs** |  **1.00** |    **0.01** |  **1.0376** |  **17.24 KB** |        **1.00** |
