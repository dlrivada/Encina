```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 2.95GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | keyCount | Mean        | Error         | StdDev       | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|---------------------------- |--------- |------------:|--------------:|-------------:|------:|--------:|--------:|--------:|--------:|----------:|------------:|
| **Invalidation_NoMatchingKeys** | **?**        |    **32.75 μs** |      **2.883 μs** |     **0.158 μs** |  **1.00** |    **0.01** |  **0.1831** |       **-** |       **-** |  **17.25 KB** |        **1.00** |
|                             |          |             |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **5**        | **2,987.23 μs** |  **7,245.149 μs** |   **397.131 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** | **367.18 KB** |           **?** |
|                             |          |             |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **10**       | **3,050.80 μs** |  **7,573.365 μs** |   **415.122 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** | **375.19 KB** |           **?** |
|                             |          |             |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **25**       | **7,360.14 μs** | **18,349.963 μs** | **1,005.823 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** | **909.14 KB** |           **?** |
