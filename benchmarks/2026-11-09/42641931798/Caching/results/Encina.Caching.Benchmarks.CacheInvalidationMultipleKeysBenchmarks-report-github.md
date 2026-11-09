```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.67GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | keyCount | Mean        | Error         | StdDev       | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|---------------------------- |--------- |------------:|--------------:|-------------:|------:|--------:|--------:|--------:|--------:|----------:|------------:|
| **Invalidation_NoMatchingKeys** | **?**        |    **26.01 μs** |      **4.589 μs** |     **0.252 μs** |  **1.00** |    **0.01** |  **1.0681** |  **0.0305** |       **-** |  **17.45 KB** |        **1.00** |
|                             |          |             |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **5**        | **2,735.22 μs** |  **7,877.504 μs** |   **431.793 μs** |     **?** |       **?** | **16.6016** | **15.6250** | **15.6250** | **367.16 KB** |           **?** |
|                             |          |             |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **10**       | **5,731.23 μs** | **18,281.198 μs** | **1,002.054 μs** |     **?** |       **?** | **17.5781** | **16.6016** | **16.6016** | **715.18 KB** |           **?** |
|                             |          |             |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **25**       | **8,236.91 μs** | **33,300.894 μs** | **1,825.334 μs** |     **?** |       **?** | **17.5781** | **15.6250** | **15.6250** | **909.11 KB** |           **?** |
