```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.37GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | keyCount | Mean        | Error         | StdDev       | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|---------------------------- |--------- |------------:|--------------:|-------------:|------:|--------:|--------:|--------:|--------:|----------:|------------:|
| **Invalidation_NoMatchingKeys** | **?**        |    **19.54 μs** |      **3.471 μs** |     **0.190 μs** |  **1.00** |    **0.01** |  **1.0376** |  **0.0305** |       **-** |  **17.06 KB** |        **1.00** |
|                             |          |             |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **5**        | **2,158.67 μs** |  **7,327.255 μs** |   **401.632 μs** |     **?** |       **?** | **16.6016** | **15.6250** | **15.6250** | **367.21 KB** |           **?** |
|                             |          |             |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **10**       | **7,949.98 μs** | **54,127.447 μs** | **2,966.908 μs** |     **?** |       **?** | **16.6016** | **15.6250** | **15.6250** | **715.14 KB** |           **?** |
|                             |          |             |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **25**       | **6,664.60 μs** | **42,510.708 μs** | **2,330.155 μs** |     **?** |       **?** | **17.5781** | **15.6250** | **15.6250** | **909.14 KB** |           **?** |
