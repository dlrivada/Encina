```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | keyCount | Mean         | Error         | StdDev       | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|---------------------------- |--------- |-------------:|--------------:|-------------:|------:|--------:|--------:|--------:|--------:|----------:|------------:|
| **Invalidation_NoMatchingKeys** | **?**        |     **38.34 μs** |      **1.888 μs** |     **0.103 μs** |  **1.00** |    **0.00** |  **0.6714** |       **-** |       **-** |  **17.43 KB** |        **1.00** |
|                             |          |              |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **5**        |  **3,874.04 μs** | **11,974.693 μs** |   **656.373 μs** |     **?** |       **?** | **17.5781** | **16.6016** | **16.6016** | **373.49 KB** |           **?** |
|                             |          |              |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **10**       |  **3,956.29 μs** | **14,241.816 μs** |   **780.642 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** | **375.12 KB** |           **?** |
|                             |          |              |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **25**       | **15,580.53 μs** | **63,633.361 μs** | **3,487.959 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** | **909.13 KB** |           **?** |
