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
| **Invalidation_NoMatchingKeys** | **?**        |    **26.40 μs** |      **2.204 μs** |     **0.121 μs** |  **1.00** |    **0.01** |  **1.0376** |  **0.0305** |       **-** |   **17.3 KB** |        **1.00** |
|                             |          |             |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **5**        | **2,761.23 μs** |  **7,717.293 μs** |   **423.011 μs** |     **?** |       **?** | **16.6016** | **15.6250** | **15.6250** | **367.18 KB** |           **?** |
|                             |          |             |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **10**       | **5,767.30 μs** | **18,713.497 μs** | **1,025.750 μs** |     **?** |       **?** | **17.5781** | **16.6016** | **16.6016** | **715.18 KB** |           **?** |
|                             |          |             |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **25**       | **8,262.17 μs** | **31,912.818 μs** | **1,749.249 μs** |     **?** |       **?** | **19.5313** | **17.5781** | **17.5781** | **937.36 KB** |           **?** |
