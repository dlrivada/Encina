```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | keyCount | Mean        | Error         | StdDev       | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|---------------------------- |--------- |------------:|--------------:|-------------:|------:|--------:|--------:|--------:|--------:|----------:|------------:|
| **Invalidation_NoMatchingKeys** | **?**        |    **24.02 μs** |      **1.841 μs** |     **0.101 μs** |  **1.00** |    **0.01** |  **0.1831** |       **-** |       **-** |   **17.3 KB** |        **1.00** |
|                             |          |             |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **5**        | **3,111.01 μs** |  **7,836.069 μs** |   **429.521 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** | **367.16 KB** |           **?** |
|                             |          |             |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **10**       | **6,124.58 μs** | **19,974.666 μs** | **1,094.879 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** | **715.14 KB** |           **?** |
|                             |          |             |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **25**       | **7,966.77 μs** | **20,659.909 μs** | **1,132.439 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** | **909.14 KB** |           **?** |
