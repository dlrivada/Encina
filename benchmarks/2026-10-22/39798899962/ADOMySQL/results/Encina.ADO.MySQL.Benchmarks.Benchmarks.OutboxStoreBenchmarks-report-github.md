```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.14GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                           | Mean     | Error    | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------------- |---------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| AddAsync_Single                  | 540.0 μs | 118.4 μs |  6.49 μs |  1.00 |    0.01 |      - |   6.86 KB |        1.00 |
| GetPendingMessagesAsync_Batch100 | 359.3 μs | 186.3 μs | 10.21 μs |  0.67 |    0.02 | 2.9297 |  48.56 KB |        7.08 |
