```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                    | Mean        | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------- |------------:|----------:|----------:|------:|--------:|----------:|------------:|
| GetDueMessagesAsync       | 1,075.28 μs | 247.83 μs | 13.584 μs |  1.00 |    0.02 | 349.05 KB |       1.000 |
| AddAsync                  |    54.80 μs |  79.60 μs |  4.363 μs |  0.05 |    0.00 |   3.42 KB |       0.010 |
| RescheduleRecurringAsync  |   162.07 μs | 537.84 μs | 29.481 μs |  0.15 |    0.02 |  13.13 KB |       0.038 |
| MarkAsFailedAsync         |   141.49 μs | 113.41 μs |  6.216 μs |  0.13 |    0.01 |  13.03 KB |       0.037 |
| GetRecurringMessagesAsync |   663.68 μs | 334.68 μs | 18.345 μs |  0.62 |    0.02 | 166.55 KB |       0.477 |
