
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                  | Mean       | Error     | StdDev   | Ratio | RatioSD | Allocated  | Alloc Ratio |
------------------------ |-----------:|----------:|---------:|------:|--------:|-----------:|------------:|
 MarkAsProcessedAsync    |   299.6 μs | 465.71 μs | 25.53 μs |  1.15 |    0.09 |   34.48 KB |        1.08 |
 GetExpiredMessagesAsync | 3,248.1 μs | 478.03 μs | 26.20 μs | 12.49 |    0.15 | 2197.47 KB |       68.79 |
 AddAsync                |   182.8 μs | 173.39 μs |  9.50 μs |  0.70 |    0.03 |   23.56 KB |        0.74 |
 GetMessageAsync         |   260.0 μs |  52.44 μs |  2.87 μs |  1.00 |    0.01 |   31.95 KB |        1.00 |
