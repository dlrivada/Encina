
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                              | Mean        | Error     | StdDev    | Ratio | RatioSD | Allocated  | Alloc Ratio |
------------------------------------ |------------:|----------:|----------:|------:|--------:|-----------:|------------:|
 'AddAsync single message'           |    48.85 μs |  49.48 μs |  2.712 μs |  1.00 |    0.07 |    2.86 KB |        1.00 |
 'GetPendingMessagesAsync batch=10'  |   508.04 μs | 343.09 μs | 18.806 μs | 10.42 |    0.59 |  146.45 KB |       51.22 |
 'GetPendingMessagesAsync batch=100' | 3,532.58 μs | 249.87 μs | 13.696 μs | 72.46 |    3.40 | 1341.01 KB |      468.99 |
 MarkAsProcessedAsync                |   129.55 μs |  91.53 μs |  5.017 μs |  2.66 |    0.15 |   11.91 KB |        4.16 |
