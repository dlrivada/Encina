```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.27GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                              | Mean        | Error     | StdDev    | Ratio | RatioSD | Allocated  | Alloc Ratio |
|------------------------------------ |------------:|----------:|----------:|------:|--------:|-----------:|------------:|
| &#39;AddAsync single message&#39;           |    34.85 μs |  89.87 μs |  4.926 μs |  1.01 |    0.17 |    2.86 KB |        1.00 |
| &#39;GetPendingMessagesAsync batch=10&#39;  |   422.27 μs | 431.58 μs | 23.656 μs | 12.27 |    1.52 |  146.34 KB |       51.18 |
| &#39;GetPendingMessagesAsync batch=100&#39; | 2,787.02 μs | 297.06 μs | 16.283 μs | 80.99 |    9.25 | 1341.13 KB |      469.03 |
| MarkAsProcessedAsync                |   111.90 μs | 213.35 μs | 11.694 μs |  3.25 |    0.47 |   11.91 KB |        4.16 |
