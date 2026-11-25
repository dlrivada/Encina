```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | concurrencyLevel | Mean        | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------------------- |----------------- |------------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| **Invalidation_ConcurrentCommands** | **10**               |   **257.72 μs** |  **1.443 μs** | **0.079 μs** |     **?** |       **?** | **1.9531** |  **173.8 KB** |           **?** |
|                                 |                  |             |           |          |       |         |        |           |             |
| **Invalidation_ConcurrentCommands** | **50**               | **1,295.32 μs** | **25.456 μs** | **1.395 μs** |     **?** |       **?** | **9.7656** | **868.35 KB** |           **?** |
|                                 |                  |             |           |          |       |         |        |           |             |
| **Invalidation_NoMatchingKeys**     | **?**                |    **24.13 μs** |  **0.358 μs** | **0.020 μs** |  **1.00** |    **0.00** | **0.1831** |  **17.39 KB** |        **1.00** |
