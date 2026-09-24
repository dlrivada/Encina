```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                              | Mean        | Error     | StdDev    | Ratio | RatioSD | Allocated  | Alloc Ratio |
|------------------------------------ |------------:|----------:|----------:|------:|--------:|-----------:|------------:|
| &#39;AddAsync single message&#39;           |    54.89 μs | 217.70 μs | 11.933 μs |  1.03 |    0.28 |    2.86 KB |        1.00 |
| &#39;GetPendingMessagesAsync batch=10&#39;  |   649.01 μs | 804.81 μs | 44.114 μs | 12.21 |    2.46 |  146.34 KB |       51.18 |
| &#39;GetPendingMessagesAsync batch=100&#39; | 3,776.32 μs | 441.82 μs | 24.217 μs | 71.06 |   13.68 | 1341.01 KB |      468.99 |
| MarkAsProcessedAsync                |   168.28 μs | 156.86 μs |  8.598 μs |  3.17 |    0.63 |   11.91 KB |        4.16 |
