
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                  | Mean       | Error    | StdDev   | Ratio | RatioSD | Allocated  | Alloc Ratio |
------------------------ |-----------:|---------:|---------:|------:|--------:|-----------:|------------:|
 MarkAsProcessedAsync    |   244.3 μs | 193.0 μs | 10.58 μs |  1.19 |    0.07 |   34.19 KB |        1.07 |
 GetExpiredMessagesAsync | 4,459.0 μs | 445.4 μs | 24.41 μs | 21.76 |    0.88 | 2189.73 KB |       68.55 |
 AddAsync                |   152.3 μs | 518.9 μs | 28.44 μs |  0.74 |    0.12 |   23.56 KB |        0.74 |
 GetMessageAsync         |   205.2 μs | 170.3 μs |  9.33 μs |  1.00 |    0.06 |   31.95 KB |        1.00 |
