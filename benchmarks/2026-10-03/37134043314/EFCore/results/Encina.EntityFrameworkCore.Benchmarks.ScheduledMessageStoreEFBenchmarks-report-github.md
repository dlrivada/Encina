```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                    | Mean        | Error      | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------- |------------:|-----------:|----------:|------:|--------:|----------:|------------:|
| GetDueMessagesAsync       | 1,051.27 μs | 146.753 μs |  8.044 μs |  1.00 |    0.01 | 349.05 KB |       1.000 |
| AddAsync                  |    51.81 μs |   7.638 μs |  0.419 μs |  0.05 |    0.00 |   3.42 KB |       0.010 |
| RescheduleRecurringAsync  |   142.32 μs | 198.643 μs | 10.888 μs |  0.14 |    0.01 |  13.13 KB |       0.038 |
| MarkAsFailedAsync         |   134.12 μs |  36.879 μs |  2.021 μs |  0.13 |    0.00 |  13.03 KB |       0.037 |
| GetRecurringMessagesAsync |   608.08 μs | 492.753 μs | 27.009 μs |  0.58 |    0.02 |  166.7 KB |       0.478 |
