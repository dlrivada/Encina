```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method       | Mean     | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------- |---------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| AddAsync     | 600.9 μs | 552.66 μs | 30.29 μs |  2.66 |    0.12 | 0.9766 |  15.82 KB |        0.89 |
| GetByIdAsync | 225.6 μs |  66.85 μs |  3.66 μs |  1.00 |    0.02 | 0.9766 |  17.79 KB |        1.00 |
