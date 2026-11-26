```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.10GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | concurrencyLevel | Mean        | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------------------- |----------------- |------------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| **Invalidation_ConcurrentCommands** | **10**               |   **336.69 μs** |  **27.20 μs** |  **1.491 μs** |     **?** |       **?** | **1.9531** | **173.81 KB** |           **?** |
|                                 |                  |             |           |           |       |         |        |           |             |
| **Invalidation_ConcurrentCommands** | **50**               | **1,712.01 μs** | **332.84 μs** | **18.244 μs** |     **?** |       **?** | **9.7656** | **868.45 KB** |           **?** |
|                                 |                  |             |           |           |       |         |        |           |             |
| **Invalidation_NoMatchingKeys**     | **?**                |    **33.74 μs** |  **15.76 μs** |  **0.864 μs** |  **1.00** |    **0.03** | **0.1831** |  **16.92 KB** |        **1.00** |
