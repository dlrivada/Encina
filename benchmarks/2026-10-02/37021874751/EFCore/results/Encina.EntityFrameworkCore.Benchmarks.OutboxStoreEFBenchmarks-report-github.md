```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                              | Mean        | Error       | StdDev    | Ratio | RatioSD | Allocated  | Alloc Ratio |
|------------------------------------ |------------:|------------:|----------:|------:|--------:|-----------:|------------:|
| &#39;AddAsync single message&#39;           |    87.54 μs |   230.06 μs | 12.610 μs |  1.01 |    0.17 |    2.86 KB |        1.00 |
| &#39;GetPendingMessagesAsync batch=10&#39;  |   538.05 μs |   516.54 μs | 28.314 μs |  6.23 |    0.78 |  146.34 KB |       51.18 |
| &#39;GetPendingMessagesAsync batch=100&#39; | 3,408.68 μs | 1,633.41 μs | 89.533 μs | 39.45 |    4.68 | 1341.01 KB |      468.99 |
| MarkAsProcessedAsync                |   196.42 μs |    86.32 μs |  4.732 μs |  2.27 |    0.27 |   11.91 KB |        4.16 |
