```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.02GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]    : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                          | concurrencyLevel | Mean        | Error    | StdDev   | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|-------------------------------- |----------------- |------------:|---------:|---------:|------:|--------:|--------:|----------:|------------:|
| **Invalidation_ConcurrentCommands** | **10**               |   **413.92 μs** | **1.517 μs** | **2.270 μs** |     **?** |       **?** | **10.2539** | **173.83 KB** |           **?** |
|                                 |                  |             |          |          |       |         |         |           |             |
| **Invalidation_ConcurrentCommands** | **50**               | **2,071.97 μs** | **5.790 μs** | **8.116 μs** |     **?** |       **?** | **50.7813** | **868.39 KB** |           **?** |
|                                 |                  |             |          |          |       |         |         |           |             |
| **Invalidation_NoMatchingKeys**     | **?**                |    **39.84 μs** | **0.247 μs** | **0.362 μs** |  **1.00** |    **0.01** |  **1.0376** |  **17.09 KB** |        **1.00** |
