
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                              | Mean        | Error     | StdDev   | Ratio | RatioSD | Allocated  | Alloc Ratio |
------------------------------------ |------------:|----------:|---------:|------:|--------:|-----------:|------------:|
 'AddAsync single message'           |    35.06 μs | 222.18 μs | 12.18 μs |  1.07 |    0.43 |    2.86 KB |        1.00 |
 'GetPendingMessagesAsync batch=10'  |   406.15 μs | 458.61 μs | 25.14 μs | 12.43 |    3.27 |  146.34 KB |       51.18 |
 'GetPendingMessagesAsync batch=100' | 2,617.30 μs | 683.74 μs | 37.48 μs | 80.12 |   20.60 | 1341.01 KB |      468.99 |
 MarkAsProcessedAsync                |   124.89 μs | 318.13 μs | 17.44 μs |  3.82 |    1.09 |   11.91 KB |        4.16 |
