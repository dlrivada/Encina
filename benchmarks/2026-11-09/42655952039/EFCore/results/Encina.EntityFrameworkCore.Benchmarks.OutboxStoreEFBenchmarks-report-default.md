
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.29GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                              | Mean        | Error     | StdDev    | Ratio | RatioSD | Allocated  | Alloc Ratio |
------------------------------------ |------------:|----------:|----------:|------:|--------:|-----------:|------------:|
 'AddAsync single message'           |    41.08 μs | 136.98 μs |  7.508 μs |  1.02 |    0.22 |    2.86 KB |        1.00 |
 'GetPendingMessagesAsync batch=10'  |   411.85 μs | 634.25 μs | 34.765 μs | 10.24 |    1.72 |  146.45 KB |       51.22 |
 'GetPendingMessagesAsync batch=100' | 2,629.35 μs | 673.51 μs | 36.917 μs | 65.39 |    9.90 | 1341.01 KB |      468.99 |
 MarkAsProcessedAsync                |    98.98 μs | 223.80 μs | 12.267 μs |  2.46 |    0.46 |   11.91 KB |        4.16 |
