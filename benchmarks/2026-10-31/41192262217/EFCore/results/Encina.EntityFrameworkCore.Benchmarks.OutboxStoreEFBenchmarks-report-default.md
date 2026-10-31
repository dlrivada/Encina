
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                              | Mean        | Error     | StdDev    | Ratio | RatioSD | Allocated  | Alloc Ratio |
------------------------------------ |------------:|----------:|----------:|------:|--------:|-----------:|------------:|
 'AddAsync single message'           |    53.42 μs |  76.56 μs |  4.196 μs |  1.00 |    0.10 |    2.86 KB |        1.00 |
 'GetPendingMessagesAsync batch=10'  |   552.57 μs |  35.35 μs |  1.938 μs | 10.39 |    0.71 |     147 KB |       51.41 |
 'GetPendingMessagesAsync batch=100' | 3,706.79 μs | 958.35 μs | 52.530 μs | 69.67 |    4.84 | 1341.23 KB |      469.07 |
 MarkAsProcessedAsync                |   135.45 μs |  81.46 μs |  4.465 μs |  2.55 |    0.19 |   11.91 KB |        4.16 |
