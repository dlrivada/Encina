
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                  | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
------------------------ |----------:|----------:|----------:|------:|--------:|----------:|------------:|
 MarkAsProcessedAsync    | 127.14 μs | 273.14 μs | 14.972 μs |  1.25 |    0.18 |  11.97 KB |        1.20 |
 GetExpiredMessagesAsync | 656.05 μs | 360.70 μs | 19.771 μs |  6.44 |    0.68 | 287.61 KB |       28.72 |
 AddAsync                |  31.33 μs |  66.39 μs |  3.639 μs |  0.31 |    0.04 |   2.85 KB |        0.28 |
 GetMessageAsync         | 102.72 μs | 210.02 μs | 11.512 μs |  1.01 |    0.14 |  10.02 KB |        1.00 |
