
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                  | Mean       | Error    | StdDev   | Ratio | RatioSD | Allocated  | Alloc Ratio |
------------------------ |-----------:|---------:|---------:|------:|--------:|-----------:|------------:|
 MarkAsProcessedAsync    |   202.1 μs | 334.3 μs | 18.33 μs |  1.09 |    0.09 |   34.19 KB |        1.07 |
 GetExpiredMessagesAsync | 2,524.3 μs | 233.1 μs | 12.78 μs | 13.58 |    0.43 | 2197.47 KB |       68.79 |
 AddAsync                |   106.7 μs | 151.4 μs |  8.30 μs |  0.57 |    0.04 |   23.56 KB |        0.74 |
 GetMessageAsync         |   186.1 μs | 121.2 μs |  6.65 μs |  1.00 |    0.04 |   31.95 KB |        1.00 |
