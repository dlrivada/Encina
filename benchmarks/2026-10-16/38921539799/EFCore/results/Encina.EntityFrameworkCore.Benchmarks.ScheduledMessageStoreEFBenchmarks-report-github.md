```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                    | Mean      | Error     | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------- |----------:|----------:|---------:|------:|--------:|----------:|------------:|
| GetDueMessagesAsync       | 976.75 μs | 700.20 μs | 38.38 μs |  1.00 |    0.05 | 349.48 KB |       1.000 |
| AddAsync                  |  51.78 μs | 182.60 μs | 10.01 μs |  0.05 |    0.01 |   3.42 KB |       0.010 |
| RescheduleRecurringAsync  | 225.17 μs | 662.15 μs | 36.29 μs |  0.23 |    0.03 |  13.25 KB |       0.038 |
| MarkAsFailedAsync         | 220.00 μs | 930.78 μs | 51.02 μs |  0.23 |    0.05 |  13.03 KB |       0.037 |
| GetRecurringMessagesAsync | 610.63 μs | 916.09 μs | 50.21 μs |  0.63 |    0.05 | 166.55 KB |       0.477 |
