
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                              | Mean        | Error     | StdDev    | Ratio | RatioSD | Allocated  | Alloc Ratio |
------------------------------------ |------------:|----------:|----------:|------:|--------:|-----------:|------------:|
 'AddAsync single message'           |    34.22 μs |  89.76 μs |  4.920 μs |  1.01 |    0.18 |    2.86 KB |        1.00 |
 'GetPendingMessagesAsync batch=10'  |   369.62 μs | 250.49 μs | 13.730 μs | 10.95 |    1.38 |  146.34 KB |       51.18 |
 'GetPendingMessagesAsync batch=100' | 2,775.43 μs | 461.91 μs | 25.319 μs | 82.21 |   10.07 | 1341.01 KB |      468.99 |
 MarkAsProcessedAsync                |   113.34 μs | 317.90 μs | 17.425 μs |  3.36 |    0.61 |   11.91 KB |        4.16 |
