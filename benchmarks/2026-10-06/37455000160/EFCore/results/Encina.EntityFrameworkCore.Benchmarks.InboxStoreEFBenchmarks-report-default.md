
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.53GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                  | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
------------------------ |----------:|----------:|----------:|------:|--------:|----------:|------------:|
 MarkAsProcessedAsync    | 100.25 μs |  98.83 μs |  5.417 μs |  1.01 |    0.07 |  11.97 KB |        1.20 |
 GetExpiredMessagesAsync | 652.62 μs | 876.96 μs | 48.069 μs |  6.54 |    0.54 | 287.72 KB |       28.73 |
 AddAsync                |  32.07 μs |  60.47 μs |  3.314 μs |  0.32 |    0.03 |   2.85 KB |        0.28 |
 GetMessageAsync         |  99.97 μs | 110.41 μs |  6.052 μs |  1.00 |    0.07 |  10.02 KB |        1.00 |
