
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 3.93GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                  | Mean       | Error       | StdDev   | Ratio | RatioSD | Allocated  | Alloc Ratio |
------------------------ |-----------:|------------:|---------:|------:|--------:|-----------:|------------:|
 MarkAsProcessedAsync    |   288.4 μs |   383.16 μs | 21.00 μs |  1.04 |    0.07 |   34.19 KB |        1.07 |
 GetExpiredMessagesAsync | 3,014.4 μs | 1,272.23 μs | 69.74 μs | 10.90 |    0.25 | 2189.73 KB |       68.55 |
 AddAsync                |   184.4 μs |    89.19 μs |  4.89 μs |  0.67 |    0.02 |   23.56 KB |        0.74 |
 GetMessageAsync         |   276.6 μs |    59.51 μs |  3.26 μs |  1.00 |    0.01 |   31.95 KB |        1.00 |
