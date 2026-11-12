```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.16GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | keyCount | Mean         | Error         | StdDev       | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|---------------------------- |--------- |-------------:|--------------:|-------------:|------:|--------:|--------:|--------:|--------:|----------:|------------:|
| **Invalidation_NoMatchingKeys** | **?**        |     **38.60 μs** |      **2.462 μs** |     **0.135 μs** |  **1.00** |    **0.00** |  **1.0376** |       **-** |       **-** |  **16.98 KB** |        **1.00** |
|                             |          |              |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **5**        |  **3,229.57 μs** |  **9,198.428 μs** |   **504.197 μs** |     **?** |       **?** | **17.5781** | **16.6016** | **16.6016** |  **367.2 KB** |           **?** |
|                             |          |              |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **10**       |  **3,271.84 μs** |  **8,496.735 μs** |   **465.735 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** | **375.15 KB** |           **?** |
|                             |          |              |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **25**       | **10,027.08 μs** | **39,591.479 μs** | **2,170.143 μs** |     **?** |       **?** | **19.5313** | **17.5781** | **17.5781** | **938.23 KB** |           **?** |
