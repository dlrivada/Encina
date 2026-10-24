```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                    | Mean      | Error       | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------- |----------:|------------:|----------:|------:|--------:|----------:|------------:|
| GetDueMessagesAsync       | 903.64 μs | 1,028.37 μs | 56.369 μs |  1.00 |    0.08 | 349.05 KB |       1.000 |
| AddAsync                  |  38.72 μs |   152.41 μs |  8.354 μs |  0.04 |    0.01 |   3.42 KB |       0.010 |
| RescheduleRecurringAsync  | 127.25 μs |   550.44 μs | 30.171 μs |  0.14 |    0.03 |  13.13 KB |       0.038 |
| MarkAsFailedAsync         | 150.94 μs |   470.39 μs | 25.784 μs |  0.17 |    0.03 |  13.03 KB |       0.037 |
| GetRecurringMessagesAsync | 551.25 μs |   736.84 μs | 40.389 μs |  0.61 |    0.05 | 166.67 KB |       0.478 |
