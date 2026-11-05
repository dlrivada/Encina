```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 2.86GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | keyCount | Mean         | Error         | StdDev       | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|---------------------------- |--------- |-------------:|--------------:|-------------:|------:|--------:|--------:|--------:|--------:|----------:|------------:|
| **Invalidation_NoMatchingKeys** | **?**        |     **34.24 μs** |      **5.662 μs** |     **0.310 μs** |  **1.00** |    **0.01** |  **1.0376** |       **-** |       **-** |  **17.27 KB** |        **1.00** |
|                             |          |              |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **5**        |  **3,581.79 μs** | **10,680.536 μs** |   **585.436 μs** |     **?** |       **?** | **17.5781** | **16.6016** | **16.6016** | **367.19 KB** |           **?** |
|                             |          |              |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **10**       |  **3,576.00 μs** |  **9,257.738 μs** |   **507.448 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** |  **375.2 KB** |           **?** |
|                             |          |              |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **25**       | **10,794.75 μs** | **37,088.989 μs** | **2,032.973 μs** |     **?** |       **?** | **17.5781** | **15.6250** | **15.6250** |  **909.1 KB** |           **?** |
