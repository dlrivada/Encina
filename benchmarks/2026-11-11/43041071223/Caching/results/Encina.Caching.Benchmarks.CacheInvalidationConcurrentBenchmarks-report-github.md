```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | concurrencyLevel | Mean        | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------------------- |----------------- |------------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| **Invalidation_ConcurrentCommands** | **10**               |   **295.85 μs** |  **52.789 μs** |  **2.894 μs** |     **?** |       **?** | **1.9531** | **173.81 KB** |           **?** |
|                                 |                  |             |            |           |       |         |        |           |             |
| **Invalidation_ConcurrentCommands** | **50**               | **1,480.77 μs** | **190.663 μs** | **10.451 μs** |     **?** |       **?** | **9.7656** | **868.44 KB** |           **?** |
|                                 |                  |             |            |           |       |         |        |           |             |
| **Invalidation_NoMatchingKeys**     | **?**                |    **28.60 μs** |   **4.154 μs** |  **0.228 μs** |  **1.00** |    **0.01** | **0.1831** |  **17.03 KB** |        **1.00** |
