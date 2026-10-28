```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | concurrencyLevel | Mean        | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------------------- |----------------- |------------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| **Invalidation_ConcurrentCommands** | **10**               |   **289.45 μs** | **15.304 μs** | **0.839 μs** |     **?** |       **?** | **1.9531** | **173.82 KB** |           **?** |
|                                 |                  |             |           |          |       |         |        |           |             |
| **Invalidation_ConcurrentCommands** | **50**               | **1,439.47 μs** | **63.887 μs** | **3.502 μs** |     **?** |       **?** | **9.7656** | **868.42 KB** |           **?** |
|                                 |                  |             |           |          |       |         |        |           |             |
| **Invalidation_NoMatchingKeys**     | **?**                |    **28.53 μs** |  **0.882 μs** | **0.048 μs** |  **1.00** |    **0.00** | **0.1831** |   **16.8 KB** |        **1.00** |
