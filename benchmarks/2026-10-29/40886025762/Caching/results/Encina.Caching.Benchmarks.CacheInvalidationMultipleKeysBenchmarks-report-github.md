```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.66GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | keyCount | Mean        | Error         | StdDev     | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|---------------------------- |--------- |------------:|--------------:|-----------:|------:|--------:|--------:|--------:|--------:|----------:|------------:|
| **Invalidation_NoMatchingKeys** | **?**        |    **38.16 μs** |      **1.448 μs** |   **0.079 μs** |  **1.00** |    **0.00** |  **1.0376** |       **-** |       **-** |   **17.5 KB** |        **1.00** |
|                             |          |             |               |            |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **5**        | **3,230.09 μs** |  **8,318.473 μs** | **455.964 μs** |     **?** |       **?** | **17.5781** | **16.6016** | **16.6016** |  **367.2 KB** |           **?** |
|                             |          |             |               |            |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **10**       | **3,229.65 μs** |  **7,990.200 μs** | **437.970 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** | **375.15 KB** |           **?** |
|                             |          |             |               |            |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **25**       | **8,950.03 μs** | **18,086.260 μs** | **991.369 μs** |     **?** |       **?** | **17.5781** | **15.6250** | **15.6250** | **909.12 KB** |           **?** |
