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
| **Invalidation_NoMatchingKeys** | **?**        |    **26.59 μs** |      **3.964 μs** |     **0.217 μs** |  **1.00** |    **0.01** |  **1.0376** |  **0.0305** |       **-** |   **17.3 KB** |        **1.00** |
|                             |          |             |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **5**        | **2,849.30 μs** |  **8,818.587 μs** |   **483.377 μs** |     **?** |       **?** | **16.6016** | **15.6250** | **15.6250** |  **367.2 KB** |           **?** |
|                             |          |             |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **10**       | **7,289.58 μs** | **42,364.906 μs** | **2,322.163 μs** |     **?** |       **?** | **17.5781** | **16.6016** | **16.6016** | **715.16 KB** |           **?** |
|                             |          |             |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **25**       | **8,449.07 μs** | **27,524.926 μs** | **1,508.734 μs** |     **?** |       **?** | **19.5313** | **17.5781** | **17.5781** |  **937.3 KB** |           **?** |
