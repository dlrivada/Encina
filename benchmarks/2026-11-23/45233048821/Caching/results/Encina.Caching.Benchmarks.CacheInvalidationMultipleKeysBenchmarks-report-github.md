```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6973P-C 3.65GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | keyCount | Mean        | Error         | StdDev     | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|---------------------------- |--------- |------------:|--------------:|-----------:|------:|--------:|--------:|--------:|--------:|----------:|------------:|
| **Invalidation_NoMatchingKeys** | **?**        |    **23.00 μs** |      **2.503 μs** |   **0.137 μs** |  **1.00** |    **0.01** |  **0.1831** |       **-** |       **-** |  **17.41 KB** |        **1.00** |
|                             |          |             |               |            |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **5**        | **2,298.41 μs** |  **5,248.471 μs** | **287.686 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** | **367.19 KB** |           **?** |
|                             |          |             |               |            |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **10**       | **4,586.78 μs** | **11,389.732 μs** | **624.310 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** | **715.16 KB** |           **?** |
|                             |          |             |               |            |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **25**       | **6,021.14 μs** | **14,340.620 μs** | **786.058 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** | **938.15 KB** |           **?** |
