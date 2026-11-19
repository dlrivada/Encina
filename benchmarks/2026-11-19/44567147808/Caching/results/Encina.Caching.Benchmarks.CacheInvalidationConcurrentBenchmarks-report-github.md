```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.28GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | concurrencyLevel | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|-------------------------------- |----------------- |----------:|----------:|---------:|------:|--------:|--------:|-------:|----------:|------------:|
| **Invalidation_ConcurrentCommands** | **10**               | **193.99 μs** | **152.96 μs** | **8.384 μs** |     **?** |       **?** | **10.4980** | **0.2441** | **173.82 KB** |           **?** |
|                                 |                  |           |           |          |       |         |         |        |           |             |
| **Invalidation_ConcurrentCommands** | **50**               | **944.96 μs** |  **77.26 μs** | **4.235 μs** |     **?** |       **?** | **52.7344** | **2.9297** | **868.43 KB** |           **?** |
|                                 |                  |           |           |          |       |         |         |        |           |             |
| **Invalidation_NoMatchingKeys**     | **?**                |  **19.09 μs** |  **15.61 μs** | **0.855 μs** |  **1.00** |    **0.05** |  **1.0376** | **0.0305** |  **17.26 KB** |        **1.00** |
