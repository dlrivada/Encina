```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.49GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                           | Mean     | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------------- |---------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| AddAsync_Single                  | 204.6 μs |  65.62 μs |  3.60 μs |  1.00 |    0.02 | 1.2207 |   20.8 KB |        1.00 |
| GetPendingMessagesAsync_Batch100 | 572.4 μs | 701.02 μs | 38.43 μs |  2.80 |    0.17 | 4.8828 |  93.64 KB |        4.50 |
