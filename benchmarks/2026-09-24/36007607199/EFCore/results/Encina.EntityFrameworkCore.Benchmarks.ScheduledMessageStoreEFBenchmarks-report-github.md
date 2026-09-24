```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                    | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------- |----------:|----------:|----------:|------:|--------:|----------:|------------:|
| GetDueMessagesAsync       | 825.63 μs | 358.06 μs | 19.626 μs |  1.00 |    0.03 | 349.05 KB |       1.000 |
| AddAsync                  |  40.24 μs | 112.42 μs |  6.162 μs |  0.05 |    0.01 |   3.42 KB |       0.010 |
| RescheduleRecurringAsync  | 121.85 μs | 204.58 μs | 11.214 μs |  0.15 |    0.01 |  13.13 KB |       0.038 |
| MarkAsFailedAsync         | 132.38 μs | 226.62 μs | 12.422 μs |  0.16 |    0.01 |  13.03 KB |       0.037 |
| GetRecurringMessagesAsync | 477.46 μs | 505.96 μs | 27.734 μs |  0.58 |    0.03 | 166.87 KB |       0.478 |
