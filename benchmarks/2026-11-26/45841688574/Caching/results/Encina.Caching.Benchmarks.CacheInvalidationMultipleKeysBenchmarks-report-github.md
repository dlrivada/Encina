```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6973P-C 4.18GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | keyCount | Mean        | Error        | StdDev     | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|---------------------------- |--------- |------------:|-------------:|-----------:|------:|--------:|--------:|--------:|--------:|----------:|------------:|
| **Invalidation_NoMatchingKeys** | **?**        |    **24.02 μs** |     **14.47 μs** |   **0.793 μs** |  **1.00** |    **0.04** |  **0.1831** |       **-** |       **-** |  **17.16 KB** |        **1.00** |
|                             |          |             |              |            |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **5**        | **2,348.32 μs** |  **4,376.61 μs** | **239.897 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** |  **367.2 KB** |           **?** |
|                             |          |             |              |            |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **10**       | **4,351.30 μs** | **12,745.47 μs** | **698.622 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** | **715.14 KB** |           **?** |
|                             |          |             |              |            |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **25**       | **5,660.80 μs** |  **9,705.54 μs** | **531.993 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** | **938.49 KB** |           **?** |
