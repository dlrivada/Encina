```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.58GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | concurrencyLevel | Mean        | Error     | StdDev   | Ratio | RatioSD | Gen0    | Allocated | Alloc Ratio |
|-------------------------------- |----------------- |------------:|----------:|---------:|------:|--------:|--------:|----------:|------------:|
| **Invalidation_ConcurrentCommands** | **10**               |   **428.22 μs** | **17.480 μs** | **0.958 μs** |     **?** |       **?** | **10.2539** | **173.82 KB** |           **?** |
|                                 |                  |             |           |          |       |         |         |           |             |
| **Invalidation_ConcurrentCommands** | **50**               | **2,121.89 μs** | **62.299 μs** | **3.415 μs** |     **?** |       **?** | **50.7813** | **868.41 KB** |           **?** |
|                                 |                  |             |           |          |       |         |         |           |             |
| **Invalidation_NoMatchingKeys**     | **?**                |    **40.81 μs** |  **1.147 μs** | **0.063 μs** |  **1.00** |    **0.00** |  **1.0376** |   **17.1 KB** |        **1.00** |
