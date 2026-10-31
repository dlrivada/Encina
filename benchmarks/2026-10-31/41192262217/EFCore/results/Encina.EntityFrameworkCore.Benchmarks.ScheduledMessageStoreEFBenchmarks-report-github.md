```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.23GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                    | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------- |----------:|----------:|----------:|------:|--------:|----------:|------------:|
| GetDueMessagesAsync       | 913.32 μs | 293.80 μs | 16.104 μs |  1.00 |    0.02 | 349.05 KB |       1.000 |
| AddAsync                  |  46.70 μs | 140.70 μs |  7.712 μs |  0.05 |    0.01 |   3.42 KB |       0.010 |
| RescheduleRecurringAsync  | 176.92 μs | 415.90 μs | 22.797 μs |  0.19 |    0.02 |  13.13 KB |       0.038 |
| MarkAsFailedAsync         | 166.49 μs | 170.26 μs |  9.332 μs |  0.18 |    0.01 |  13.03 KB |       0.037 |
| GetRecurringMessagesAsync | 522.66 μs | 321.53 μs | 17.624 μs |  0.57 |    0.02 | 166.55 KB |       0.477 |
