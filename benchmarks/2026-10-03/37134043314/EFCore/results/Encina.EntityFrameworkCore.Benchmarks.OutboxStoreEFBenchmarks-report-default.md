
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                              | Mean        | Error     | StdDev    | Ratio | RatioSD | Allocated  | Alloc Ratio |
------------------------------------ |------------:|----------:|----------:|------:|--------:|-----------:|------------:|
 'AddAsync single message'           |    57.34 μs | 108.19 μs |  5.930 μs |  1.01 |    0.13 |    2.86 KB |        1.00 |
 'GetPendingMessagesAsync batch=10'  |   488.84 μs | 163.56 μs |  8.965 μs |  8.59 |    0.83 |  146.34 KB |       51.18 |
 'GetPendingMessagesAsync batch=100' | 3,605.00 μs | 638.98 μs | 35.025 μs | 63.35 |    6.03 | 1341.01 KB |      468.99 |
 MarkAsProcessedAsync                |   146.06 μs |  98.02 μs |  5.373 μs |  2.57 |    0.26 |   11.91 KB |        4.16 |
