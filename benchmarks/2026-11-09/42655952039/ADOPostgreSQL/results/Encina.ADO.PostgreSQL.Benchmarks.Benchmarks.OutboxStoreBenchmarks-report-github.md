```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.38GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                           | Mean     | Error     | StdDev  | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------------- |---------:|----------:|--------:|------:|--------:|-------:|----------:|------------:|
| AddAsync_Single                  | 115.1 μs |  30.22 μs | 1.66 μs |  1.00 |    0.02 | 0.2441 |   5.34 KB |        1.00 |
| GetPendingMessagesAsync_Batch100 | 249.8 μs | 122.89 μs | 6.74 μs |  2.17 |    0.06 | 2.4414 |  40.23 KB |        7.53 |
