```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                    | Mean        | Error       | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------- |------------:|------------:|----------:|------:|--------:|----------:|------------:|
| GetDueMessagesAsync       | 1,158.56 μs |   496.13 μs | 27.195 μs |  1.00 |    0.03 | 349.16 KB |       1.000 |
| AddAsync                  |    55.24 μs |    67.02 μs |  3.673 μs |  0.05 |    0.00 |   3.42 KB |       0.010 |
| RescheduleRecurringAsync  |   176.20 μs |   393.20 μs | 21.553 μs |  0.15 |    0.02 |  13.13 KB |       0.038 |
| MarkAsFailedAsync         |   171.56 μs |   201.44 μs | 11.042 μs |  0.15 |    0.01 |  13.03 KB |       0.037 |
| GetRecurringMessagesAsync |   709.35 μs | 1,331.32 μs | 72.974 μs |  0.61 |    0.06 | 166.55 KB |       0.477 |
