```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                    | Mean        | Error     | StdDev    | Ratio | Allocated | Alloc Ratio |
|-------------------------- |------------:|----------:|----------:|------:|----------:|------------:|
| GetDueMessagesAsync       | 1,056.72 μs | 161.34 μs |  8.843 μs |  1.00 | 349.05 KB |       1.000 |
| AddAsync                  |    47.54 μs |  26.43 μs |  1.449 μs |  0.04 |   3.42 KB |       0.010 |
| RescheduleRecurringAsync  |   152.19 μs | 328.69 μs | 18.017 μs |  0.14 |  13.13 KB |       0.038 |
| MarkAsFailedAsync         |   136.59 μs |  34.87 μs |  1.911 μs |  0.13 |  13.03 KB |       0.037 |
| GetRecurringMessagesAsync |   588.37 μs |  54.25 μs |  2.974 μs |  0.56 | 166.55 KB |       0.477 |
