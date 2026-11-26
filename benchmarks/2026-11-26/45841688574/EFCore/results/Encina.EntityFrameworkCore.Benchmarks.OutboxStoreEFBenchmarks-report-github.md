```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.50GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                              | Mean        | Error     | StdDev    | Ratio | RatioSD | Allocated  | Alloc Ratio |
|------------------------------------ |------------:|----------:|----------:|------:|--------:|-----------:|------------:|
| &#39;AddAsync single message&#39;           |    33.71 μs |  83.35 μs |  4.569 μs |  1.01 |    0.17 |    2.86 KB |        1.00 |
| &#39;GetPendingMessagesAsync batch=10&#39;  |   377.30 μs | 248.25 μs | 13.607 μs | 11.33 |    1.36 |  146.34 KB |       51.18 |
| &#39;GetPendingMessagesAsync batch=100&#39; | 2,622.94 μs | 300.03 μs | 16.446 μs | 78.75 |    9.13 | 1341.01 KB |      468.99 |
| MarkAsProcessedAsync                |   102.66 μs | 251.08 μs | 13.763 μs |  3.08 |    0.51 |   11.91 KB |        4.16 |
