```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.28GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | keyCount | Mean        | Error         | StdDev     | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|---------------------------- |--------- |------------:|--------------:|-----------:|------:|--------:|--------:|--------:|--------:|----------:|------------:|
| **Invalidation_NoMatchingKeys** | **?**        |    **18.37 μs** |      **2.592 μs** |   **0.142 μs** |  **1.00** |    **0.01** |  **1.0376** |  **0.0305** |       **-** |  **17.19 KB** |        **1.00** |
|                             |          |             |               |            |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **5**        | **1,886.77 μs** |  **5,173.118 μs** | **283.556 μs** |     **?** |       **?** | **16.6016** | **15.6250** | **15.6250** | **367.23 KB** |           **?** |
|                             |          |             |               |            |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **10**       | **3,985.65 μs** | **12,768.104 μs** | **699.863 μs** |     **?** |       **?** | **16.6016** | **15.6250** | **15.6250** | **715.13 KB** |           **?** |
|                             |          |             |               |            |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **25**       | **5,433.72 μs** | **17,648.549 μs** | **967.377 μs** |     **?** |       **?** | **17.5781** | **15.6250** | **15.6250** | **909.13 KB** |           **?** |
