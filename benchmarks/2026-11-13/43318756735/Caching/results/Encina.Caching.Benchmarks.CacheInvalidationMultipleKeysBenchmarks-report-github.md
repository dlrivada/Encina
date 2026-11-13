```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.59GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | keyCount | Mean        | Error         | StdDev       | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|---------------------------- |--------- |------------:|--------------:|-------------:|------:|--------:|--------:|--------:|--------:|----------:|------------:|
| **Invalidation_NoMatchingKeys** | **?**        |    **37.53 μs** |      **1.046 μs** |     **0.057 μs** |  **1.00** |    **0.00** |  **1.0376** |       **-** |       **-** |  **17.45 KB** |        **1.00** |
|                             |          |             |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **5**        | **3,259.69 μs** |  **9,776.723 μs** |   **535.895 μs** |     **?** |       **?** | **17.5781** | **16.6016** | **16.6016** | **367.18 KB** |           **?** |
|                             |          |             |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **10**       | **3,265.52 μs** |  **8,297.011 μs** |   **454.787 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** | **375.15 KB** |           **?** |
|                             |          |             |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **25**       | **9,609.08 μs** | **46,039.673 μs** | **2,523.590 μs** |     **?** |       **?** | **17.5781** | **15.6250** | **15.6250** | **909.13 KB** |           **?** |
