```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 2.77GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | concurrencyLevel | Mean        | Error      | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|-------------------------------- |----------------- |------------:|-----------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| **Invalidation_ConcurrentCommands** | **10**               |   **362.65 μs** | **229.114 μs** | **12.558 μs** |     **?** |       **?** | **10.2539** |      **-** | **173.81 KB** |           **?** |
|                                 |                  |             |            |           |       |         |         |        |           |             |
| **Invalidation_ConcurrentCommands** | **50**               | **1,810.35 μs** |  **41.940 μs** |  **2.299 μs** |     **?** |       **?** | **52.7344** | **1.9531** | **868.45 KB** |           **?** |
|                                 |                  |             |            |           |       |         |         |        |           |             |
| **Invalidation_NoMatchingKeys**     | **?**                |    **34.40 μs** |   **1.577 μs** |  **0.086 μs** |  **1.00** |    **0.00** |  **1.0376** |      **-** |  **17.21 KB** |        **1.00** |
