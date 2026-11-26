
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                              | Mean        | Error    | StdDev   | Ratio | RatioSD | Allocated  | Alloc Ratio |
------------------------------------ |------------:|---------:|---------:|------:|--------:|-----------:|------------:|
 'AddAsync single message'           |    74.82 μs | 58.82 μs | 3.224 μs |  1.00 |    0.05 |    2.86 KB |        1.00 |
 'GetPendingMessagesAsync batch=10'  |   537.16 μs | 39.16 μs | 2.147 μs |  7.19 |    0.27 |  146.34 KB |       51.18 |
 'GetPendingMessagesAsync batch=100' | 3,318.49 μs | 45.82 μs | 2.512 μs | 44.41 |    1.69 | 1341.16 KB |      469.04 |
 MarkAsProcessedAsync                |   195.08 μs | 29.53 μs | 1.619 μs |  2.61 |    0.10 |   11.91 KB |        4.16 |
