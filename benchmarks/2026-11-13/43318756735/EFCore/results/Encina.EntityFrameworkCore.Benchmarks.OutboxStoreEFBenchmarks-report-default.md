
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.49GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                              | Mean        | Error     | StdDev    | Ratio | RatioSD | Allocated  | Alloc Ratio |
------------------------------------ |------------:|----------:|----------:|------:|--------:|-----------:|------------:|
 'AddAsync single message'           |    36.07 μs |  58.49 μs |  3.206 μs |  1.01 |    0.11 |    2.86 KB |        1.00 |
 'GetPendingMessagesAsync batch=10'  |   398.25 μs | 168.39 μs |  9.230 μs | 11.10 |    0.93 |  146.34 KB |       51.18 |
 'GetPendingMessagesAsync batch=100' | 2,737.02 μs | 615.91 μs | 33.760 μs | 76.31 |    6.22 | 1341.01 KB |      468.99 |
 MarkAsProcessedAsync                |   101.96 μs | 119.48 μs |  6.549 μs |  2.84 |    0.28 |   11.91 KB |        4.16 |
