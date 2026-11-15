```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | keyCount | Mean        | Error         | StdDev       | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|---------------------------- |--------- |------------:|--------------:|-------------:|------:|--------:|--------:|--------:|--------:|----------:|------------:|
| **Invalidation_NoMatchingKeys** | **?**        |    **26.30 μs** |      **2.282 μs** |     **0.125 μs** |  **1.00** |    **0.01** |  **1.0376** |  **0.0305** |       **-** |  **17.26 KB** |        **1.00** |
|                             |          |             |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **5**        | **2,764.59 μs** |  **8,163.409 μs** |   **447.464 μs** |     **?** |       **?** | **16.6016** | **15.6250** | **15.6250** | **367.18 KB** |           **?** |
|                             |          |             |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **10**       | **5,840.65 μs** | **21,580.684 μs** | **1,182.910 μs** |     **?** |       **?** | **17.5781** | **16.6016** | **16.6016** | **715.16 KB** |           **?** |
|                             |          |             |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **25**       | **9,186.67 μs** | **38,809.606 μs** | **2,127.285 μs** |     **?** |       **?** | **17.5781** | **15.6250** | **15.6250** | **909.13 KB** |           **?** |
