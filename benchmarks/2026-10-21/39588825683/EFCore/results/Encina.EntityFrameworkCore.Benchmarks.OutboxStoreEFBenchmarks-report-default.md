
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.19GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                              | Mean        | Error       | StdDev    | Ratio | RatioSD | Allocated  | Alloc Ratio |
------------------------------------ |------------:|------------:|----------:|------:|--------:|-----------:|------------:|
 'AddAsync single message'           |    41.65 μs |   110.57 μs |  6.061 μs |  1.01 |    0.17 |    2.86 KB |        1.00 |
 'GetPendingMessagesAsync batch=10'  |   422.25 μs |   574.16 μs | 31.472 μs | 10.27 |    1.37 |  146.34 KB |       51.18 |
 'GetPendingMessagesAsync batch=100' | 3,039.53 μs | 1,119.70 μs | 61.375 μs | 73.95 |    8.71 | 1341.01 KB |      468.99 |
 MarkAsProcessedAsync                |   136.55 μs |   320.17 μs | 17.550 μs |  3.32 |    0.54 |   11.91 KB |        4.16 |
