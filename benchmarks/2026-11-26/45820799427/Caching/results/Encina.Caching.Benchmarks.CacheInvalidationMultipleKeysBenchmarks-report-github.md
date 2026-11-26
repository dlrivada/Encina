```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.17GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | keyCount | Mean         | Error         | StdDev       | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|---------------------------- |--------- |-------------:|--------------:|-------------:|------:|--------:|--------:|--------:|--------:|----------:|------------:|
| **Invalidation_NoMatchingKeys** | **?**        |     **39.09 μs** |      **1.790 μs** |     **0.098 μs** |  **1.00** |    **0.00** |  **1.0376** |       **-** |       **-** |  **17.24 KB** |        **1.00** |
|                             |          |              |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **5**        |  **3,313.30 μs** |  **9,859.841 μs** |   **540.451 μs** |     **?** |       **?** | **17.5781** | **16.6016** | **16.6016** | **367.19 KB** |           **?** |
|                             |          |              |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **10**       |  **3,309.88 μs** |  **9,362.598 μs** |   **513.196 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** | **375.14 KB** |           **?** |
|                             |          |              |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **25**       | **11,495.99 μs** | **58,226.771 μs** | **3,191.606 μs** |     **?** |       **?** | **17.5781** | **15.6250** | **15.6250** | **909.14 KB** |           **?** |
