
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.55GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=MediumRun  InvocationCount=1  IterationCount=15  
LaunchCount=2  UnrollFactor=1  WarmupCount=10  

 Method                              | Mean        | Error     | StdDev    | Ratio | RatioSD | Allocated  | Alloc Ratio |
------------------------------------ |------------:|----------:|----------:|------:|--------:|-----------:|------------:|
 'AddAsync single message'           |    28.16 μs |  2.236 μs |  3.207 μs |  1.01 |    0.16 |    2.86 KB |        1.00 |
 'GetPendingMessagesAsync batch=10'  |   369.38 μs |  9.430 μs | 13.220 μs | 13.28 |    1.53 |  146.41 KB |       51.20 |
 'GetPendingMessagesAsync batch=100' | 2,634.84 μs | 19.860 μs | 29.111 μs | 94.71 |   10.40 | 1341.01 KB |      468.99 |
 MarkAsProcessedAsync                |    92.43 μs |  4.236 μs |  6.075 μs |  3.32 |    0.42 |   11.91 KB |        4.16 |
