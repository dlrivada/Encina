```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6973P-C 3.98GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | keyCount | Mean        | Error         | StdDev       | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|---------------------------- |--------- |------------:|--------------:|-------------:|------:|--------:|--------:|--------:|--------:|----------:|------------:|
| **Invalidation_NoMatchingKeys** | **?**        |    **24.18 μs** |      **6.895 μs** |     **0.378 μs** |  **1.00** |    **0.02** |  **0.1831** |       **-** |       **-** |  **17.14 KB** |        **1.00** |
|                             |          |             |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **5**        | **3,151.78 μs** |  **7,214.672 μs** |   **395.461 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** | **367.17 KB** |           **?** |
|                             |          |             |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **10**       | **6,010.09 μs** | **18,806.683 μs** | **1,030.858 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** | **715.16 KB** |           **?** |
|                             |          |             |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **25**       | **8,389.44 μs** | **19,364.366 μs** | **1,061.426 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** | **909.14 KB** |           **?** |
