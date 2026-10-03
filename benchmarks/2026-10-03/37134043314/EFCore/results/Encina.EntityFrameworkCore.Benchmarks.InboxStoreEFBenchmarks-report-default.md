
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                  | Mean      | Error      | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
------------------------ |----------:|-----------:|----------:|------:|--------:|----------:|------------:|
 MarkAsProcessedAsync    | 141.83 μs |  79.941 μs |  4.382 μs |  1.27 |    0.04 |  11.97 KB |        1.20 |
 GetExpiredMessagesAsync | 898.58 μs | 346.752 μs | 19.007 μs |  8.07 |    0.18 | 287.61 KB |       28.72 |
 AddAsync                |  41.81 μs |   4.367 μs |  0.239 μs |  0.38 |    0.01 |   2.85 KB |        0.28 |
 GetMessageAsync         | 111.36 μs |  30.607 μs |  1.678 μs |  1.00 |    0.02 |  10.02 KB |        1.00 |
