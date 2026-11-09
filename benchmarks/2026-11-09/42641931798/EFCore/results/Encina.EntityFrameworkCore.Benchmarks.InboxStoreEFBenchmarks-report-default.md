
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.53GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                  | Mean       | Error    | StdDev   | Ratio | RatioSD | Allocated  | Alloc Ratio |
------------------------ |-----------:|---------:|---------:|------:|--------:|-----------:|------------:|
 MarkAsProcessedAsync    |   182.9 μs | 197.0 μs | 10.80 μs |  1.07 |    0.08 |   34.19 KB |        1.07 |
 GetExpiredMessagesAsync | 2,299.7 μs | 442.8 μs | 24.27 μs | 13.46 |    0.66 | 2197.47 KB |       68.79 |
 AddAsync                |   105.1 μs | 168.8 μs |  9.25 μs |  0.62 |    0.06 |   23.56 KB |        0.74 |
 GetMessageAsync         |   171.2 μs | 168.8 μs |  9.25 μs |  1.00 |    0.07 |   31.95 KB |        1.00 |
