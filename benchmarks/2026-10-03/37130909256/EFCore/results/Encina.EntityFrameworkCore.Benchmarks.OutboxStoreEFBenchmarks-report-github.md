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
| &#39;AddAsync single message&#39;           |    33.17 μs |  88.23 μs |  4.836 μs |  1.01 |    0.18 |    2.86 KB |        1.00 |
| &#39;GetPendingMessagesAsync batch=10&#39;  |   366.42 μs | 249.25 μs | 13.662 μs | 11.20 |    1.44 |  146.34 KB |       51.18 |
| &#39;GetPendingMessagesAsync batch=100&#39; | 2,609.29 μs | 650.26 μs | 35.643 μs | 79.77 |    9.97 | 1341.13 KB |      469.03 |
| MarkAsProcessedAsync                |   102.99 μs | 184.07 μs | 10.090 μs |  3.15 |    0.48 |   11.91 KB |        4.16 |
