```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.21GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | keyCount | Mean        | Error         | StdDev       | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|---------------------------- |--------- |------------:|--------------:|-------------:|------:|--------:|--------:|--------:|--------:|----------:|------------:|
| **Invalidation_NoMatchingKeys** | **?**        |    **37.44 μs** |      **0.328 μs** |     **0.018 μs** |  **1.00** |    **0.00** |  **1.0376** |       **-** |       **-** |  **17.28 KB** |        **1.00** |
|                             |          |             |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **5**        | **3,193.49 μs** |  **8,305.548 μs** |   **455.255 μs** |     **?** |       **?** | **17.5781** | **16.6016** | **16.6016** | **367.17 KB** |           **?** |
|                             |          |             |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **10**       | **3,238.96 μs** |  **7,488.125 μs** |   **410.449 μs** |     **?** |       **?** | **17.5781** | **15.6250** | **15.6250** | **387.69 KB** |           **?** |
|                             |          |             |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **25**       | **8,417.98 μs** | **26,170.238 μs** | **1,434.479 μs** |     **?** |       **?** | **17.5781** | **15.6250** | **15.6250** | **938.77 KB** |           **?** |
