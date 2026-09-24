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
| &#39;AddAsync single message&#39;           |    46.68 μs |  39.40 μs |  2.160 μs |  1.00 |    0.06 |    2.86 KB |        1.00 |
| &#39;GetPendingMessagesAsync batch=10&#39;  |   490.52 μs | 136.54 μs |  7.484 μs | 10.52 |    0.45 |  146.34 KB |       51.18 |
| &#39;GetPendingMessagesAsync batch=100&#39; | 3,557.54 μs | 401.65 μs | 22.016 μs | 76.32 |    3.16 | 1341.01 KB |      468.99 |
| MarkAsProcessedAsync                |   137.06 μs | 139.17 μs |  7.628 μs |  2.94 |    0.19 |   11.91 KB |        4.16 |
