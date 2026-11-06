```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.18GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                    | Mean      | Error     | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------- |----------:|----------:|---------:|------:|--------:|----------:|------------:|
| GetDueMessagesAsync       | 965.96 μs | 911.34 μs | 49.95 μs |  1.00 |    0.06 | 349.13 KB |       1.000 |
| AddAsync                  |  56.24 μs | 203.54 μs | 11.16 μs |  0.06 |    0.01 |   3.42 KB |       0.010 |
| RescheduleRecurringAsync  | 156.67 μs | 547.77 μs | 30.02 μs |  0.16 |    0.03 |  13.13 KB |       0.038 |
| MarkAsFailedAsync         | 190.99 μs | 438.49 μs | 24.04 μs |  0.20 |    0.02 |  13.03 KB |       0.037 |
| GetRecurringMessagesAsync | 512.54 μs | 797.11 μs | 43.69 μs |  0.53 |    0.05 | 166.55 KB |       0.477 |
