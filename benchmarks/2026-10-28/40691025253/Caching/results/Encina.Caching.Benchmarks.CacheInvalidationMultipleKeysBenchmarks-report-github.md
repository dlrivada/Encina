```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                      | keyCount | Mean        | Error        | StdDev       | Ratio | RatioSD | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|---------------------------- |--------- |------------:|-------------:|-------------:|------:|--------:|--------:|--------:|--------:|----------:|------------:|
| **Invalidation_NoMatchingKeys** | **?**        |    **30.84 μs** |     **15.29 μs** |     **0.838 μs** |  **1.00** |    **0.03** |  **0.1831** |       **-** |       **-** |   **17.3 KB** |        **1.00** |
|                             |          |             |              |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **5**        | **3,603.99 μs** |  **8,473.37 μs** |   **464.454 μs** |     **?** |       **?** | **17.5781** | **17.5781** | **17.5781** | **373.27 KB** |           **?** |
|                             |          |             |              |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **10**       | **3,613.13 μs** |  **7,798.55 μs** |   **427.465 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** | **375.16 KB** |           **?** |
|                             |          |             |              |              |       |         |         |         |         |           |             |
| **Invalidation_MultipleKeys**   | **25**       | **8,775.29 μs** | **23,178.31 μs** | **1,270.482 μs** |     **?** |       **?** | **15.6250** | **15.6250** | **15.6250** |  **938.6 KB** |           **?** |
