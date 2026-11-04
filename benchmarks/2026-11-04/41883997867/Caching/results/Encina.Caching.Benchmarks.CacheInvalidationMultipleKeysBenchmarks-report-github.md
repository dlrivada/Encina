```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | keyCount | Mean         | Error         | StdDev       | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|---------------------------- |--------- |-------------:|--------------:|-------------:|------:|--------:|--------:|--------:|--------:|----------:|------------:|
| **Invalidation_NoMatchingKeys** | **?**        |     **27.12 μs** |      **5.680 μs** |     **0.311 μs** |  **1.00** |    **0.01** |  **1.0376** |  **0.0305** |       **-** |     **17 KB** |        **1.00** |
|                             |          |              |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **5**        |  **2,883.95 μs** |  **9,032.377 μs** |   **495.095 μs** |     **?** |       **?** | **16.6016** | **15.6250** | **15.6250** | **367.16 KB** |           **?** |
|                             |          |              |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **10**       |  **6,892.71 μs** | **24,408.804 μs** | **1,337.929 μs** |     **?** |       **?** | **17.5781** | **16.6016** | **16.6016** | **715.13 KB** |           **?** |
|                             |          |              |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **25**       | **11,527.38 μs** | **83,781.735 μs** | **4,592.360 μs** |     **?** |       **?** | **17.5781** | **15.6250** | **15.6250** | **909.12 KB** |           **?** |
