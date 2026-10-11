```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=MediumRun  InvocationCount=1  IterationCount=15  
LaunchCount=2  UnrollFactor=1  WarmupCount=10  

```
| Method                              | Mean        | Error    | StdDev    | Ratio  | RatioSD | Allocated  | Alloc Ratio |
|------------------------------------ |------------:|---------:|----------:|-------:|--------:|-----------:|------------:|
| &#39;AddAsync single message&#39;           |    22.60 μs | 1.717 μs |  2.350 μs |   1.01 |    0.14 |    2.86 KB |        1.00 |
| &#39;GetPendingMessagesAsync batch=10&#39;  |   358.45 μs | 5.384 μs |  8.058 μs |  16.02 |    1.64 |  146.34 KB |       51.18 |
| &#39;GetPendingMessagesAsync batch=100&#39; | 2,566.89 μs | 9.957 μs | 14.595 μs | 114.74 |   11.49 | 1341.01 KB |      468.99 |
| MarkAsProcessedAsync                |    95.21 μs | 5.504 μs |  8.068 μs |   4.26 |    0.56 |   11.91 KB |        4.16 |
