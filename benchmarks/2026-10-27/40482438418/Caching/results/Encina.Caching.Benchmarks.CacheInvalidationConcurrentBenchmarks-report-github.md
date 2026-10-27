```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | concurrencyLevel | Mean        | Error     | StdDev   | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|-------------------------------- |----------------- |------------:|----------:|---------:|------:|--------:|--------:|-------:|----------:|------------:|
| **Invalidation_ConcurrentCommands** | **10**               |   **285.62 μs** | **60.851 μs** | **3.335 μs** |     **?** |       **?** | **10.2539** |      **-** | **173.81 KB** |           **?** |
|                                 |                  |             |           |          |       |         |         |        |           |             |
| **Invalidation_ConcurrentCommands** | **50**               | **1,412.77 μs** | **95.881 μs** | **5.256 μs** |     **?** |       **?** | **52.7344** | **1.9531** | **868.45 KB** |           **?** |
|                                 |                  |             |           |          |       |         |         |        |           |             |
| **Invalidation_NoMatchingKeys**     | **?**                |    **26.51 μs** |  **1.212 μs** | **0.066 μs** |  **1.00** |    **0.00** |  **1.0376** | **0.0305** |  **17.34 KB** |        **1.00** |
