```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | concurrencyLevel | Mean        | Error     | StdDev   | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|-------------------------------- |----------------- |------------:|----------:|---------:|------:|--------:|--------:|----------:|------------:|
| **Invalidation_ConcurrentCommands** | **10**               |   **411.16 μs** |  **5.846 μs** | **0.320 μs** |     **?** |       **?** |  **6.8359** | **173.81 KB** |           **?** |
|                                 |                  |             |           |          |       |         |         |           |             |
| **Invalidation_ConcurrentCommands** | **50**               | **2,016.82 μs** | **37.408 μs** | **2.050 μs** |     **?** |       **?** | **35.1563** | **868.48 KB** |           **?** |
|                                 |                  |             |           |          |       |         |         |           |             |
| **Invalidation_NoMatchingKeys**     | **?**                |    **38.88 μs** |  **0.853 μs** | **0.047 μs** |  **1.00** |    **0.00** |  **0.6714** |   **17.2 KB** |        **1.00** |
