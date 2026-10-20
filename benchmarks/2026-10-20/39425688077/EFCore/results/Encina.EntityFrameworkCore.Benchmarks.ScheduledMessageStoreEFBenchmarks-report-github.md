```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.14GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                    | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------- |----------:|----------:|----------:|------:|--------:|----------:|------------:|
| GetDueMessagesAsync       | 886.85 μs | 704.84 μs | 38.634 μs |  1.00 |    0.05 | 349.05 KB |       1.000 |
| AddAsync                  |  39.01 μs |  94.49 μs |  5.179 μs |  0.04 |    0.01 |   3.42 KB |       0.010 |
| RescheduleRecurringAsync  | 175.29 μs | 613.09 μs | 33.605 μs |  0.20 |    0.03 |  13.13 KB |       0.038 |
| MarkAsFailedAsync         | 163.52 μs | 817.63 μs | 44.817 μs |  0.18 |    0.04 |  13.03 KB |       0.037 |
| GetRecurringMessagesAsync | 510.76 μs | 622.01 μs | 34.095 μs |  0.58 |    0.04 | 166.55 KB |       0.477 |
