
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.20GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                              | Mean        | Error     | StdDev   | Ratio | RatioSD | Allocated  | Alloc Ratio |
------------------------------------ |------------:|----------:|---------:|------:|--------:|-----------:|------------:|
 'AddAsync single message'           |    93.63 μs | 295.94 μs | 16.22 μs |  1.02 |    0.23 |    2.86 KB |        1.00 |
 'GetPendingMessagesAsync batch=10'  |   574.30 μs | 294.43 μs | 16.14 μs |  6.27 |    1.06 |  146.34 KB |       51.18 |
 'GetPendingMessagesAsync batch=100' | 3,381.67 μs | 380.59 μs | 20.86 μs | 36.94 |    6.16 | 1341.01 KB |      468.99 |
 MarkAsProcessedAsync                |   231.28 μs | 204.55 μs | 11.21 μs |  2.53 |    0.43 |   11.91 KB |        4.16 |
