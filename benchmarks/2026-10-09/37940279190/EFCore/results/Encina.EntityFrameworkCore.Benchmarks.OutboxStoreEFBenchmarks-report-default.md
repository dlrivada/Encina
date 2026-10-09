
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                              | Mean        | Error     | StdDev    | Ratio | RatioSD | Allocated  | Alloc Ratio |
------------------------------------ |------------:|----------:|----------:|------:|--------:|-----------:|------------:|
 'AddAsync single message'           |    46.71 μs |  74.93 μs |  4.107 μs |  1.01 |    0.11 |    2.86 KB |        1.00 |
 'GetPendingMessagesAsync batch=10'  |   553.36 μs | 363.32 μs | 19.915 μs | 11.91 |    0.98 |  146.34 KB |       51.18 |
 'GetPendingMessagesAsync batch=100' | 3,543.85 μs | 146.15 μs |  8.011 μs | 76.27 |    5.78 | 1341.01 KB |      468.99 |
 MarkAsProcessedAsync                |   142.93 μs | 113.48 μs |  6.220 μs |  3.08 |    0.26 |   11.91 KB |        4.16 |
