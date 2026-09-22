```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                  | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------ |----------:|----------:|----------:|------:|--------:|----------:|------------:|
| MarkAsProcessedAsync    | 123.97 μs | 814.02 μs | 44.619 μs |  1.03 |    0.35 |  11.97 KB |        1.20 |
| GetExpiredMessagesAsync | 655.38 μs | 551.82 μs | 30.247 μs |  5.45 |    0.79 | 287.61 KB |       28.72 |
| AddAsync                |  33.79 μs |  80.14 μs |  4.393 μs |  0.28 |    0.05 |   2.85 KB |        0.28 |
| GetMessageAsync         | 122.65 μs | 388.76 μs | 21.309 μs |  1.02 |    0.21 |  10.02 KB |        1.00 |
