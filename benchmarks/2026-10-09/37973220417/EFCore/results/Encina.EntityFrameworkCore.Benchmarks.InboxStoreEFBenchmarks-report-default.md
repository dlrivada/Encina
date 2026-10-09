
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.21GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                  | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
------------------------ |----------:|----------:|----------:|------:|--------:|----------:|------------:|
 MarkAsProcessedAsync    | 123.82 μs | 426.87 μs | 23.398 μs |  1.24 |    0.24 |  11.97 KB |        1.20 |
 GetExpiredMessagesAsync | 679.75 μs | 445.11 μs | 24.398 μs |  6.82 |    0.74 | 287.61 KB |       28.72 |
 AddAsync                |  35.31 μs |  85.00 μs |  4.659 μs |  0.35 |    0.05 |   2.85 KB |        0.28 |
 GetMessageAsync         | 100.69 μs | 234.37 μs | 12.847 μs |  1.01 |    0.15 |  10.02 KB |        1.00 |
