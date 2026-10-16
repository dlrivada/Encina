
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                              | Mean        | Error       | StdDev    | Ratio | RatioSD | Allocated  | Alloc Ratio |
------------------------------------ |------------:|------------:|----------:|------:|--------:|-----------:|------------:|
 'AddAsync single message'           |    55.19 μs |   149.91 μs |  8.217 μs |  1.01 |    0.18 |    2.86 KB |        1.00 |
 'GetPendingMessagesAsync batch=10'  |   513.05 μs |   581.84 μs | 31.892 μs |  9.43 |    1.26 |  146.48 KB |       51.23 |
 'GetPendingMessagesAsync batch=100' | 3,578.31 μs | 1,075.96 μs | 58.977 μs | 65.76 |    8.09 | 1341.01 KB |      468.99 |
 MarkAsProcessedAsync                |   141.04 μs |   134.53 μs |  7.374 μs |  2.59 |    0.34 |   11.91 KB |        4.16 |
