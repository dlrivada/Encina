```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.78GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | keyCount | Mean         | Error         | StdDev       | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|---------------------------- |--------- |-------------:|--------------:|-------------:|------:|--------:|--------:|--------:|--------:|----------:|------------:|
| **Invalidation_NoMatchingKeys** | **?**        |     **39.72 μs** |      **1.026 μs** |     **0.056 μs** |  **1.00** |    **0.00** |  **0.6714** |       **-** |       **-** |  **16.79 KB** |        **1.00** |
|                             |          |              |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **5**        |  **3,527.46 μs** |  **9,941.783 μs** |   **544.943 μs** |     **?** |       **?** | **16.6016** | **15.6250** | **15.6250** | **367.21 KB** |           **?** |
|                             |          |              |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **10**       |  **3,518.49 μs** |  **9,523.976 μs** |   **522.041 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** | **387.59 KB** |           **?** |
|                             |          |              |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **25**       | **11,026.89 μs** | **51,655.242 μs** | **2,831.398 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** | **909.13 KB** |           **?** |
