```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6973P-C 4.14GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                          | concurrencyLevel | Mean        | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------------------- |----------------- |------------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| **Invalidation_ConcurrentCommands** | **10**               |   **231.32 μs** |  **1.002 μs** |  **1.468 μs** |     **?** |       **?** | **1.9531** | **173.89 KB** |           **?** |
|                                 |                  |             |           |           |       |         |        |           |             |
| **Invalidation_ConcurrentCommands** | **50**               | **1,187.15 μs** | **44.576 μs** | **66.719 μs** |     **?** |       **?** | **9.7656** |  **868.5 KB** |           **?** |
|                                 |                  |             |           |           |       |         |        |           |             |
| **Invalidation_NoMatchingKeys**     | **?**                |    **23.05 μs** |  **0.804 μs** |  **1.152 μs** |  **1.00** |    **0.07** | **0.1831** |   **17.2 KB** |        **1.00** |
