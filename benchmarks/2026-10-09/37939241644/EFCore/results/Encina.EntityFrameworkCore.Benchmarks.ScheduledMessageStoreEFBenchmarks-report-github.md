```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                    | Mean        | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------- |------------:|----------:|----------:|------:|--------:|----------:|------------:|
| GetDueMessagesAsync       | 1,064.82 μs |  80.11 μs |  4.391 μs |  1.00 |    0.01 | 349.28 KB |       1.000 |
| AddAsync                  |    56.04 μs | 147.72 μs |  8.097 μs |  0.05 |    0.01 |   3.42 KB |       0.010 |
| RescheduleRecurringAsync  |   142.45 μs |  68.61 μs |  3.761 μs |  0.13 |    0.00 |  13.13 KB |       0.038 |
| MarkAsFailedAsync         |   136.44 μs |  87.88 μs |  4.817 μs |  0.13 |    0.00 |  13.03 KB |       0.037 |
| GetRecurringMessagesAsync |   618.13 μs | 622.55 μs | 34.124 μs |  0.58 |    0.03 | 166.63 KB |       0.477 |
