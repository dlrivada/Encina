```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]    : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                          | concurrencyLevel | Mean        | Error    | StdDev   | Median      | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|-------------------------------- |----------------- |------------:|---------:|---------:|------------:|------:|--------:|--------:|----------:|------------:|
| **Invalidation_ConcurrentCommands** | **10**               |   **406.45 μs** | **0.931 μs** | **1.274 μs** |   **406.61 μs** |     **?** |       **?** |  **6.8359** | **173.81 KB** |           **?** |
|                                 |                  |             |          |          |             |       |         |         |           |             |
| **Invalidation_ConcurrentCommands** | **50**               | **2,009.99 μs** | **2.629 μs** | **3.598 μs** | **2,009.33 μs** |     **?** |       **?** | **35.1563** | **868.54 KB** |           **?** |
|                                 |                  |             |          |          |             |       |         |         |           |             |
| **Invalidation_NoMatchingKeys**     | **?**                |    **39.47 μs** | **0.377 μs** | **0.529 μs** |    **39.09 μs** |  **1.00** |    **0.02** |  **0.6714** |  **17.07 KB** |        **1.00** |
