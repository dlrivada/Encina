```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | concurrencyLevel | Mean        | Error      | StdDev   | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|-------------------------------- |----------------- |------------:|-----------:|---------:|------:|--------:|--------:|----------:|------------:|
| **Invalidation_ConcurrentCommands** | **10**               |   **405.50 μs** |  **40.247 μs** | **2.206 μs** |     **?** |       **?** | **10.2539** | **173.83 KB** |           **?** |
|                                 |                  |             |            |          |       |         |         |           |             |
| **Invalidation_ConcurrentCommands** | **50**               | **2,025.30 μs** | **162.423 μs** | **8.903 μs** |     **?** |       **?** | **50.7813** | **868.46 KB** |           **?** |
|                                 |                  |             |            |          |       |         |         |           |             |
| **Invalidation_NoMatchingKeys**     | **?**                |    **37.68 μs** |   **2.462 μs** | **0.135 μs** |  **1.00** |    **0.00** |  **1.0376** |  **17.26 KB** |        **1.00** |
