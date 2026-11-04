
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.21GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                  | Mean       | Error     | StdDev   | Ratio | RatioSD | Allocated  | Alloc Ratio |
------------------------ |-----------:|----------:|---------:|------:|--------:|-----------:|------------:|
 MarkAsProcessedAsync    |   318.6 μs | 429.83 μs | 23.56 μs |  1.07 |    0.12 |   34.19 KB |        1.07 |
 GetExpiredMessagesAsync | 3,333.8 μs | 949.05 μs | 52.02 μs | 11.24 |    0.98 | 2196.91 KB |       68.77 |
 AddAsync                |   179.2 μs |  52.57 μs |  2.88 μs |  0.60 |    0.05 |   23.56 KB |        0.74 |
 GetMessageAsync         |   298.6 μs | 550.21 μs | 30.16 μs |  1.01 |    0.12 |   31.95 KB |        1.00 |
