
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.53GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                  | Mean       | Error     | StdDev   | Ratio | RatioSD | Allocated  | Alloc Ratio |
------------------------ |-----------:|----------:|---------:|------:|--------:|-----------:|------------:|
 MarkAsProcessedAsync    |   185.9 μs | 216.54 μs | 11.87 μs |  1.13 |    0.10 |   34.19 KB |        1.07 |
 GetExpiredMessagesAsync | 2,314.3 μs | 502.46 μs | 27.54 μs | 14.06 |    1.03 | 2197.78 KB |       68.80 |
 AddAsync                |   101.8 μs |  83.91 μs |  4.60 μs |  0.62 |    0.05 |   23.56 KB |        0.74 |
 GetMessageAsync         |   165.4 μs | 242.31 μs | 13.28 μs |  1.00 |    0.10 |   31.95 KB |        1.00 |
