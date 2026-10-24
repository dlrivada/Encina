```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | concurrencyLevel | Mean        | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------------------- |----------------- |------------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| **Invalidation_ConcurrentCommands** | **10**               |   **278.62 μs** |  **60.963 μs** |  **3.342 μs** |     **?** |       **?** | **1.9531** | **173.81 KB** |           **?** |
|                                 |                  |             |            |           |       |         |        |           |             |
| **Invalidation_ConcurrentCommands** | **50**               | **1,395.97 μs** | **342.388 μs** | **18.767 μs** |     **?** |       **?** | **9.7656** | **868.39 KB** |           **?** |
|                                 |                  |             |            |           |       |         |        |           |             |
| **Invalidation_NoMatchingKeys**     | **?**                |    **25.46 μs** |   **2.162 μs** |  **0.119 μs** |  **1.00** |    **0.01** | **0.1831** |  **17.44 KB** |        **1.00** |
