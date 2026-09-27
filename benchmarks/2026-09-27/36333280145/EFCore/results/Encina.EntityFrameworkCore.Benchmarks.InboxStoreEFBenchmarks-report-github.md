```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                  | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------ |----------:|----------:|----------:|------:|--------:|----------:|------------:|
| MarkAsProcessedAsync    | 202.40 μs | 211.94 μs | 11.617 μs |  1.20 |    0.07 |  11.97 KB |        1.20 |
| GetExpiredMessagesAsync | 800.93 μs | 445.96 μs | 24.445 μs |  4.74 |    0.20 | 287.92 KB |       28.75 |
| AddAsync                |  69.98 μs |  18.26 μs |  1.001 μs |  0.41 |    0.01 |   2.85 KB |        0.28 |
| GetMessageAsync         | 169.11 μs | 122.25 μs |  6.701 μs |  1.00 |    0.05 |  10.02 KB |        1.00 |
