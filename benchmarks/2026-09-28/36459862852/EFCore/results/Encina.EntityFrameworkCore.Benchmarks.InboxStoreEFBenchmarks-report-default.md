
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                  | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
------------------------ |----------:|----------:|----------:|------:|--------:|----------:|------------:|
 MarkAsProcessedAsync    | 196.41 μs | 355.89 μs | 19.507 μs |  1.18 |    0.10 |  11.97 KB |        1.20 |
 GetExpiredMessagesAsync | 763.23 μs | 231.16 μs | 12.671 μs |  4.60 |    0.10 | 287.61 KB |       28.72 |
 AddAsync                |  67.82 μs |  11.26 μs |  0.617 μs |  0.41 |    0.01 |   2.85 KB |        0.28 |
 GetMessageAsync         | 166.03 μs |  57.61 μs |  3.158 μs |  1.00 |    0.02 |  10.02 KB |        1.00 |
