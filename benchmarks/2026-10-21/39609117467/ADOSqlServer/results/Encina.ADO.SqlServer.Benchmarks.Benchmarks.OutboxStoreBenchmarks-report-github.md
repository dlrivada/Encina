```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.42GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                           | Mean     | Error    | StdDev   | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------------------- |---------:|---------:|---------:|------:|--------:|-------:|-------:|----------:|------------:|
| AddAsync_Single                  | 563.8 μs | 672.7 μs | 36.87 μs |  1.00 |    0.08 | 0.9766 |      - |  17.59 KB |        1.00 |
| GetPendingMessagesAsync_Batch100 | 614.6 μs | 399.7 μs | 21.91 μs |  1.09 |    0.07 | 6.8359 | 0.9766 | 125.59 KB |        7.14 |
