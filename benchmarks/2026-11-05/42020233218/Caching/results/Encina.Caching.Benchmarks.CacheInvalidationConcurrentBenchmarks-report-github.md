```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 2.83GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | concurrencyLevel | Mean        | Error     | StdDev   | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|-------------------------------- |----------------- |------------:|----------:|---------:|------:|--------:|--------:|-------:|----------:|------------:|
| **Invalidation_ConcurrentCommands** | **10**               |   **366.43 μs** | **33.260 μs** | **1.823 μs** |     **?** |       **?** | **10.2539** |      **-** | **173.81 KB** |           **?** |
|                                 |                  |             |           |          |       |         |         |        |           |             |
| **Invalidation_ConcurrentCommands** | **50**               | **1,865.41 μs** | **27.506 μs** | **1.508 μs** |     **?** |       **?** | **52.7344** | **1.9531** |  **868.4 KB** |           **?** |
|                                 |                  |             |           |          |       |         |         |        |           |             |
| **Invalidation_NoMatchingKeys**     | **?**                |    **35.37 μs** |  **0.248 μs** | **0.014 μs** |  **1.00** |    **0.00** |  **1.0376** |      **-** |  **17.12 KB** |        **1.00** |
