```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.28GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                              | Mean        | Error       | StdDev   | Ratio | RatioSD | Allocated  | Alloc Ratio |
|------------------------------------ |------------:|------------:|---------:|------:|--------:|-----------:|------------:|
| &#39;AddAsync single message&#39;           |    64.77 μs |   350.69 μs | 19.22 μs |  1.06 |    0.40 |    2.86 KB |        1.00 |
| &#39;GetPendingMessagesAsync batch=10&#39;  |   544.01 μs | 1,748.58 μs | 95.85 μs |  8.93 |    2.74 |  146.45 KB |       51.22 |
| &#39;GetPendingMessagesAsync batch=100&#39; | 3,053.69 μs | 1,406.28 μs | 77.08 μs | 50.12 |   13.26 | 1341.32 KB |      469.10 |
| MarkAsProcessedAsync                |   100.31 μs |   184.63 μs | 10.12 μs |  1.65 |    0.46 |   11.91 KB |        4.16 |
