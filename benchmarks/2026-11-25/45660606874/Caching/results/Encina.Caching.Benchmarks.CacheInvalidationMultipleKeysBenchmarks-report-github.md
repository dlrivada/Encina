```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.83GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | keyCount | Mean        | Error         | StdDev     | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|---------------------------- |--------- |------------:|--------------:|-----------:|------:|--------:|--------:|--------:|--------:|----------:|------------:|
| **Invalidation_NoMatchingKeys** | **?**        |    **24.78 μs** |      **0.711 μs** |   **0.039 μs** |  **1.00** |    **0.00** |  **0.1831** |       **-** |       **-** |   **17.2 KB** |        **1.00** |
|                             |          |             |               |            |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **5**        | **2,355.21 μs** |  **5,737.981 μs** | **314.518 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** | **367.18 KB** |           **?** |
|                             |          |             |               |            |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **10**       | **4,434.68 μs** | **12,289.197 μs** | **673.612 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** | **715.15 KB** |           **?** |
|                             |          |             |               |            |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **25**       | **5,705.96 μs** | **13,628.314 μs** | **747.014 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** | **909.13 KB** |           **?** |
