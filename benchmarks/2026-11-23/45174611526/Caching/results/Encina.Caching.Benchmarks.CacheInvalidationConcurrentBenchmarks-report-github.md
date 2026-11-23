```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 2.99GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | concurrencyLevel | Mean        | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------------------- |----------------- |------------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| **Invalidation_ConcurrentCommands** | **10**               |   **343.80 μs** |  **7.050 μs** | **0.386 μs** |     **?** |       **?** | **1.9531** | **173.81 KB** |           **?** |
|                                 |                  |             |           |          |       |         |        |           |             |
| **Invalidation_ConcurrentCommands** | **50**               | **1,722.85 μs** | **28.095 μs** | **1.540 μs** |     **?** |       **?** | **9.7656** | **868.41 KB** |           **?** |
|                                 |                  |             |           |          |       |         |        |           |             |
| **Invalidation_NoMatchingKeys**     | **?**                |    **32.68 μs** |  **1.360 μs** | **0.075 μs** |  **1.00** |    **0.00** | **0.1831** |  **17.16 KB** |        **1.00** |
