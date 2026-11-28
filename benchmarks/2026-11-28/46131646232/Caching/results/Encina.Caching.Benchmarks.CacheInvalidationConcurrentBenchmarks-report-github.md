```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6973P-C 4.06GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | concurrencyLevel | Mean        | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------------------- |----------------- |------------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| **Invalidation_ConcurrentCommands** | **10**               |   **233.89 μs** | **25.895 μs** | **1.419 μs** |     **?** |       **?** | **1.9531** | **173.82 KB** |           **?** |
|                                 |                  |             |           |          |       |         |        |           |             |
| **Invalidation_ConcurrentCommands** | **50**               | **1,165.94 μs** | **11.258 μs** | **0.617 μs** |     **?** |       **?** | **9.7656** | **868.42 KB** |           **?** |
|                                 |                  |             |           |          |       |         |        |           |             |
| **Invalidation_NoMatchingKeys**     | **?**                |    **22.74 μs** |  **3.974 μs** | **0.218 μs** |  **1.00** |    **0.01** | **0.1831** |  **17.29 KB** |        **1.00** |
