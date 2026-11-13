```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.65GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | concurrencyLevel | Mean        | Error      | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|-------------------------------- |----------------- |------------:|-----------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| **Invalidation_ConcurrentCommands** | **10**               |   **282.71 μs** |   **8.689 μs** |  **0.476 μs** |     **?** |       **?** | **10.2539** |      **-** | **173.83 KB** |           **?** |
|                                 |                  |             |            |           |       |         |         |        |           |             |
| **Invalidation_ConcurrentCommands** | **50**               | **1,435.19 μs** | **431.264 μs** | **23.639 μs** |     **?** |       **?** | **52.7344** | **1.9531** | **868.48 KB** |           **?** |
|                                 |                  |             |            |           |       |         |         |        |           |             |
| **Invalidation_NoMatchingKeys**     | **?**                |    **26.79 μs** |   **4.953 μs** |  **0.272 μs** |  **1.00** |    **0.01** |  **1.0376** | **0.0305** |  **17.26 KB** |        **1.00** |
