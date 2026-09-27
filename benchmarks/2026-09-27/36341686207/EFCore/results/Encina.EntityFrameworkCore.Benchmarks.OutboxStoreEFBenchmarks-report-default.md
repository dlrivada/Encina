
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                              | Mean        | Error     | StdDev    | Ratio | RatioSD | Allocated  | Alloc Ratio |
------------------------------------ |------------:|----------:|----------:|------:|--------:|-----------:|------------:|
 'AddAsync single message'           |    46.39 μs |  16.13 μs |  0.884 μs |  1.00 |    0.02 |    2.86 KB |        1.00 |
 'GetPendingMessagesAsync batch=10'  |   517.24 μs | 616.87 μs | 33.813 μs | 11.15 |    0.66 |  146.65 KB |       51.29 |
 'GetPendingMessagesAsync batch=100' | 3,511.76 μs | 297.81 μs | 16.324 μs | 75.72 |    1.27 | 1341.13 KB |      469.03 |
 MarkAsProcessedAsync                |   140.19 μs | 180.89 μs |  9.915 μs |  3.02 |    0.19 |    12.2 KB |        4.27 |
