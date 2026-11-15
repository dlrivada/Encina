```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.67GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | concurrencyLevel | Mean        | Error      | StdDev   | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|-------------------------------- |----------------- |------------:|-----------:|---------:|------:|--------:|--------:|-------:|----------:|------------:|
| **Invalidation_ConcurrentCommands** | **10**               |   **284.30 μs** |  **26.236 μs** | **1.438 μs** |     **?** |       **?** | **10.2539** |      **-** | **173.82 KB** |           **?** |
|                                 |                  |             |            |          |       |         |         |        |           |             |
| **Invalidation_ConcurrentCommands** | **50**               | **1,446.68 μs** | **121.885 μs** | **6.681 μs** |     **?** |       **?** | **52.7344** | **1.9531** | **868.47 KB** |           **?** |
|                                 |                  |             |            |          |       |         |         |        |           |             |
| **Invalidation_NoMatchingKeys**     | **?**                |    **27.63 μs** |   **2.610 μs** | **0.143 μs** |  **1.00** |    **0.01** |  **1.0376** | **0.0305** |  **17.09 KB** |        **1.00** |
