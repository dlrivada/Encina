```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | keyCount | Mean        | Error         | StdDev       | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|---------------------------- |--------- |------------:|--------------:|-------------:|------:|--------:|--------:|--------:|--------:|----------:|------------:|
| **Invalidation_NoMatchingKeys** | **?**        |    **26.23 μs** |      **1.842 μs** |     **0.101 μs** |  **1.00** |    **0.00** |  **1.0376** |  **0.0305** |       **-** |  **17.34 KB** |        **1.00** |
|                             |          |             |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **5**        | **2,793.00 μs** |  **8,036.417 μs** |   **440.503 μs** |     **?** |       **?** | **16.6016** | **15.6250** | **15.6250** | **367.18 KB** |           **?** |
|                             |          |             |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **10**       | **6,463.81 μs** | **29,595.867 μs** | **1,622.249 μs** |     **?** |       **?** | **17.5781** | **16.6016** | **16.6016** | **715.19 KB** |           **?** |
|                             |          |             |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **25**       | **8,107.82 μs** | **27,926.351 μs** | **1,530.738 μs** |     **?** |       **?** | **19.5313** | **17.5781** | **17.5781** |  **937.3 KB** |           **?** |
