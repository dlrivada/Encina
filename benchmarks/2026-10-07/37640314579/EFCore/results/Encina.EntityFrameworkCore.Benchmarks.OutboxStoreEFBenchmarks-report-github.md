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
| &#39;AddAsync single message&#39;           |    43.94 μs |  63.51 μs |  3.481 μs |  1.00 |    0.10 |    2.86 KB |        1.00 |
| &#39;GetPendingMessagesAsync batch=10&#39;  |   489.50 μs | 117.85 μs |  6.460 μs | 11.19 |    0.75 |  146.34 KB |       51.18 |
| &#39;GetPendingMessagesAsync batch=100&#39; | 3,462.54 μs | 321.87 μs | 17.643 μs | 79.12 |    5.24 | 1341.01 KB |      468.99 |
| MarkAsProcessedAsync                |   138.76 μs |  95.12 μs |  5.214 μs |  3.17 |    0.23 |   11.91 KB |        4.16 |
