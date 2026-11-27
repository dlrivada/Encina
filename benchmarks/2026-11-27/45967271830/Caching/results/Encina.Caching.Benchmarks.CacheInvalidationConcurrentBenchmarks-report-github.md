```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6973P-C 3.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | concurrencyLevel | Mean        | Error      | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------------------- |----------------- |------------:|-----------:|---------:|------:|--------:|-------:|----------:|------------:|
| **Invalidation_ConcurrentCommands** | **10**               |   **281.95 μs** |  **29.526 μs** | **1.618 μs** |     **?** |       **?** | **1.9531** | **173.82 KB** |           **?** |
|                                 |                  |             |            |          |       |         |        |           |             |
| **Invalidation_ConcurrentCommands** | **50**               | **1,417.97 μs** | **106.851 μs** | **5.857 μs** |     **?** |       **?** | **9.7656** | **868.44 KB** |           **?** |
|                                 |                  |             |            |          |       |         |        |           |             |
| **Invalidation_NoMatchingKeys**     | **?**                |    **27.51 μs** |   **0.436 μs** | **0.024 μs** |  **1.00** |    **0.00** | **0.1831** |  **17.09 KB** |        **1.00** |
