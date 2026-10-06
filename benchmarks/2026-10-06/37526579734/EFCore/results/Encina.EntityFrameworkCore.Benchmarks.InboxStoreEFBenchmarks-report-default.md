
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                  | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
------------------------ |----------:|----------:|----------:|------:|--------:|----------:|------------:|
 MarkAsProcessedAsync    | 153.73 μs | 289.21 μs | 15.853 μs |  1.26 |    0.12 |  11.97 KB |        1.20 |
 GetExpiredMessagesAsync | 852.82 μs | 192.50 μs | 10.551 μs |  7.01 |    0.23 | 287.61 KB |       28.72 |
 AddAsync                |  55.12 μs | 220.92 μs | 12.109 μs |  0.45 |    0.09 |   2.85 KB |        0.28 |
 GetMessageAsync         | 121.76 μs |  80.32 μs |  4.402 μs |  1.00 |    0.04 |  10.02 KB |        1.00 |
