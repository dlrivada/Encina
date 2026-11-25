```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6973P-C 3.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | keyCount | Mean        | Error        | StdDev     | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|---------------------------- |--------- |------------:|-------------:|-----------:|------:|--------:|--------:|--------:|--------:|----------:|------------:|
| **Invalidation_NoMatchingKeys** | **?**        |    **27.23 μs** |     **10.91 μs** |   **0.598 μs** |  **1.00** |    **0.03** |  **0.1831** |       **-** |       **-** |  **17.16 KB** |        **1.00** |
|                             |          |             |              |            |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **5**        | **2,530.31 μs** |  **5,813.28 μs** | **318.646 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** | **367.17 KB** |           **?** |
|                             |          |             |              |            |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **10**       | **4,864.97 μs** | **14,551.83 μs** | **797.635 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** | **715.14 KB** |           **?** |
|                             |          |             |              |            |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **25**       | **6,244.68 μs** | **16,297.64 μs** | **893.328 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** |  **938.6 KB** |           **?** |
