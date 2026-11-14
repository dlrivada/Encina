
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.35GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                  | Mean       | Error    | StdDev   | Ratio | RatioSD | Allocated  | Alloc Ratio |
------------------------ |-----------:|---------:|---------:|------:|--------:|-----------:|------------:|
 MarkAsProcessedAsync    |   192.0 μs | 360.6 μs | 19.77 μs |  1.07 |    0.14 |   34.19 KB |        1.07 |
 GetExpiredMessagesAsync | 2,422.1 μs | 619.9 μs | 33.98 μs | 13.55 |    1.37 | 2197.47 KB |       68.79 |
 AddAsync                |   106.5 μs | 164.5 μs |  9.02 μs |  0.60 |    0.07 |   23.56 KB |        0.74 |
 GetMessageAsync         |   180.4 μs | 387.7 μs | 21.25 μs |  1.01 |    0.14 |   31.95 KB |        1.00 |
