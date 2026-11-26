```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.00GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | concurrencyLevel | Mean        | Error      | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------------------- |----------------- |------------:|-----------:|---------:|------:|--------:|-------:|----------:|------------:|
| **Invalidation_ConcurrentCommands** | **10**               |   **341.56 μs** |  **22.268 μs** | **1.221 μs** |     **?** |       **?** | **1.9531** | **173.83 KB** |           **?** |
|                                 |                  |             |            |          |       |         |        |           |             |
| **Invalidation_ConcurrentCommands** | **50**               | **1,729.72 μs** | **100.169 μs** | **5.491 μs** |     **?** |       **?** | **9.7656** | **868.45 KB** |           **?** |
|                                 |                  |             |            |          |       |         |        |           |             |
| **Invalidation_NoMatchingKeys**     | **?**                |    **33.09 μs** |   **1.208 μs** | **0.066 μs** |  **1.00** |    **0.00** | **0.1831** |   **17.2 KB** |        **1.00** |
