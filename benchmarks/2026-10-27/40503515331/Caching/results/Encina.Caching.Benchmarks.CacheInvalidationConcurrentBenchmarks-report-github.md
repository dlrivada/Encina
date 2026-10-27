```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | concurrencyLevel | Mean        | Error    | StdDev   | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|-------------------------------- |----------------- |------------:|---------:|---------:|------:|--------:|--------:|-------:|----------:|------------:|
| **Invalidation_ConcurrentCommands** | **10**               |   **371.92 μs** | **47.83 μs** | **2.621 μs** |     **?** |       **?** | **10.2539** |      **-** | **173.79 KB** |           **?** |
|                                 |                  |             |          |          |       |         |         |        |           |             |
| **Invalidation_ConcurrentCommands** | **50**               | **1,809.73 μs** | **61.06 μs** | **3.347 μs** |     **?** |       **?** | **52.7344** | **1.9531** | **868.52 KB** |           **?** |
|                                 |                  |             |          |          |       |         |         |        |           |             |
| **Invalidation_NoMatchingKeys**     | **?**                |    **35.41 μs** | **23.24 μs** | **1.274 μs** |  **1.00** |    **0.04** |  **1.0376** |      **-** |   **17.1 KB** |        **1.00** |
