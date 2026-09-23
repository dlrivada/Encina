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
| GetDueMessagesAsync       | 795.07 μs | 526.27 μs | 28.846 μs |  1.00 |    0.04 | 349.05 KB |       1.000 |
| AddAsync                  |  32.48 μs |  46.81 μs |  2.566 μs |  0.04 |    0.00 |   3.42 KB |       0.010 |
| RescheduleRecurringAsync  | 104.47 μs | 166.47 μs |  9.125 μs |  0.13 |    0.01 |  13.13 KB |       0.038 |
| MarkAsFailedAsync         | 110.28 μs | 150.14 μs |  8.230 μs |  0.14 |    0.01 |  13.03 KB |       0.037 |
| GetRecurringMessagesAsync | 459.29 μs | 434.17 μs | 23.798 μs |  0.58 |    0.03 | 166.87 KB |       0.478 |
