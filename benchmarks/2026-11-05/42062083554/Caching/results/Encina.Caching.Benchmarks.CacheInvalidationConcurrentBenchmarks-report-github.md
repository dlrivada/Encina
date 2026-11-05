```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.72GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | concurrencyLevel | Mean        | Error     | StdDev   | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|-------------------------------- |----------------- |------------:|----------:|---------:|------:|--------:|--------:|----------:|------------:|
| **Invalidation_ConcurrentCommands** | **10**               |   **406.73 μs** | **16.299 μs** | **0.893 μs** |     **?** |       **?** |  **6.8359** |  **173.8 KB** |           **?** |
|                                 |                  |             |           |          |       |         |         |           |             |
| **Invalidation_ConcurrentCommands** | **50**               | **2,024.70 μs** | **72.574 μs** | **3.978 μs** |     **?** |       **?** | **35.1563** | **868.36 KB** |           **?** |
|                                 |                  |             |           |          |       |         |         |           |             |
| **Invalidation_NoMatchingKeys**     | **?**                |    **39.00 μs** |  **0.174 μs** | **0.010 μs** |  **1.00** |    **0.00** |  **0.6714** |  **17.15 KB** |        **1.00** |
