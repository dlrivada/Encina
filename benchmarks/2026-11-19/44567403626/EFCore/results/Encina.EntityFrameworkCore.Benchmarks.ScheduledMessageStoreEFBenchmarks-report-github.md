```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.47GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                    | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------- |----------:|----------:|----------:|------:|--------:|----------:|------------:|
| GetDueMessagesAsync       | 934.37 μs | 604.95 μs | 33.159 μs |  1.00 |    0.04 | 349.05 KB |       1.000 |
| AddAsync                  |  42.11 μs | 110.94 μs |  6.081 μs |  0.05 |    0.01 |   3.42 KB |       0.010 |
| RescheduleRecurringAsync  | 124.25 μs | 483.82 μs | 26.520 μs |  0.13 |    0.02 |  13.13 KB |       0.038 |
| MarkAsFailedAsync         | 186.47 μs | 630.97 μs | 34.586 μs |  0.20 |    0.03 |  13.03 KB |       0.037 |
| GetRecurringMessagesAsync | 563.56 μs | 884.78 μs | 48.498 μs |  0.60 |    0.05 | 166.55 KB |       0.477 |
