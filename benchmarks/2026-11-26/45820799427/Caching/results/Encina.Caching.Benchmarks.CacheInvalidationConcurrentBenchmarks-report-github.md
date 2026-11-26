```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.16GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | concurrencyLevel | Mean        | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------------------- |----------------- |------------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| **Invalidation_ConcurrentCommands** | **10**               |   **257.34 μs** | **13.850 μs** | **0.759 μs** |     **?** |       **?** | **1.9531** | **173.81 KB** |           **?** |
|                                 |                  |             |           |          |       |         |        |           |             |
| **Invalidation_ConcurrentCommands** | **50**               | **1,284.67 μs** | **53.754 μs** | **2.946 μs** |     **?** |       **?** | **9.7656** |  **868.4 KB** |           **?** |
|                                 |                  |             |           |          |       |         |        |           |             |
| **Invalidation_NoMatchingKeys**     | **?**                |    **25.25 μs** |  **2.082 μs** | **0.114 μs** |  **1.00** |    **0.01** | **0.1831** |  **17.14 KB** |        **1.00** |
