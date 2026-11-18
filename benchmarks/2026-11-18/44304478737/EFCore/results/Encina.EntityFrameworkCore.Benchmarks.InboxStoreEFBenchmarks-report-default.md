
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                  | Mean       | Error    | StdDev   | Ratio | RatioSD | Allocated  | Alloc Ratio |
------------------------ |-----------:|---------:|---------:|------:|--------:|-----------:|------------:|
 MarkAsProcessedAsync    |   210.9 μs | 168.0 μs |  9.21 μs |  1.05 |    0.05 |   34.19 KB |        1.07 |
 GetExpiredMessagesAsync | 4,682.7 μs | 712.8 μs | 39.07 μs | 23.37 |    0.75 | 2197.47 KB |       68.79 |
 AddAsync                |   132.4 μs | 136.0 μs |  7.45 μs |  0.66 |    0.04 |   23.56 KB |        0.74 |
 GetMessageAsync         |   200.5 μs | 130.1 μs |  7.13 μs |  1.00 |    0.04 |   31.95 KB |        1.00 |
