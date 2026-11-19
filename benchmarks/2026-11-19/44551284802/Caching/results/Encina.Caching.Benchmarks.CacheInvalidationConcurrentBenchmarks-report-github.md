```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | concurrencyLevel | Mean        | Error      | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|-------------------------------- |----------------- |------------:|-----------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| **Invalidation_ConcurrentCommands** | **10**               |   **288.09 μs** |  **40.037 μs** |  **2.195 μs** |     **?** |       **?** | **10.2539** |      **-** | **173.84 KB** |           **?** |
|                                 |                  |             |            |           |       |         |         |        |           |             |
| **Invalidation_ConcurrentCommands** | **50**               | **1,434.71 μs** | **235.734 μs** | **12.921 μs** |     **?** |       **?** | **52.7344** | **1.9531** | **868.41 KB** |           **?** |
|                                 |                  |             |            |           |       |         |         |        |           |             |
| **Invalidation_NoMatchingKeys**     | **?**                |    **26.63 μs** |   **1.011 μs** |  **0.055 μs** |  **1.00** |    **0.00** |  **1.0376** | **0.0305** |  **17.34 KB** |        **1.00** |
