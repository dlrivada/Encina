
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 3.76GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]    : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=MediumRun  InvocationCount=1  IterationCount=15  
LaunchCount=2  UnrollFactor=1  WarmupCount=10  

 Method                  | Mean       | Error    | StdDev   | Ratio | RatioSD | Allocated  | Alloc Ratio |
------------------------ |-----------:|---------:|---------:|------:|--------:|-----------:|------------:|
 MarkAsProcessedAsync    |   270.6 μs |  7.14 μs | 10.01 μs |  1.08 |    0.06 |   34.19 KB |        1.07 |
 GetExpiredMessagesAsync | 2,971.2 μs | 25.85 μs | 37.89 μs | 11.81 |    0.48 | 2189.73 KB |       68.55 |
 AddAsync                |   165.8 μs |  2.28 μs |  3.12 μs |  0.66 |    0.03 |   23.56 KB |        0.74 |
 GetMessageAsync         |   251.9 μs |  7.31 μs | 10.25 μs |  1.00 |    0.06 |   31.95 KB |        1.00 |
