```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                              | Mean        | Error     | StdDev    | Ratio | RatioSD | Allocated  | Alloc Ratio |
|------------------------------------ |------------:|----------:|----------:|------:|--------:|-----------:|------------:|
| &#39;AddAsync single message&#39;           |    75.64 μs |  69.42 μs |  3.805 μs |  1.00 |    0.06 |    2.86 KB |        1.00 |
| &#39;GetPendingMessagesAsync batch=10&#39;  |   519.90 μs | 486.22 μs | 26.651 μs |  6.89 |    0.43 |  146.41 KB |       51.20 |
| &#39;GetPendingMessagesAsync batch=100&#39; | 3,086.69 μs | 450.43 μs | 24.690 μs | 40.88 |    1.84 | 1341.01 KB |      468.99 |
| MarkAsProcessedAsync                |   202.26 μs | 106.79 μs |  5.853 μs |  2.68 |    0.14 |   11.91 KB |        4.16 |
