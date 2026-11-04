```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.21GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | keyCount | Mean        | Error         | StdDev       | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|---------------------------- |--------- |------------:|--------------:|-------------:|------:|--------:|--------:|--------:|--------:|----------:|------------:|
| **Invalidation_NoMatchingKeys** | **?**        |    **38.83 μs** |      **0.230 μs** |     **0.013 μs** |  **1.00** |    **0.00** |  **1.0376** |       **-** |       **-** |  **17.49 KB** |        **1.00** |
|                             |          |             |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **5**        | **3,170.65 μs** |  **8,765.868 μs** |   **480.487 μs** |     **?** |       **?** | **17.5781** | **16.6016** | **16.6016** | **367.19 KB** |           **?** |
|                             |          |             |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **10**       | **3,187.26 μs** |  **7,802.852 μs** |   **427.701 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** | **375.17 KB** |           **?** |
|                             |          |             |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **25**       | **8,419.13 μs** | **28,070.410 μs** | **1,538.634 μs** |     **?** |       **?** | **17.5781** | **15.6250** | **15.6250** | **909.11 KB** |           **?** |
