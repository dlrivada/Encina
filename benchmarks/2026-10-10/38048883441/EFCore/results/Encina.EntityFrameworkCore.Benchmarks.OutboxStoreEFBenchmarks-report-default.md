
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                              | Mean        | Error     | StdDev    | Ratio | RatioSD | Allocated  | Alloc Ratio |
------------------------------------ |------------:|----------:|----------:|------:|--------:|-----------:|------------:|
 'AddAsync single message'           |    50.54 μs |  67.80 μs |  3.716 μs |  1.00 |    0.09 |    2.86 KB |        1.00 |
 'GetPendingMessagesAsync batch=10'  |   502.23 μs | 204.99 μs | 11.236 μs |  9.97 |    0.67 |  146.65 KB |       51.29 |
 'GetPendingMessagesAsync batch=100' | 3,536.05 μs | 440.24 μs | 24.131 μs | 70.22 |    4.55 | 1341.01 KB |      468.99 |
 MarkAsProcessedAsync                |   135.75 μs | 105.24 μs |  5.768 μs |  2.70 |    0.20 |   11.91 KB |        4.16 |
