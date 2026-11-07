```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | concurrencyLevel | Mean        | Error     | StdDev   | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|-------------------------------- |----------------- |------------:|----------:|---------:|------:|--------:|--------:|-------:|----------:|------------:|
| **Invalidation_ConcurrentCommands** | **10**               |   **286.36 μs** |  **7.949 μs** | **0.436 μs** |     **?** |       **?** | **10.2539** |      **-** | **173.82 KB** |           **?** |
|                                 |                  |             |           |          |       |         |         |        |           |             |
| **Invalidation_ConcurrentCommands** | **50**               | **1,439.99 μs** | **86.716 μs** | **4.753 μs** |     **?** |       **?** | **52.7344** | **1.9531** | **868.47 KB** |           **?** |
|                                 |                  |             |           |          |       |         |         |        |           |             |
| **Invalidation_NoMatchingKeys**     | **?**                |    **27.16 μs** |  **3.459 μs** | **0.190 μs** |  **1.00** |    **0.01** |  **1.0376** | **0.0305** |  **17.17 KB** |        **1.00** |
