```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.58GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | concurrencyLevel | Mean        | Error      | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------------------- |----------------- |------------:|-----------:|---------:|------:|--------:|-------:|----------:|------------:|
| **Invalidation_ConcurrentCommands** | **10**               |   **293.13 μs** | **177.727 μs** | **9.742 μs** |     **?** |       **?** | **1.9531** | **173.81 KB** |           **?** |
|                                 |                  |             |            |          |       |         |        |           |             |
| **Invalidation_ConcurrentCommands** | **50**               | **1,453.47 μs** |  **55.817 μs** | **3.060 μs** |     **?** |       **?** | **9.7656** | **868.47 KB** |           **?** |
|                                 |                  |             |            |          |       |         |        |           |             |
| **Invalidation_NoMatchingKeys**     | **?**                |    **27.29 μs** |   **0.430 μs** | **0.024 μs** |  **1.00** |    **0.00** | **0.1831** |  **17.39 KB** |        **1.00** |
