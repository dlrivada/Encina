```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=MediumRun  InvocationCount=1  IterationCount=15  
LaunchCount=2  UnrollFactor=1  WarmupCount=10  

```
| Method                  | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------ |----------:|----------:|----------:|------:|--------:|----------:|------------:|
| MarkAsProcessedAsync    | 125.21 μs | 18.022 μs | 25.847 μs |  0.96 |    0.25 |  11.97 KB |        1.20 |
| GetExpiredMessagesAsync | 728.62 μs | 28.935 μs | 43.308 μs |  5.61 |    0.94 | 287.61 KB |       28.72 |
| AddAsync                |  27.19 μs |  2.800 μs |  4.105 μs |  0.21 |    0.05 |   2.85 KB |        0.28 |
| GetMessageAsync         | 132.79 μs | 13.370 μs | 20.012 μs |  1.02 |    0.22 |  10.02 KB |        1.00 |
