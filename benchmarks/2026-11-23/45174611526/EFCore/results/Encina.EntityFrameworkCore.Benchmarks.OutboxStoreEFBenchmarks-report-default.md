
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                              | Mean        | Error     | StdDev    | Ratio | RatioSD | Allocated  | Alloc Ratio |
------------------------------------ |------------:|----------:|----------:|------:|--------:|-----------:|------------:|
 'AddAsync single message'           |    75.97 μs |  64.62 μs |  3.542 μs |  1.00 |    0.06 |    2.86 KB |        1.00 |
 'GetPendingMessagesAsync batch=10'  |   518.82 μs |  23.35 μs |  1.280 μs |  6.84 |    0.27 |  146.34 KB |       51.18 |
 'GetPendingMessagesAsync batch=100' | 3,263.42 μs | 420.35 μs | 23.041 μs | 43.02 |    1.71 | 1341.16 KB |      469.04 |
 MarkAsProcessedAsync                |   209.27 μs | 385.61 μs | 21.136 μs |  2.76 |    0.26 |   11.91 KB |        4.16 |
