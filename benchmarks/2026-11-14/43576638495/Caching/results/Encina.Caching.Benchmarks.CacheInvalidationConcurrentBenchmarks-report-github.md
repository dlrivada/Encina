```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.32GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | concurrencyLevel | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|-------------------------------- |----------------- |----------:|----------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| **Invalidation_ConcurrentCommands** | **10**               | **194.74 μs** |  **39.93 μs** |  **2.189 μs** |     **?** |       **?** | **10.4980** | **0.2441** | **173.82 KB** |           **?** |
|                                 |                  |           |           |           |       |         |         |        |           |             |
| **Invalidation_ConcurrentCommands** | **50**               | **971.88 μs** | **381.54 μs** | **20.913 μs** |     **?** |       **?** | **52.7344** | **1.9531** | **868.47 KB** |           **?** |
|                                 |                  |           |           |           |       |         |         |        |           |             |
| **Invalidation_NoMatchingKeys**     | **?**                |  **18.45 μs** |  **10.62 μs** |  **0.582 μs** |  **1.00** |    **0.04** |  **1.0376** | **0.0305** |  **17.31 KB** |        **1.00** |
