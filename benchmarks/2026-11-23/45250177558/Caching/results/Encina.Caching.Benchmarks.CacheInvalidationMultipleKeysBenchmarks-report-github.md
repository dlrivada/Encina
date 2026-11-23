```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | keyCount | Mean         | Error          | StdDev       | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|---------------------------- |--------- |-------------:|---------------:|-------------:|------:|--------:|--------:|--------:|--------:|----------:|------------:|
| **Invalidation_NoMatchingKeys** | **?**        |     **39.63 μs** |       **4.029 μs** |     **0.221 μs** |  **1.00** |    **0.01** |  **1.0376** |       **-** |       **-** |  **17.07 KB** |        **1.00** |
|                             |          |              |                |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **5**        |  **3,432.52 μs** |  **10,381.422 μs** |   **569.041 μs** |     **?** |       **?** | **17.5781** | **16.6016** | **16.6016** |  **367.2 KB** |           **?** |
|                             |          |              |                |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **10**       |  **3,711.22 μs** |  **10,494.699 μs** |   **575.250 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** | **375.15 KB** |           **?** |
|                             |          |              |                |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **25**       | **15,563.98 μs** | **102,970.341 μs** | **5,644.152 μs** |     **?** |       **?** | **17.5781** | **15.6250** | **15.6250** | **938.78 KB** |           **?** |
