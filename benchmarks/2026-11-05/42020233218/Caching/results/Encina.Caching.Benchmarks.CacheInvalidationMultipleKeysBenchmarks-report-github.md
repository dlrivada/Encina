```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.96GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | keyCount | Mean         | Error         | StdDev       | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|---------------------------- |--------- |-------------:|--------------:|-------------:|------:|--------:|--------:|--------:|--------:|----------:|------------:|
| **Invalidation_NoMatchingKeys** | **?**        |     **39.04 μs** |      **2.201 μs** |     **0.121 μs** |  **1.00** |    **0.00** |  **0.6714** |       **-** |       **-** |  **17.38 KB** |        **1.00** |
|                             |          |              |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **5**        |  **4,272.62 μs** | **12,608.945 μs** |   **691.139 μs** |     **?** |       **?** | **17.5781** | **16.6016** | **16.6016** |  **367.2 KB** |           **?** |
|                             |          |              |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **10**       |  **4,160.94 μs** | **10,646.556 μs** |   **583.574 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** | **375.16 KB** |           **?** |
|                             |          |              |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **25**       | **16,928.77 μs** | **60,093.690 μs** | **3,293.938 μs** |     **?** |       **?** | **19.5313** | **17.5781** | **17.5781** | **938.26 KB** |           **?** |
