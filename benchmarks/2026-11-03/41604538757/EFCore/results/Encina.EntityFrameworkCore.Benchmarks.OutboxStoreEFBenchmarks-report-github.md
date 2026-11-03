```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.32GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                              | Mean        | Error     | StdDev    | Ratio | RatioSD | Allocated  | Alloc Ratio |
|------------------------------------ |------------:|----------:|----------:|------:|--------:|-----------:|------------:|
| &#39;AddAsync single message&#39;           |    77.29 μs |  88.36 μs |  4.843 μs |  1.00 |    0.08 |    2.86 KB |        1.00 |
| &#39;GetPendingMessagesAsync batch=10&#39;  |   558.23 μs | 106.43 μs |  5.834 μs |  7.24 |    0.38 |  146.48 KB |       51.23 |
| &#39;GetPendingMessagesAsync batch=100&#39; | 3,275.93 μs | 648.84 μs | 35.565 μs | 42.49 |    2.26 | 1341.01 KB |      468.99 |
| MarkAsProcessedAsync                |   220.19 μs | 553.20 μs | 30.323 μs |  2.86 |    0.37 |   11.91 KB |        4.16 |
