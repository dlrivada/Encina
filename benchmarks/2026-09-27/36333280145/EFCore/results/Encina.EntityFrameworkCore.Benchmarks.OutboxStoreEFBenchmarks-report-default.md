
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                              | Mean        | Error     | StdDev    | Ratio | RatioSD | Allocated  | Alloc Ratio |
------------------------------------ |------------:|----------:|----------:|------:|--------:|-----------:|------------:|
 'AddAsync single message'           |    49.97 μs |  46.75 μs |  2.562 μs |  1.00 |    0.06 |    2.86 KB |        1.00 |
 'GetPendingMessagesAsync batch=10'  |   490.41 μs | 139.84 μs |  7.665 μs |  9.83 |    0.44 |  146.34 KB |       51.18 |
 'GetPendingMessagesAsync batch=100' | 3,518.65 μs | 185.39 μs | 10.162 μs | 70.54 |    3.05 | 1341.32 KB |      469.10 |
 MarkAsProcessedAsync                |   138.77 μs |  93.59 μs |  5.130 μs |  2.78 |    0.15 |   11.91 KB |        4.16 |
