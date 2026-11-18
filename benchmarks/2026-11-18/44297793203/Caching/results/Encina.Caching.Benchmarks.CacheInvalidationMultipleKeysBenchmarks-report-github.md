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
| **Invalidation_NoMatchingKeys** | **?**        |    **24.47 μs** |      **0.915 μs** |   **0.050 μs** |  **1.00** |    **0.00** |  **0.1831** |       **-** |       **-** |   **17.3 KB** |        **1.00** |
|                             |          |             |               |            |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **5**        | **2,346.35 μs** |  **5,735.489 μs** | **314.382 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** |  **367.2 KB** |           **?** |
|                             |          |             |               |            |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **10**       | **4,532.36 μs** | **12,159.677 μs** | **666.513 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** | **715.16 KB** |           **?** |
|                             |          |             |               |            |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **25**       | **5,671.54 μs** | **14,102.739 μs** | **773.019 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** | **909.16 KB** |           **?** |
