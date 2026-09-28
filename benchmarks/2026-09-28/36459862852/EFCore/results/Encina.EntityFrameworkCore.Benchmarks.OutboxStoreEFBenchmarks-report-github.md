```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                              | Mean        | Error     | StdDev    | Ratio | RatioSD | Allocated  | Alloc Ratio |
|------------------------------------ |------------:|----------:|----------:|------:|--------:|-----------:|------------:|
| &#39;AddAsync single message&#39;           |    48.14 μs |  70.15 μs |  3.845 μs |  1.00 |    0.10 |    2.86 KB |        1.00 |
| &#39;GetPendingMessagesAsync batch=10&#39;  |   515.54 μs | 662.26 μs | 36.301 μs | 10.76 |    1.02 |  146.45 KB |       51.22 |
| &#39;GetPendingMessagesAsync batch=100&#39; | 3,464.60 μs | 315.04 μs | 17.268 μs | 72.29 |    5.25 | 1341.13 KB |      469.03 |
| MarkAsProcessedAsync                |   137.01 μs | 236.77 μs | 12.978 μs |  2.86 |    0.31 |   11.91 KB |        4.16 |
