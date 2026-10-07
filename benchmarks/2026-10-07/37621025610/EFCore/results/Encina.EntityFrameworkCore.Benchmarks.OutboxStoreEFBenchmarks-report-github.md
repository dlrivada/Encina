```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                              | Mean        | Error     | StdDev    | Ratio | RatioSD | Allocated  | Alloc Ratio |
|------------------------------------ |------------:|----------:|----------:|------:|--------:|-----------:|------------:|
| &#39;AddAsync single message&#39;           |    34.24 μs |  65.76 μs |  3.604 μs |  1.01 |    0.13 |    2.86 KB |        1.00 |
| &#39;GetPendingMessagesAsync batch=10&#39;  |   394.66 μs | 513.83 μs | 28.165 μs | 11.61 |    1.27 |  146.48 KB |       51.23 |
| &#39;GetPendingMessagesAsync batch=100&#39; | 2,570.56 μs | 131.49 μs |  7.207 μs | 75.64 |    6.82 | 1341.27 KB |      469.08 |
| MarkAsProcessedAsync                |   100.62 μs |  74.70 μs |  4.095 μs |  2.96 |    0.29 |   11.91 KB |        4.16 |
