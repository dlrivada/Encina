```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | concurrencyLevel | Mean        | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------------------- |----------------- |------------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| **Invalidation_ConcurrentCommands** | **10**               |   **331.83 μs** |   **6.243 μs** |  **0.342 μs** |     **?** |       **?** | **1.9531** |  **173.8 KB** |           **?** |
|                                 |                  |             |            |           |       |         |        |           |             |
| **Invalidation_ConcurrentCommands** | **50**               | **1,663.94 μs** | **257.153 μs** | **14.095 μs** |     **?** |       **?** | **9.7656** | **868.46 KB** |           **?** |
|                                 |                  |             |            |           |       |         |        |           |             |
| **Invalidation_NoMatchingKeys**     | **?**                |    **32.72 μs** |   **1.723 μs** |  **0.094 μs** |  **1.00** |    **0.00** | **0.1831** |  **17.16 KB** |        **1.00** |
