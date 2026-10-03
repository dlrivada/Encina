
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                              | Mean        | Error     | StdDev    | Ratio | RatioSD | Allocated  | Alloc Ratio |
------------------------------------ |------------:|----------:|----------:|------:|--------:|-----------:|------------:|
 'AddAsync single message'           |    53.20 μs | 187.83 μs | 10.295 μs |  1.02 |    0.23 |    2.86 KB |        1.00 |
 'GetPendingMessagesAsync batch=10'  |   490.32 μs | 101.45 μs |  5.561 μs |  9.43 |    1.43 |  146.34 KB |       51.18 |
 'GetPendingMessagesAsync batch=100' | 3,490.55 μs | 249.84 μs | 13.695 μs | 67.13 |   10.18 | 1341.01 KB |      468.99 |
 MarkAsProcessedAsync                |   147.96 μs | 320.40 μs | 17.562 μs |  2.85 |    0.52 |   11.91 KB |        4.16 |
