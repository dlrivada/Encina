
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.26GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                  | Mean       | Error    | StdDev   | Ratio | RatioSD | Allocated  | Alloc Ratio |
------------------------ |-----------:|---------:|---------:|------:|--------:|-----------:|------------:|
 MarkAsProcessedAsync    |   202.0 μs | 178.8 μs |  9.80 μs |  0.98 |    0.07 |   34.19 KB |        1.07 |
 GetExpiredMessagesAsync | 2,566.6 μs | 582.1 μs | 31.91 μs | 12.50 |    0.80 | 2197.47 KB |       68.79 |
 AddAsync                |   161.3 μs | 404.5 μs | 22.17 μs |  0.79 |    0.11 |   23.56 KB |        0.74 |
 GetMessageAsync         |   206.0 μs | 263.8 μs | 14.46 μs |  1.00 |    0.09 |   31.95 KB |        1.00 |
