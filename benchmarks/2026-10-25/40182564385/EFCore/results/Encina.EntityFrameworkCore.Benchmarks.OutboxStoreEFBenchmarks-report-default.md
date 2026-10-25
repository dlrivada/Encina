
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                              | Mean        | Error     | StdDev    | Ratio | RatioSD | Allocated  | Alloc Ratio |
------------------------------------ |------------:|----------:|----------:|------:|--------:|-----------:|------------:|
 'AddAsync single message'           |    49.38 μs |  50.34 μs |  2.760 μs |  1.00 |    0.07 |    2.86 KB |        1.00 |
 'GetPendingMessagesAsync batch=10'  |   509.38 μs | 516.09 μs | 28.289 μs | 10.34 |    0.71 |  146.45 KB |       51.22 |
 'GetPendingMessagesAsync batch=100' | 3,508.66 μs | 408.32 μs | 22.382 μs | 71.21 |    3.48 | 1341.16 KB |      469.04 |
 MarkAsProcessedAsync                |   134.44 μs |  73.21 μs |  4.013 μs |  2.73 |    0.15 |   11.91 KB |        4.16 |
