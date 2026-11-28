```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.26GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | keyCount | Mean        | Error         | StdDev     | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|---------------------------- |--------- |------------:|--------------:|-----------:|------:|--------:|--------:|--------:|--------:|----------:|------------:|
| **Invalidation_NoMatchingKeys** | **?**        |    **24.09 μs** |      **3.741 μs** |   **0.205 μs** |  **1.00** |    **0.01** |  **0.1831** |       **-** |       **-** |  **17.15 KB** |        **1.00** |
|                             |          |             |               |            |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **5**        | **2,311.03 μs** |  **5,184.536 μs** | **284.182 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** | **367.19 KB** |           **?** |
|                             |          |             |               |            |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **10**       | **4,382.25 μs** | **12,312.047 μs** | **674.865 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** | **715.17 KB** |           **?** |
|                             |          |             |               |            |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **25**       | **5,603.75 μs** | **13,668.862 μs** | **749.236 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** | **938.57 KB** |           **?** |
