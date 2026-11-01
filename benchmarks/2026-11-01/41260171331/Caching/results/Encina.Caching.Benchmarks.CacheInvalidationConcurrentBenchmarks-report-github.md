```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]    : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                          | concurrencyLevel | Mean        | Error     | StdDev    | Median      | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|-------------------------------- |----------------- |------------:|----------:|----------:|------------:|------:|--------:|--------:|-------:|----------:|------------:|
| **Invalidation_ConcurrentCommands** | **10**               |   **365.39 μs** |  **2.407 μs** |  **3.452 μs** |   **366.39 μs** |     **?** |       **?** | **10.2539** |      **-** | **173.81 KB** |           **?** |
|                                 |                  |             |           |           |             |       |         |         |        |           |             |
| **Invalidation_ConcurrentCommands** | **50**               | **1,850.03 μs** | **29.418 μs** | **43.120 μs** | **1,823.98 μs** |     **?** |       **?** | **52.7344** | **1.9531** | **868.49 KB** |           **?** |
|                                 |                  |             |           |           |             |       |         |         |        |           |             |
| **Invalidation_NoMatchingKeys**     | **?**                |    **34.35 μs** |  **0.134 μs** |  **0.193 μs** |    **34.36 μs** |  **1.00** |    **0.01** |  **1.0376** |      **-** |  **17.28 KB** |        **1.00** |
