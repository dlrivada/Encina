
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6973P-C 4.10GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                              | Mean        | Error     | StdDev    | Ratio | RatioSD | Allocated  | Alloc Ratio |
------------------------------------ |------------:|----------:|----------:|------:|--------:|-----------:|------------:|
 'AddAsync single message'           |    71.54 μs |  45.99 μs |  2.521 μs |  1.00 |    0.04 |    2.86 KB |        1.00 |
 'GetPendingMessagesAsync batch=10'  |   512.37 μs |  45.21 μs |  2.478 μs |  7.17 |    0.22 |  146.34 KB |       51.18 |
 'GetPendingMessagesAsync batch=100' | 3,078.34 μs | 308.91 μs | 16.932 μs | 43.07 |    1.31 | 1341.01 KB |      468.99 |
 MarkAsProcessedAsync                |   194.19 μs |  46.63 μs |  2.556 μs |  2.72 |    0.09 |   11.91 KB |        4.16 |
