```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.64GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | keyCount | Mean         | Error         | StdDev       | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|---------------------------- |--------- |-------------:|--------------:|-------------:|------:|--------:|--------:|--------:|--------:|----------:|------------:|
| **Invalidation_NoMatchingKeys** | **?**        |     **38.29 μs** |      **0.461 μs** |     **0.025 μs** |  **1.00** |    **0.00** |  **1.0376** |       **-** |       **-** |  **17.31 KB** |        **1.00** |
|                             |          |              |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **5**        |  **3,299.04 μs** |  **9,637.837 μs** |   **528.282 μs** |     **?** |       **?** | **17.5781** | **16.6016** | **16.6016** |  **367.2 KB** |           **?** |
|                             |          |              |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **10**       |  **3,247.77 μs** |  **8,347.220 μs** |   **457.539 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** | **375.14 KB** |           **?** |
|                             |          |              |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **25**       | **10,478.64 μs** | **33,237.080 μs** | **1,821.837 μs** |     **?** |       **?** | **17.5781** | **15.6250** | **15.6250** | **909.14 KB** |           **?** |
