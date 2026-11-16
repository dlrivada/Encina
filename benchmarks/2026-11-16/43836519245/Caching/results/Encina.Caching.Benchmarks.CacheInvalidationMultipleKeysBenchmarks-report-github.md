```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.95GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | keyCount | Mean        | Error         | StdDev     | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|---------------------------- |--------- |------------:|--------------:|-----------:|------:|--------:|--------:|--------:|--------:|----------:|------------:|
| **Invalidation_NoMatchingKeys** | **?**        |    **25.24 μs** |      **1.621 μs** |   **0.089 μs** |  **1.00** |    **0.00** |  **0.1831** |       **-** |       **-** |  **16.95 KB** |        **1.00** |
|                             |          |             |               |            |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **5**        | **2,372.36 μs** |  **5,683.783 μs** | **311.547 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** | **367.19 KB** |           **?** |
|                             |          |             |               |            |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **10**       | **4,446.90 μs** | **11,095.665 μs** | **608.191 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** | **715.15 KB** |           **?** |
|                             |          |             |               |            |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **25**       | **5,701.34 μs** | **13,200.790 μs** | **723.580 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** | **909.14 KB** |           **?** |
