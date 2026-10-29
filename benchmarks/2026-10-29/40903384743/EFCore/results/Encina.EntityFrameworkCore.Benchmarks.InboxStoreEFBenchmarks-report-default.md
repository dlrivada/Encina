
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.23GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                  | Mean       | Error    | StdDev   | Ratio | RatioSD | Allocated  | Alloc Ratio |
------------------------ |-----------:|---------:|---------:|------:|--------:|-----------:|------------:|
 MarkAsProcessedAsync    |   197.4 μs | 292.8 μs | 16.05 μs |  1.14 |    0.09 |   34.19 KB |        1.07 |
 GetExpiredMessagesAsync | 2,298.5 μs | 362.1 μs | 19.85 μs | 13.30 |    0.59 | 2190.05 KB |       68.56 |
 AddAsync                |   122.0 μs | 249.7 μs | 13.69 μs |  0.71 |    0.08 |   23.56 KB |        0.74 |
 GetMessageAsync         |   173.1 μs | 158.8 μs |  8.70 μs |  1.00 |    0.06 |   31.95 KB |        1.00 |
