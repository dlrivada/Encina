
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.38GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                              | Mean        | Error     | StdDev    | Ratio | RatioSD | Allocated  | Alloc Ratio |
------------------------------------ |------------:|----------:|----------:|------:|--------:|-----------:|------------:|
 'AddAsync single message'           |    68.30 μs |  62.76 μs |  3.440 μs |  1.00 |    0.06 |    2.86 KB |        1.00 |
 'GetPendingMessagesAsync batch=10'  |   567.47 μs | 281.19 μs | 15.413 μs |  8.32 |    0.42 |  146.34 KB |       51.18 |
 'GetPendingMessagesAsync batch=100' | 3,587.36 μs | 777.33 μs | 42.608 μs | 52.61 |    2.42 | 1341.16 KB |      469.04 |
 MarkAsProcessedAsync                |   192.88 μs | 201.38 μs | 11.038 μs |  2.83 |    0.19 |   11.91 KB |        4.16 |
