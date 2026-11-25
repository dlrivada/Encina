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
| **Invalidation_NoMatchingKeys** | **?**        |    **26.85 μs** |      **2.580 μs** |     **0.141 μs** |  **1.00** |    **0.01** |  **1.0376** |  **0.0305** |       **-** |  **17.19 KB** |        **1.00** |
|                             |          |             |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **5**        | **3,001.37 μs** | **13,699.414 μs** |   **750.911 μs** |     **?** |       **?** | **16.6016** | **15.6250** | **15.6250** |  **367.2 KB** |           **?** |
|                             |          |             |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **10**       | **9,828.85 μs** | **19,565.428 μs** | **1,072.447 μs** |     **?** |       **?** | **17.5781** | **16.6016** | **16.6016** | **715.14 KB** |           **?** |
|                             |          |             |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **25**       | **9,722.58 μs** | **41,709.882 μs** | **2,286.259 μs** |     **?** |       **?** | **17.5781** | **15.6250** | **15.6250** | **909.09 KB** |           **?** |
