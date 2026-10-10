
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                  | Mean       | Error    | StdDev   | Ratio | RatioSD | Allocated  | Alloc Ratio |
------------------------ |-----------:|---------:|---------:|------:|--------:|-----------:|------------:|
 MarkAsProcessedAsync    |   233.4 μs | 654.7 μs | 35.88 μs |  1.29 |    0.19 |   34.19 KB |        1.07 |
 GetExpiredMessagesAsync | 2,426.7 μs | 926.4 μs | 50.78 μs | 13.38 |    0.93 | 2189.73 KB |       68.55 |
 AddAsync                |   125.2 μs | 275.1 μs | 15.08 μs |  0.69 |    0.09 |   23.56 KB |        0.74 |
 GetMessageAsync         |   182.0 μs | 256.8 μs | 14.07 μs |  1.00 |    0.10 |   31.95 KB |        1.00 |
