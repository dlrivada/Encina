```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                           | Mean     | Error     | StdDev  | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------------- |---------:|----------:|--------:|------:|--------:|-------:|----------:|------------:|
| AddAsync_Single                  | 320.6 μs |  15.03 μs | 0.82 μs |  1.00 |    0.00 | 0.9766 |   20.8 KB |        1.00 |
| GetPendingMessagesAsync_Batch100 | 684.6 μs | 134.20 μs | 7.36 μs |  2.14 |    0.02 | 4.8828 |  93.64 KB |        4.50 |
