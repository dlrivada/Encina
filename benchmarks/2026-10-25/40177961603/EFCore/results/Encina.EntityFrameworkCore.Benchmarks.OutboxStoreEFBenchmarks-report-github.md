```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                              | Mean        | Error     | StdDev    | Ratio | RatioSD | Allocated  | Alloc Ratio |
|------------------------------------ |------------:|----------:|----------:|------:|--------:|-----------:|------------:|
| &#39;AddAsync single message&#39;           |    37.13 μs |  95.12 μs |  5.214 μs |  1.01 |    0.17 |    2.86 KB |        1.00 |
| &#39;GetPendingMessagesAsync batch=10&#39;  |   456.15 μs | 735.52 μs | 40.316 μs | 12.44 |    1.72 |  146.45 KB |       51.22 |
| &#39;GetPendingMessagesAsync batch=100&#39; | 2,739.82 μs | 575.71 μs | 31.557 μs | 74.71 |    8.61 | 1341.12 KB |      469.02 |
| MarkAsProcessedAsync                |   136.16 μs | 532.19 μs | 29.171 μs |  3.71 |    0.81 |   11.91 KB |        4.16 |
