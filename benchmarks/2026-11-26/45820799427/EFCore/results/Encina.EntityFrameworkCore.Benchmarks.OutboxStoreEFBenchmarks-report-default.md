
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.46GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                              | Mean        | Error     | StdDev    | Ratio | RatioSD | Allocated  | Alloc Ratio |
------------------------------------ |------------:|----------:|----------:|------:|--------:|-----------:|------------:|
 'AddAsync single message'           |    39.80 μs | 159.37 μs |  8.736 μs |  1.03 |    0.27 |    2.86 KB |        1.00 |
 'GetPendingMessagesAsync batch=10'  |   431.01 μs | 581.28 μs | 31.862 μs | 11.17 |    2.17 |  146.41 KB |       51.20 |
 'GetPendingMessagesAsync batch=100' | 2,815.49 μs | 950.15 μs | 52.081 μs | 72.98 |   13.43 | 1341.01 KB |      468.99 |
 MarkAsProcessedAsync                |   143.51 μs | 281.82 μs | 15.447 μs |  3.72 |    0.77 |   11.91 KB |        4.16 |
