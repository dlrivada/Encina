
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.51GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                              | Mean        | Error     | StdDev    | Ratio | RatioSD | Allocated  | Alloc Ratio |
------------------------------------ |------------:|----------:|----------:|------:|--------:|-----------:|------------:|
 'AddAsync single message'           |    28.84 μs |  74.40 μs |  4.078 μs |  1.01 |    0.17 |    2.86 KB |        1.00 |
 'GetPendingMessagesAsync batch=10'  |   448.24 μs | 626.75 μs | 34.354 μs | 15.75 |    2.17 |  146.45 KB |       51.22 |
 'GetPendingMessagesAsync batch=100' | 2,641.14 μs | 929.17 μs | 50.931 μs | 92.80 |   11.27 | 1341.01 KB |      468.99 |
 MarkAsProcessedAsync                |    94.81 μs | 218.39 μs | 11.971 μs |  3.33 |    0.54 |   11.91 KB |        4.16 |
