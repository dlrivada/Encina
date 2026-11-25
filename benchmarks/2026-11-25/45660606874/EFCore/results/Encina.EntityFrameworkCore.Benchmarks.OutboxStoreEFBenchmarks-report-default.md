
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.48GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                              | Mean        | Error     | StdDev    | Ratio | RatioSD | Allocated  | Alloc Ratio |
------------------------------------ |------------:|----------:|----------:|------:|--------:|-----------:|------------:|
 'AddAsync single message'           |    37.46 μs |  84.34 μs |  4.623 μs |  1.01 |    0.15 |    2.86 KB |        1.00 |
 'GetPendingMessagesAsync batch=10'  |   400.26 μs | 415.18 μs | 22.758 μs | 10.79 |    1.24 |  146.41 KB |       51.20 |
 'GetPendingMessagesAsync batch=100' | 2,793.77 μs | 462.51 μs | 25.352 μs | 75.32 |    7.86 | 1341.16 KB |      469.04 |
 MarkAsProcessedAsync                |   117.34 μs | 225.57 μs | 12.364 μs |  3.16 |    0.44 |   11.91 KB |        4.16 |
