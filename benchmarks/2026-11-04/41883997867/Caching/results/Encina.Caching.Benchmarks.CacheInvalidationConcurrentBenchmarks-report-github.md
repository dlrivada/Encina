```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.00GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | concurrencyLevel | Mean        | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------------------- |----------------- |------------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| **Invalidation_ConcurrentCommands** | **10**               |   **347.62 μs** | **197.896 μs** | **10.847 μs** |     **?** |       **?** | **1.9531** | **173.81 KB** |           **?** |
|                                 |                  |             |            |           |       |         |        |           |             |
| **Invalidation_ConcurrentCommands** | **50**               | **1,718.31 μs** | **220.755 μs** | **12.100 μs** |     **?** |       **?** | **9.7656** | **868.44 KB** |           **?** |
|                                 |                  |             |            |           |       |         |        |           |             |
| **Invalidation_NoMatchingKeys**     | **?**                |    **32.92 μs** |   **1.637 μs** |  **0.090 μs** |  **1.00** |    **0.00** | **0.1831** |  **17.12 KB** |        **1.00** |
