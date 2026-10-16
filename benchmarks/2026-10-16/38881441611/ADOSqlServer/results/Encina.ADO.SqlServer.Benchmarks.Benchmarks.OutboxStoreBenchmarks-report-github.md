```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                           | Mean     | Error    | StdDev   | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------------------- |---------:|---------:|---------:|------:|--------:|-------:|-------:|----------:|------------:|
| AddAsync_Single                  | 598.9 μs | 186.2 μs | 10.21 μs |  1.00 |    0.02 | 0.9766 |      - |  16.51 KB |        1.00 |
| GetPendingMessagesAsync_Batch100 | 635.7 μs | 246.9 μs | 13.53 μs |  1.06 |    0.03 | 6.8359 | 0.9766 | 124.49 KB |        7.54 |
