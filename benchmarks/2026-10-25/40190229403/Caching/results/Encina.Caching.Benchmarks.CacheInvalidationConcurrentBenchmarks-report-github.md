```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | concurrencyLevel | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|-------------------------------- |----------------- |----------:|----------:|---------:|------:|--------:|--------:|-------:|----------:|------------:|
| **Invalidation_ConcurrentCommands** | **10**               | **189.78 μs** | **14.410 μs** | **0.790 μs** |     **?** |       **?** | **10.4980** | **0.2441** | **173.82 KB** |           **?** |
|                                 |                  |           |           |          |       |         |         |        |           |             |
| **Invalidation_ConcurrentCommands** | **50**               | **961.26 μs** | **81.658 μs** | **4.476 μs** |     **?** |       **?** | **52.7344** | **2.9297** | **868.49 KB** |           **?** |
|                                 |                  |           |           |          |       |         |         |        |           |             |
| **Invalidation_NoMatchingKeys**     | **?**                |  **17.75 μs** |  **1.735 μs** | **0.095 μs** |  **1.00** |    **0.01** |  **1.0376** | **0.0305** |  **17.29 KB** |        **1.00** |
