```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.21GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                    | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------- |----------:|----------:|----------:|------:|--------:|----------:|------------:|
| GetDueMessagesAsync       | 887.54 μs | 997.94 μs | 54.700 μs |  1.00 |    0.07 | 349.05 KB |       1.000 |
| AddAsync                  |  47.13 μs | 188.55 μs | 10.335 μs |  0.05 |    0.01 |   3.42 KB |       0.010 |
| RescheduleRecurringAsync  | 174.57 μs | 511.59 μs | 28.042 μs |  0.20 |    0.03 |  13.13 KB |       0.038 |
| MarkAsFailedAsync         | 118.16 μs | 167.54 μs |  9.184 μs |  0.13 |    0.01 |  13.03 KB |       0.037 |
| GetRecurringMessagesAsync | 533.53 μs | 783.46 μs | 42.944 μs |  0.60 |    0.05 | 166.55 KB |       0.477 |
