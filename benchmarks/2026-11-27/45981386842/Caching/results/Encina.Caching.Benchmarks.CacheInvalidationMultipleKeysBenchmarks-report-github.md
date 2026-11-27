```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.04GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | keyCount | Mean        | Error         | StdDev       | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|---------------------------- |--------- |------------:|--------------:|-------------:|------:|--------:|--------:|--------:|--------:|----------:|------------:|
| **Invalidation_NoMatchingKeys** | **?**        |    **32.74 μs** |      **6.121 μs** |     **0.335 μs** |  **1.00** |    **0.01** |  **0.1831** |       **-** |       **-** |  **17.13 KB** |        **1.00** |
|                             |          |             |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **5**        | **3,005.26 μs** |  **7,297.670 μs** |   **400.010 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** | **367.22 KB** |           **?** |
|                             |          |             |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **10**       | **3,060.03 μs** |  **7,113.949 μs** |   **389.940 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** | **375.15 KB** |           **?** |
|                             |          |             |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **25**       | **7,947.02 μs** | **22,192.287 μs** | **1,216.434 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** | **937.82 KB** |           **?** |
