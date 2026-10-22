```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.67GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | concurrencyLevel | Mean        | Error     | StdDev   | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|-------------------------------- |----------------- |------------:|----------:|---------:|------:|--------:|--------:|-------:|----------:|------------:|
| **Invalidation_ConcurrentCommands** | **10**               |   **279.77 μs** | **24.216 μs** | **1.327 μs** |     **?** |       **?** | **10.2539** |      **-** | **173.82 KB** |           **?** |
|                                 |                  |             |           |          |       |         |         |        |           |             |
| **Invalidation_ConcurrentCommands** | **50**               | **1,392.23 μs** | **95.167 μs** | **5.216 μs** |     **?** |       **?** | **52.7344** | **1.9531** | **868.47 KB** |           **?** |
|                                 |                  |             |           |          |       |         |         |        |           |             |
| **Invalidation_NoMatchingKeys**     | **?**                |    **26.45 μs** |  **2.038 μs** | **0.112 μs** |  **1.00** |    **0.01** |  **1.0376** | **0.0305** |  **17.04 KB** |        **1.00** |
