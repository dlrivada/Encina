```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=MediumRun  InvocationCount=1  IterationCount=15  
LaunchCount=2  UnrollFactor=1  WarmupCount=10  

```
| Method                              | Mean        | Error     | StdDev    | Ratio | RatioSD | Allocated  | Alloc Ratio |
|------------------------------------ |------------:|----------:|----------:|------:|--------:|-----------:|------------:|
| &#39;AddAsync single message&#39;           |    30.29 μs |  1.633 μs |  2.290 μs |  1.01 |    0.11 |    2.86 KB |        1.00 |
| &#39;GetPendingMessagesAsync batch=10&#39;  |   373.23 μs |  3.206 μs |  4.280 μs | 12.39 |    0.93 |  146.45 KB |       51.22 |
| &#39;GetPendingMessagesAsync batch=100&#39; | 2,632.19 μs | 37.060 μs | 55.470 μs | 87.39 |    6.76 | 1341.13 KB |      469.03 |
| MarkAsProcessedAsync                |    90.56 μs |  4.200 μs |  5.607 μs |  3.01 |    0.29 |   11.91 KB |        4.16 |
