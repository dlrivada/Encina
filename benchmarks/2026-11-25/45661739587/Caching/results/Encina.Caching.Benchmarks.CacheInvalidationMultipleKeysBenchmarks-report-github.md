```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | keyCount | Mean        | Error         | StdDev     | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|---------------------------- |--------- |------------:|--------------:|-----------:|------:|--------:|--------:|--------:|--------:|----------:|------------:|
| **Invalidation_NoMatchingKeys** | **?**        |    **23.72 μs** |      **0.396 μs** |   **0.022 μs** |  **1.00** |    **0.00** |  **0.1831** |       **-** |       **-** |  **17.38 KB** |        **1.00** |
|                             |          |             |               |            |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **5**        | **2,378.26 μs** |  **5,183.232 μs** | **284.110 μs** |     **?** |       **?** | **16.6016** | **16.6016** | **16.6016** | **373.48 KB** |           **?** |
|                             |          |             |               |            |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **10**       | **4,487.96 μs** | **12,667.347 μs** | **694.340 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** | **715.15 KB** |           **?** |
|                             |          |             |               |            |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **25**       | **5,549.91 μs** | **13,652.943 μs** | **748.364 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** | **909.14 KB** |           **?** |
