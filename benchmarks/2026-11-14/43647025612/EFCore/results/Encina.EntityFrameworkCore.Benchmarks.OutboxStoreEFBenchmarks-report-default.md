
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.29GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                              | Mean        | Error     | StdDev    | Ratio | RatioSD | Allocated  | Alloc Ratio |
------------------------------------ |------------:|----------:|----------:|------:|--------:|-----------:|------------:|
 'AddAsync single message'           |    34.33 μs | 143.10 μs |  7.844 μs |  1.04 |    0.32 |    2.86 KB |        1.00 |
 'GetPendingMessagesAsync batch=10'  |   427.34 μs | 626.62 μs | 34.347 μs | 12.97 |    3.09 |  146.34 KB |       51.18 |
 'GetPendingMessagesAsync batch=100' | 2,718.29 μs | 305.85 μs | 16.765 μs | 82.50 |   18.79 | 1341.01 KB |      468.99 |
 MarkAsProcessedAsync                |   108.49 μs | 242.19 μs | 13.275 μs |  3.29 |    0.83 |   11.91 KB |        4.16 |
