```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.00GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | keyCount | Mean         | Error         | StdDev       | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|---------------------------- |--------- |-------------:|--------------:|-------------:|------:|--------:|--------:|--------:|--------:|----------:|------------:|
| **Invalidation_NoMatchingKeys** | **?**        |     **34.20 μs** |      **1.944 μs** |     **0.107 μs** |  **1.00** |    **0.00** |  **0.1831** |       **-** |       **-** |  **17.14 KB** |        **1.00** |
|                             |          |              |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **5**        |  **3,949.63 μs** | **10,156.427 μs** |   **556.708 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** | **367.19 KB** |           **?** |
|                             |          |              |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **10**       |  **3,986.08 μs** | **10,189.271 μs** |   **558.508 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** | **375.17 KB** |           **?** |
|                             |          |              |               |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **25**       | **11,213.22 μs** | **38,896.117 μs** | **2,132.027 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** | **938.65 KB** |           **?** |
