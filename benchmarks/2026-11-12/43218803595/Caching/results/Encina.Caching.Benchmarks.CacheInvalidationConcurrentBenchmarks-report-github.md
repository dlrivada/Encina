```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 11.0.100
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | concurrencyLevel | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|-------------------------------- |----------------- |----------:|-----------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| **Invalidation_ConcurrentCommands** | **10**               | **196.71 μs** |  **42.277 μs** |  **2.317 μs** |     **?** |       **?** | **10.4980** | **0.2441** | **173.82 KB** |           **?** |
|                                 |                  |           |            |           |       |         |         |        |           |             |
| **Invalidation_ConcurrentCommands** | **50**               | **989.08 μs** | **554.107 μs** | **30.373 μs** |     **?** |       **?** | **52.7344** | **2.9297** | **868.48 KB** |           **?** |
|                                 |                  |           |            |           |       |         |         |        |           |             |
| **Invalidation_NoMatchingKeys**     | **?**                |  **17.89 μs** |   **2.420 μs** |  **0.133 μs** |  **1.00** |    **0.01** |  **1.0376** | **0.0305** |  **17.36 KB** |        **1.00** |
