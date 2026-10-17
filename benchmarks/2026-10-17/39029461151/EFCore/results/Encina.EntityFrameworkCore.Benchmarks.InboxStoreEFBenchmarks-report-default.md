
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                  | Mean       | Error      | StdDev   | Ratio | RatioSD | Allocated  | Alloc Ratio |
------------------------ |-----------:|-----------:|---------:|------:|--------:|-----------:|------------:|
 MarkAsProcessedAsync    |   220.1 μs |   252.4 μs | 13.83 μs |  1.08 |    0.07 |   34.19 KB |        1.07 |
 GetExpiredMessagesAsync | 4,445.6 μs | 1,746.3 μs | 95.72 μs | 21.84 |    0.70 | 2189.73 KB |       68.55 |
 AddAsync                |   127.9 μs |   207.6 μs | 11.38 μs |  0.63 |    0.05 |   23.56 KB |        0.74 |
 GetMessageAsync         |   203.7 μs |   112.0 μs |  6.14 μs |  1.00 |    0.04 |   31.95 KB |        1.00 |
