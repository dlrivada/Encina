```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.52GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                           | Mean     | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------------- |---------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| AddAsync_Single                  | 202.5 μs |  71.26 μs |  3.91 μs |  1.00 |    0.02 | 1.2207 |   20.8 KB |        1.00 |
| GetPendingMessagesAsync_Batch100 | 481.1 μs | 270.87 μs | 14.85 μs |  2.38 |    0.07 | 4.8828 |  93.64 KB |        4.50 |
