```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                              | Mean        | Error       | StdDev     | Ratio | RatioSD | Allocated  | Alloc Ratio |
|------------------------------------ |------------:|------------:|-----------:|------:|--------:|-----------:|------------:|
| &#39;AddAsync single message&#39;           |    63.26 μs |    89.41 μs |   4.901 μs |  1.00 |    0.09 |    2.86 KB |        1.00 |
| &#39;GetPendingMessagesAsync batch=10&#39;  |   519.87 μs |   208.13 μs |  11.408 μs |  8.25 |    0.56 |  146.34 KB |       51.18 |
| &#39;GetPendingMessagesAsync batch=100&#39; | 3,687.30 μs | 2,235.23 μs | 122.521 μs | 58.52 |    4.16 | 1341.43 KB |      469.13 |
| MarkAsProcessedAsync                |   137.82 μs |   202.10 μs |  11.078 μs |  2.19 |    0.21 |   11.91 KB |        4.16 |
