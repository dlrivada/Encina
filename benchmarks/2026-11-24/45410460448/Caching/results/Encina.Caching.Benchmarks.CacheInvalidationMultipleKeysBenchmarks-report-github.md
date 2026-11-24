```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.16GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | keyCount | Mean        | Error         | StdDev       | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|---------------------------- |--------- |------------:|--------------:|-------------:|------:|--------:|--------:|--------:|--------:|----------:|------------:|
| **Invalidation_NoMatchingKeys** | **?**        |    **18.21 μs** |      **4.415 μs** |     **0.242 μs** |  **1.00** |    **0.02** |  **1.0376** |  **0.0305** |       **-** |  **17.32 KB** |        **1.00** |
|                             |          |             |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **5**        | **2,210.10 μs** |  **8,416.575 μs** |   **461.341 μs** |     **?** |       **?** | **16.6016** | **15.6250** | **15.6250** | **367.21 KB** |           **?** |
|                             |          |             |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **10**       | **4,717.96 μs** | **26,526.639 μs** | **1,454.015 μs** |     **?** |       **?** | **16.6016** | **15.6250** | **15.6250** | **715.15 KB** |           **?** |
|                             |          |             |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **25**       | **8,531.63 μs** | **43,141.595 μs** | **2,364.736 μs** |     **?** |       **?** | **17.5781** | **15.6250** | **15.6250** | **909.12 KB** |           **?** |
