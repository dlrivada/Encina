```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                              | Mean        | Error     | StdDev    | Ratio | RatioSD | Allocated  | Alloc Ratio |
|------------------------------------ |------------:|----------:|----------:|------:|--------:|-----------:|------------:|
| &#39;AddAsync single message&#39;           |    71.24 μs |  49.53 μs |  2.715 μs |  1.00 |    0.05 |    2.86 KB |        1.00 |
| &#39;GetPendingMessagesAsync batch=10&#39;  |   568.71 μs | 410.12 μs | 22.480 μs |  7.99 |    0.38 |  146.56 KB |       51.26 |
| &#39;GetPendingMessagesAsync batch=100&#39; | 3,307.35 μs |  67.01 μs |  3.673 μs | 46.47 |    1.50 | 1341.01 KB |      468.99 |
| MarkAsProcessedAsync                |   211.33 μs | 383.31 μs | 21.010 μs |  2.97 |    0.27 |   11.91 KB |        4.16 |
