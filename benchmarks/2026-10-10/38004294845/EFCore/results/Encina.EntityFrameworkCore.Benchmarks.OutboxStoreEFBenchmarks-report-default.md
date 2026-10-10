
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                              | Mean        | Error     | StdDev   | Ratio | RatioSD | Allocated  | Alloc Ratio |
------------------------------------ |------------:|----------:|---------:|------:|--------:|-----------:|------------:|
 'AddAsync single message'           |    37.52 μs | 223.94 μs | 12.28 μs |  1.07 |    0.41 |    2.86 KB |        1.00 |
 'GetPendingMessagesAsync batch=10'  |   419.76 μs | 562.17 μs | 30.81 μs | 11.94 |    3.10 |  146.34 KB |       51.18 |
 'GetPendingMessagesAsync batch=100' | 2,698.24 μs | 417.43 μs | 22.88 μs | 76.72 |   19.30 | 1341.13 KB |      469.03 |
 MarkAsProcessedAsync                |   132.23 μs | 358.67 μs | 19.66 μs |  3.76 |    1.07 |   11.91 KB |        4.16 |
