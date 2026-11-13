```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | concurrencyLevel | Mean        | Error      | StdDev   | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|-------------------------------- |----------------- |------------:|-----------:|---------:|------:|--------:|--------:|-------:|----------:|------------:|
| **Invalidation_ConcurrentCommands** | **10**               |   **283.46 μs** |  **60.349 μs** | **3.308 μs** |     **?** |       **?** | **10.2539** |      **-** | **173.83 KB** |           **?** |
|                                 |                  |             |            |          |       |         |         |        |           |             |
| **Invalidation_ConcurrentCommands** | **50**               | **1,425.30 μs** | **173.748 μs** | **9.524 μs** |     **?** |       **?** | **52.7344** | **1.9531** | **868.44 KB** |           **?** |
|                                 |                  |             |            |          |       |         |         |        |           |             |
| **Invalidation_NoMatchingKeys**     | **?**                |    **27.11 μs** |   **1.307 μs** | **0.072 μs** |  **1.00** |    **0.00** |  **1.0376** | **0.0305** |  **17.13 KB** |        **1.00** |
