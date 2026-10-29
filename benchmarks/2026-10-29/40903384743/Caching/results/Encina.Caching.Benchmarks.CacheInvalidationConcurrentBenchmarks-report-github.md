```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.52GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | concurrencyLevel | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|-------------------------------- |----------------- |----------:|----------:|---------:|------:|--------:|--------:|-------:|----------:|------------:|
| **Invalidation_ConcurrentCommands** | **10**               | **194.10 μs** | **40.806 μs** | **2.237 μs** |     **?** |       **?** | **10.4980** | **0.2441** | **173.82 KB** |           **?** |
|                                 |                  |           |           |          |       |         |         |        |           |             |
| **Invalidation_ConcurrentCommands** | **50**               | **924.72 μs** | **90.830 μs** | **4.979 μs** |     **?** |       **?** | **52.7344** | **2.9297** | **868.46 KB** |           **?** |
|                                 |                  |           |           |          |       |         |         |        |           |             |
| **Invalidation_NoMatchingKeys**     | **?**                |  **18.35 μs** |  **2.066 μs** | **0.113 μs** |  **1.00** |    **0.01** |  **1.0376** | **0.0305** |  **17.01 KB** |        **1.00** |
