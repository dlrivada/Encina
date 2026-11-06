
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                              | Mean        | Error     | StdDev    | Ratio | RatioSD | Allocated  | Alloc Ratio |
------------------------------------ |------------:|----------:|----------:|------:|--------:|-----------:|------------:|
 'AddAsync single message'           |    38.72 μs | 179.68 μs |  9.849 μs |  1.05 |    0.34 |    2.86 KB |        1.00 |
 'GetPendingMessagesAsync batch=10'  |   398.16 μs | 305.04 μs | 16.720 μs | 10.77 |    2.54 |  146.48 KB |       51.23 |
 'GetPendingMessagesAsync batch=100' | 2,668.74 μs | 231.35 μs | 12.681 μs | 72.18 |   16.81 | 1341.01 KB |      468.99 |
 MarkAsProcessedAsync                |   103.96 μs | 188.35 μs | 10.324 μs |  2.81 |    0.70 |   11.91 KB |        4.16 |
