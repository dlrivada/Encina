
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                  | Mean       | Error       | StdDev    | Ratio | RatioSD | Allocated  | Alloc Ratio |
------------------------ |-----------:|------------:|----------:|------:|--------:|-----------:|------------:|
 MarkAsProcessedAsync    |   200.2 μs |    279.1 μs |  15.30 μs |  1.13 |    0.09 |   34.19 KB |        1.07 |
 GetExpiredMessagesAsync | 2,983.2 μs | 11,243.6 μs | 616.30 μs | 16.90 |    3.09 | 2189.73 KB |       68.55 |
 AddAsync                |   113.4 μs |    110.5 μs |   6.06 μs |  0.64 |    0.04 |   23.56 KB |        0.74 |
 GetMessageAsync         |   176.7 μs |    135.3 μs |   7.41 μs |  1.00 |    0.05 |   31.95 KB |        1.00 |
