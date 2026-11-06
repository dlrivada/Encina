```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | keyCount | Mean         | Error         | StdDev       | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|---------------------------- |--------- |-------------:|--------------:|-------------:|------:|--------:|--------:|--------:|--------:|----------:|------------:|
| **Invalidation_NoMatchingKeys** | **?**        |     **39.14 μs** |      **0.742 μs** |     **0.041 μs** |  **1.00** |    **0.00** |  **1.0376** |       **-** |       **-** |  **17.33 KB** |        **1.00** |
|                             |          |              |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **5**        |  **3,373.96 μs** |  **9,406.750 μs** |   **515.616 μs** |     **?** |       **?** | **17.5781** | **16.6016** | **16.6016** |  **367.2 KB** |           **?** |
|                             |          |              |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **10**       |  **3,392.19 μs** |  **8,161.736 μs** |   **447.372 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** | **375.17 KB** |           **?** |
|                             |          |              |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **25**       | **12,081.77 μs** | **53,825.518 μs** | **2,950.358 μs** |     **?** |       **?** | **17.5781** | **15.6250** | **15.6250** | **938.82 KB** |           **?** |
