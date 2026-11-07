```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.00GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | keyCount | Mean         | Error         | StdDev       | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|---------------------------- |--------- |-------------:|--------------:|-------------:|------:|--------:|--------:|--------:|--------:|----------:|------------:|
| **Invalidation_NoMatchingKeys** | **?**        |     **33.13 μs** |      **5.349 μs** |     **0.293 μs** |  **1.00** |    **0.01** |  **0.1831** |       **-** |       **-** |  **17.35 KB** |        **1.00** |
|                             |          |              |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **5**        |  **3,805.38 μs** |  **9,709.899 μs** |   **532.232 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** | **367.18 KB** |           **?** |
|                             |          |              |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **10**       |  **3,967.09 μs** |  **9,681.756 μs** |   **530.690 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** | **375.17 KB** |           **?** |
|                             |          |              |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **25**       | **11,406.25 μs** | **51,281.406 μs** | **2,810.907 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** | **938.35 KB** |           **?** |
