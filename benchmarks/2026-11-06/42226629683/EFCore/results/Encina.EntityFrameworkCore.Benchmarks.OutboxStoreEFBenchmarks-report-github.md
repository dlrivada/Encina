```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                              | Mean        | Error     | StdDev    | Ratio | RatioSD | Allocated  | Alloc Ratio |
|------------------------------------ |------------:|----------:|----------:|------:|--------:|-----------:|------------:|
| &#39;AddAsync single message&#39;           |    44.53 μs |  49.58 μs |  2.718 μs |  1.00 |    0.07 |    2.86 KB |        1.00 |
| &#39;GetPendingMessagesAsync batch=10&#39;  |   507.89 μs | 254.52 μs | 13.951 μs | 11.43 |    0.64 |  146.34 KB |       51.18 |
| &#39;GetPendingMessagesAsync batch=100&#39; | 3,620.03 μs | 765.03 μs | 41.934 μs | 81.49 |    4.24 | 1341.01 KB |      468.99 |
| MarkAsProcessedAsync                |   133.37 μs |  86.61 μs |  4.748 μs |  3.00 |    0.18 |   11.91 KB |        4.16 |
