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
| **Invalidation_NoMatchingKeys** | **?**        |    **26.70 μs** |      **1.987 μs** |     **0.109 μs** |  **1.00** |    **0.00** |  **1.0376** |  **0.0305** |       **-** |   **17.2 KB** |        **1.00** |
|                             |          |             |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **5**        | **2,772.66 μs** |  **7,857.302 μs** |   **430.685 μs** |     **?** |       **?** | **16.6016** | **15.6250** | **15.6250** |  **367.2 KB** |           **?** |
|                             |          |             |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **10**       | **6,057.48 μs** | **26,300.100 μs** | **1,441.597 μs** |     **?** |       **?** | **17.5781** | **16.6016** | **16.6016** | **715.17 KB** |           **?** |
|                             |          |             |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **25**       | **8,541.97 μs** | **28,419.536 μs** | **1,557.771 μs** |     **?** |       **?** | **19.5313** | **17.5781** | **17.5781** | **937.55 KB** |           **?** |
