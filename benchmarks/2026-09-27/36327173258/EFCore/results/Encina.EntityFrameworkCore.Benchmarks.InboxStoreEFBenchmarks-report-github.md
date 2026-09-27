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
| MarkAsProcessedAsync    | 209.46 μs | 276.95 μs | 15.180 μs |  1.16 |    0.08 |  11.97 KB |        1.20 |
| GetExpiredMessagesAsync | 814.64 μs | 380.25 μs | 20.843 μs |  4.50 |    0.14 | 287.61 KB |       28.72 |
| AddAsync                |  69.76 μs |  53.04 μs |  2.907 μs |  0.39 |    0.02 |   2.85 KB |        0.28 |
| GetMessageAsync         | 181.08 μs |  88.17 μs |  4.833 μs |  1.00 |    0.03 |  10.02 KB |        1.00 |
