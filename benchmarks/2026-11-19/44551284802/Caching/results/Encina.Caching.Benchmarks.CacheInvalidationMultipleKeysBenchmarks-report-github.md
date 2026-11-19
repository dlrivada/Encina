```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.66GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | keyCount | Mean         | Error         | StdDev       | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|---------------------------- |--------- |-------------:|--------------:|-------------:|------:|--------:|--------:|--------:|--------:|----------:|------------:|
| **Invalidation_NoMatchingKeys** | **?**        |     **38.71 μs** |      **2.865 μs** |     **0.157 μs** |  **1.00** |    **0.00** |  **1.0376** |       **-** |       **-** |  **17.09 KB** |        **1.00** |
|                             |          |              |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **5**        |  **3,279.76 μs** |  **9,979.768 μs** |   **547.025 μs** |     **?** |       **?** | **17.5781** | **16.6016** | **16.6016** | **367.18 KB** |           **?** |
|                             |          |              |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **10**       |  **3,281.54 μs** |  **8,730.048 μs** |   **478.523 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** | **375.14 KB** |           **?** |
|                             |          |              |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **25**       | **12,997.62 μs** | **69,616.768 μs** | **3,815.930 μs** |     **?** |       **?** | **17.5781** | **15.6250** | **15.6250** | **909.13 KB** |           **?** |
