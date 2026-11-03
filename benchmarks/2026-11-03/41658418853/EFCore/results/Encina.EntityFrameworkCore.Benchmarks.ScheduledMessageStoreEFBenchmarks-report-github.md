```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.35GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                    | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------- |----------:|----------:|----------:|------:|--------:|----------:|------------:|
| GetDueMessagesAsync       | 797.01 μs | 408.51 μs | 22.392 μs |  1.00 |    0.03 | 349.05 KB |       1.000 |
| AddAsync                  |  38.49 μs |  78.81 μs |  4.320 μs |  0.05 |    0.00 |   3.42 KB |       0.010 |
| RescheduleRecurringAsync  | 129.00 μs | 369.39 μs | 20.248 μs |  0.16 |    0.02 |  13.13 KB |       0.038 |
| MarkAsFailedAsync         | 118.61 μs | 172.96 μs |  9.480 μs |  0.15 |    0.01 |  13.03 KB |       0.037 |
| GetRecurringMessagesAsync | 468.82 μs | 542.99 μs | 29.763 μs |  0.59 |    0.04 | 166.55 KB |       0.477 |
