```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.72GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | concurrencyLevel | Mean        | Error     | StdDev   | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|-------------------------------- |----------------- |------------:|----------:|---------:|------:|--------:|--------:|----------:|------------:|
| **Invalidation_ConcurrentCommands** | **10**               |   **412.53 μs** |  **6.926 μs** | **0.380 μs** |     **?** |       **?** | **10.2539** |  **173.8 KB** |           **?** |
|                                 |                  |             |           |          |       |         |         |           |             |
| **Invalidation_ConcurrentCommands** | **50**               | **2,121.88 μs** | **48.282 μs** | **2.646 μs** |     **?** |       **?** | **50.7813** | **868.36 KB** |           **?** |
|                                 |                  |             |           |          |       |         |         |           |             |
| **Invalidation_NoMatchingKeys**     | **?**                |    **39.69 μs** |  **1.276 μs** | **0.070 μs** |  **1.00** |    **0.00** |  **1.0376** |  **17.14 KB** |        **1.00** |
