```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.52GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                    | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------- |----------:|----------:|----------:|------:|--------:|----------:|------------:|
| GetDueMessagesAsync       | 772.33 μs | 300.39 μs | 16.466 μs |  1.00 |    0.03 | 349.05 KB |       1.000 |
| AddAsync                  |  31.66 μs |  95.57 μs |  5.239 μs |  0.04 |    0.01 |   3.42 KB |       0.010 |
| RescheduleRecurringAsync  | 105.10 μs | 198.48 μs | 10.879 μs |  0.14 |    0.01 |  13.13 KB |       0.038 |
| MarkAsFailedAsync         | 104.74 μs | 179.38 μs |  9.832 μs |  0.14 |    0.01 |  13.03 KB |       0.037 |
| GetRecurringMessagesAsync | 455.19 μs |  59.14 μs |  3.241 μs |  0.59 |    0.01 | 166.55 KB |       0.477 |
