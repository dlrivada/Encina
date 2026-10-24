```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | concurrencyLevel | Mean        | Error      | StdDev   | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|-------------------------------- |----------------- |------------:|-----------:|---------:|------:|--------:|--------:|-------:|----------:|------------:|
| **Invalidation_ConcurrentCommands** | **10**               |   **276.03 μs** |  **21.794 μs** | **1.195 μs** |     **?** |       **?** | **10.2539** |      **-** | **173.83 KB** |           **?** |
|                                 |                  |             |            |          |       |         |         |        |           |             |
| **Invalidation_ConcurrentCommands** | **50**               | **1,401.17 μs** | **103.140 μs** | **5.653 μs** |     **?** |       **?** | **52.7344** | **1.9531** | **868.46 KB** |           **?** |
|                                 |                  |             |            |          |       |         |         |        |           |             |
| **Invalidation_NoMatchingKeys**     | **?**                |    **25.69 μs** |   **2.203 μs** | **0.121 μs** |  **1.00** |    **0.01** |  **1.0376** | **0.0305** |  **17.41 KB** |        **1.00** |
