```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                              | Mean        | Error     | StdDev    | Ratio | RatioSD | Allocated  | Alloc Ratio |
|------------------------------------ |------------:|----------:|----------:|------:|--------:|-----------:|------------:|
| &#39;AddAsync single message&#39;           |    46.26 μs |  66.50 μs |  3.645 μs |  1.00 |    0.10 |    2.86 KB |        1.00 |
| &#39;GetPendingMessagesAsync batch=10&#39;  |   510.00 μs | 489.84 μs | 26.850 μs | 11.07 |    0.93 |  146.34 KB |       51.18 |
| &#39;GetPendingMessagesAsync batch=100&#39; | 3,509.18 μs | 368.94 μs | 20.223 μs | 76.19 |    5.40 | 1341.01 KB |      468.99 |
| MarkAsProcessedAsync                |   139.33 μs | 133.28 μs |  7.305 μs |  3.03 |    0.25 |   11.91 KB |        4.16 |
