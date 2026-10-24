```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                              | Mean        | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------------ |------------:|----------:|----------:|------:|--------:|----------:|------------:|
| &#39;AddAsync single message&#39;           |    32.34 μs |  89.70 μs |  4.917 μs |  1.02 |    0.19 |   2.86 KB |        1.00 |
| &#39;GetPendingMessagesAsync batch=10&#39;  |   408.07 μs | 143.22 μs |  7.851 μs | 12.81 |    1.70 | 146.34 KB |       51.18 |
| &#39;GetPendingMessagesAsync batch=100&#39; | 2,826.37 μs | 585.14 μs | 32.073 μs | 88.76 |   11.73 | 1341.2 KB |      469.05 |
| MarkAsProcessedAsync                |   117.35 μs | 531.22 μs | 29.118 μs |  3.69 |    0.93 |  11.91 KB |        4.16 |
