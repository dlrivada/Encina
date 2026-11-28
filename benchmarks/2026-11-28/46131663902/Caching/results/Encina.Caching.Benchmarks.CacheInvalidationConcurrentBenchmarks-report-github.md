```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | concurrencyLevel | Mean        | Error      | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|-------------------------------- |----------------- |------------:|-----------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| **Invalidation_ConcurrentCommands** | **10**               |   **282.82 μs** |  **17.443 μs** |  **0.956 μs** |     **?** |       **?** | **10.2539** |      **-** | **173.81 KB** |           **?** |
|                                 |                  |             |            |           |       |         |         |        |           |             |
| **Invalidation_ConcurrentCommands** | **50**               | **1,404.24 μs** | **197.386 μs** | **10.819 μs** |     **?** |       **?** | **52.7344** | **1.9531** | **868.49 KB** |           **?** |
|                                 |                  |             |            |           |       |         |         |        |           |             |
| **Invalidation_NoMatchingKeys**     | **?**                |    **26.74 μs** |   **2.876 μs** |  **0.158 μs** |  **1.00** |    **0.01** |  **1.0071** | **0.0305** |  **16.93 KB** |        **1.00** |
