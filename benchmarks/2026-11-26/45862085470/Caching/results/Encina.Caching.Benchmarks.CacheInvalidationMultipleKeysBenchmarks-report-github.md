```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | keyCount | Mean        | Error         | StdDev     | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|---------------------------- |--------- |------------:|--------------:|-----------:|------:|--------:|--------:|--------:|--------:|----------:|------------:|
| **Invalidation_NoMatchingKeys** | **?**        |    **24.65 μs** |      **0.066 μs** |   **0.004 μs** |  **1.00** |    **0.00** |  **0.1831** |       **-** |       **-** |  **17.07 KB** |        **1.00** |
|                             |          |             |               |            |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **5**        | **2,365.81 μs** |  **5,617.937 μs** | **307.938 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** | **367.19 KB** |           **?** |
|                             |          |             |               |            |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **10**       | **4,513.42 μs** | **11,909.217 μs** | **652.784 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** | **715.14 KB** |           **?** |
|                             |          |             |               |            |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **25**       | **5,724.23 μs** | **13,324.780 μs** | **730.376 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** | **938.62 KB** |           **?** |
