```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6973P-C 4.05GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | concurrencyLevel | Mean        | Error      | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------------------- |----------------- |------------:|-----------:|---------:|------:|--------:|-------:|----------:|------------:|
| **Invalidation_ConcurrentCommands** | **10**               |   **249.17 μs** | **129.098 μs** | **7.076 μs** |     **?** |       **?** | **1.9531** | **173.83 KB** |           **?** |
|                                 |                  |             |            |          |       |         |        |           |             |
| **Invalidation_ConcurrentCommands** | **50**               | **1,232.74 μs** |  **48.704 μs** | **2.670 μs** |     **?** |       **?** | **9.7656** | **868.46 KB** |           **?** |
|                                 |                  |             |            |          |       |         |        |           |             |
| **Invalidation_NoMatchingKeys**     | **?**                |    **24.59 μs** |   **6.783 μs** | **0.372 μs** |  **1.00** |    **0.02** | **0.1831** |  **17.06 KB** |        **1.00** |
