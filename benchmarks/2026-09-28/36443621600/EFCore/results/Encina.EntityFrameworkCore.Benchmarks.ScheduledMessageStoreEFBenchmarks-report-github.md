```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                    | Mean        | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------- |------------:|----------:|----------:|------:|--------:|----------:|------------:|
| GetDueMessagesAsync       | 1,063.73 μs | 106.52 μs |  5.839 μs |  1.00 |    0.01 | 349.46 KB |       1.000 |
| AddAsync                  |    51.93 μs |  63.94 μs |  3.505 μs |  0.05 |    0.00 |   3.42 KB |       0.010 |
| RescheduleRecurringAsync  |   141.06 μs | 105.88 μs |  5.804 μs |  0.13 |    0.00 |  13.13 KB |       0.038 |
| MarkAsFailedAsync         |   154.99 μs | 115.69 μs |  6.341 μs |  0.15 |    0.01 |  13.03 KB |       0.037 |
| GetRecurringMessagesAsync |   614.12 μs | 418.14 μs | 22.920 μs |  0.58 |    0.02 | 166.55 KB |       0.477 |
