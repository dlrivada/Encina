
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                              | Mean        | Error     | StdDev    | Ratio | RatioSD | Allocated  | Alloc Ratio |
------------------------------------ |------------:|----------:|----------:|------:|--------:|-----------:|------------:|
 'AddAsync single message'           |    34.47 μs |  93.93 μs |  5.149 μs |  1.02 |    0.19 |    2.86 KB |        1.00 |
 'GetPendingMessagesAsync batch=10'  |   406.22 μs | 511.35 μs | 28.029 μs | 11.96 |    1.73 |  146.45 KB |       51.22 |
 'GetPendingMessagesAsync batch=100' | 2,796.99 μs | 405.87 μs | 22.247 μs | 82.37 |   10.83 | 1341.01 KB |      468.99 |
 MarkAsProcessedAsync                |   113.23 μs | 377.79 μs | 20.708 μs |  3.33 |    0.69 |   11.91 KB |        4.16 |
