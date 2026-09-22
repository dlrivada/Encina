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
| GetDueMessagesAsync       | 772.81 μs | 305.14 μs | 16.726 μs |  1.00 |    0.03 | 349.13 KB |       1.000 |
| AddAsync                  |  35.46 μs |  66.35 μs |  3.637 μs |  0.05 |    0.00 |   3.42 KB |       0.010 |
| RescheduleRecurringAsync  | 110.76 μs | 311.41 μs | 17.069 μs |  0.14 |    0.02 |  13.13 KB |       0.038 |
| MarkAsFailedAsync         | 110.00 μs | 140.23 μs |  7.687 μs |  0.14 |    0.01 |  13.03 KB |       0.037 |
| GetRecurringMessagesAsync | 475.34 μs | 502.77 μs | 27.559 μs |  0.62 |    0.03 | 166.55 KB |       0.477 |
