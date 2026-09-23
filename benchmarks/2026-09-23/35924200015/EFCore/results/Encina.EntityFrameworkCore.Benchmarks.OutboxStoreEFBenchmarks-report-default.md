
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                              | Mean        | Error     | StdDev    | Ratio | RatioSD | Allocated  | Alloc Ratio |
------------------------------------ |------------:|----------:|----------:|------:|--------:|-----------:|------------:|
 'AddAsync single message'           |    72.52 μs |  56.10 μs |  3.075 μs |  1.00 |    0.05 |    2.86 KB |        1.00 |
 'GetPendingMessagesAsync batch=10'  |   516.21 μs | 393.60 μs | 21.574 μs |  7.13 |    0.37 |  146.34 KB |       51.18 |
 'GetPendingMessagesAsync batch=100' | 2,942.91 μs | 182.96 μs | 10.029 μs | 40.63 |    1.48 | 1341.01 KB |      468.99 |
 MarkAsProcessedAsync                |   186.12 μs | 126.40 μs |  6.928 μs |  2.57 |    0.12 |   11.91 KB |        4.16 |
