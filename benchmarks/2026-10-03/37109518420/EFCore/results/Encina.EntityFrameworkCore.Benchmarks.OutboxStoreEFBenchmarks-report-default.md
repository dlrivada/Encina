
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                              | Mean        | Error     | StdDev    | Ratio | RatioSD | Allocated  | Alloc Ratio |
------------------------------------ |------------:|----------:|----------:|------:|--------:|-----------:|------------:|
 'AddAsync single message'           |    34.68 μs |  87.14 μs |  4.776 μs |  1.01 |    0.17 |    2.86 KB |        1.00 |
 'GetPendingMessagesAsync batch=10'  |   353.65 μs | 202.43 μs | 11.096 μs | 10.32 |    1.20 |  146.34 KB |       51.18 |
 'GetPendingMessagesAsync batch=100' | 2,576.65 μs | 445.37 μs | 24.412 μs | 75.20 |    8.55 | 1341.01 KB |      468.99 |
 MarkAsProcessedAsync                |   105.60 μs | 165.81 μs |  9.089 μs |  3.08 |    0.42 |   11.91 KB |        4.16 |
