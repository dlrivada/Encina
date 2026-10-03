
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                  | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
------------------------ |----------:|----------:|----------:|------:|--------:|----------:|------------:|
 MarkAsProcessedAsync    | 104.98 μs | 185.16 μs | 10.149 μs |  1.03 |    0.13 |  11.97 KB |        1.20 |
 GetExpiredMessagesAsync | 636.47 μs | 291.37 μs | 15.971 μs |  6.27 |    0.63 | 287.61 KB |       28.72 |
 AddAsync                |  31.28 μs |  24.68 μs |  1.353 μs |  0.31 |    0.03 |   2.85 KB |        0.28 |
 GetMessageAsync         | 102.30 μs | 202.62 μs | 11.106 μs |  1.01 |    0.14 |  10.02 KB |        1.00 |
