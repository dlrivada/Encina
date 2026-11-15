```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                          | concurrencyLevel | Mean        | Error    | StdDev    | Median      | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|-------------------------------- |----------------- |------------:|---------:|----------:|------------:|------:|--------:|--------:|-------:|----------:|------------:|
| **Invalidation_ConcurrentCommands** | **10**               |   **285.40 μs** | **1.046 μs** |  **1.500 μs** |   **285.50 μs** |     **?** |       **?** | **10.2539** |      **-** |  **173.8 KB** |           **?** |
|                                 |                  |             |          |           |             |       |         |         |        |           |             |
| **Invalidation_ConcurrentCommands** | **50**               | **1,436.42 μs** | **9.653 μs** | **14.150 μs** | **1,444.37 μs** |     **?** |       **?** | **52.7344** | **1.9531** |  **868.4 KB** |           **?** |
|                                 |                  |             |          |           |             |       |         |         |        |           |             |
| **Invalidation_NoMatchingKeys**     | **?**                |    **26.79 μs** | **0.205 μs** |  **0.300 μs** |    **26.63 μs** |  **1.00** |    **0.02** |  **1.0376** | **0.0305** |  **17.33 KB** |        **1.00** |
