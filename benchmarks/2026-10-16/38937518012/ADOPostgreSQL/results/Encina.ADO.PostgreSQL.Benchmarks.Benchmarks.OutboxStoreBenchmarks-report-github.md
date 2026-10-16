```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                           | Mean     | Error    | StdDev  | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------------- |---------:|---------:|--------:|------:|--------:|-------:|----------:|------------:|
| AddAsync_Single                  | 125.9 μs | 34.25 μs | 1.88 μs |  1.00 |    0.02 | 0.2441 |   5.29 KB |        1.00 |
| GetPendingMessagesAsync_Batch100 | 254.6 μs | 13.92 μs | 0.76 μs |  2.02 |    0.03 | 2.4414 |  40.16 KB |        7.59 |
