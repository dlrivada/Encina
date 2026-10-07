```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                  | Mean      | Error       | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------ |----------:|------------:|----------:|------:|--------:|----------:|------------:|
| MarkAsProcessedAsync    | 141.63 μs |   567.04 μs |  31.08 μs |  1.01 |    0.25 |  12.27 KB |        1.22 |
| GetExpiredMessagesAsync | 866.10 μs | 1,948.66 μs | 106.81 μs |  6.19 |    1.18 | 288.07 KB |       28.76 |
| AddAsync                |  47.14 μs |   214.03 μs |  11.73 μs |  0.34 |    0.09 |   2.85 KB |        0.28 |
| GetMessageAsync         | 143.31 μs |   503.51 μs |  27.60 μs |  1.02 |    0.24 |  10.02 KB |        1.00 |
