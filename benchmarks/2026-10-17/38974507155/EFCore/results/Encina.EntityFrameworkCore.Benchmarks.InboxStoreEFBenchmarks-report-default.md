
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.49GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                  | Mean       | Error      | StdDev   | Ratio | RatioSD | Allocated  | Alloc Ratio |
------------------------ |-----------:|-----------:|---------:|------:|--------:|-----------:|------------:|
 MarkAsProcessedAsync    |   178.2 μs |   168.2 μs |  9.22 μs |  1.04 |    0.13 |   34.19 KB |        1.07 |
 GetExpiredMessagesAsync | 2,347.0 μs | 1,452.3 μs | 79.61 μs | 13.75 |    1.66 | 2189.73 KB |       68.55 |
 AddAsync                |   126.4 μs |   318.6 μs | 17.46 μs |  0.74 |    0.12 |   23.56 KB |        0.74 |
 GetMessageAsync         |   172.9 μs |   435.1 μs | 23.85 μs |  1.01 |    0.17 |   31.95 KB |        1.00 |
