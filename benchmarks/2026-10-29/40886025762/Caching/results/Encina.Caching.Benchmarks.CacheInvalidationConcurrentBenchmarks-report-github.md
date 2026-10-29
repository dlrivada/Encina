```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.63GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | concurrencyLevel | Mean        | Error     | StdDev   | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|-------------------------------- |----------------- |------------:|----------:|---------:|------:|--------:|--------:|----------:|------------:|
| **Invalidation_ConcurrentCommands** | **10**               |   **417.69 μs** |  **8.012 μs** | **0.439 μs** |     **?** |       **?** | **10.2539** |  **173.8 KB** |           **?** |
|                                 |                  |             |           |          |       |         |         |           |             |
| **Invalidation_ConcurrentCommands** | **50**               | **2,115.90 μs** | **86.649 μs** | **4.750 μs** |     **?** |       **?** | **50.7813** | **868.43 KB** |           **?** |
|                                 |                  |             |           |          |       |         |         |           |             |
| **Invalidation_NoMatchingKeys**     | **?**                |    **39.90 μs** |  **2.257 μs** | **0.124 μs** |  **1.00** |    **0.00** |  **1.0376** |  **17.29 KB** |        **1.00** |
