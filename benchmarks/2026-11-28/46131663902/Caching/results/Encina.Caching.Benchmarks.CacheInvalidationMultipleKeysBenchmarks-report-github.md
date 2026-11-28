```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.62GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | keyCount | Mean        | Error         | StdDev       | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|---------------------------- |--------- |------------:|--------------:|-------------:|------:|--------:|--------:|--------:|--------:|----------:|------------:|
| **Invalidation_NoMatchingKeys** | **?**        |    **39.21 μs** |      **3.769 μs** |     **0.207 μs** |  **1.00** |    **0.01** |  **1.0376** |       **-** |       **-** |  **17.31 KB** |        **1.00** |
|                             |          |             |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **5**        | **3,287.55 μs** |  **8,071.701 μs** |   **442.437 μs** |     **?** |       **?** | **17.5781** | **16.6016** | **16.6016** | **367.22 KB** |           **?** |
|                             |          |             |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **10**       | **3,270.43 μs** |  **7,901.335 μs** |   **433.099 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** | **375.14 KB** |           **?** |
|                             |          |             |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **25**       | **8,852.68 μs** | **29,710.026 μs** | **1,628.507 μs** |     **?** |       **?** | **17.5781** | **15.6250** | **15.6250** |  **909.1 KB** |           **?** |
