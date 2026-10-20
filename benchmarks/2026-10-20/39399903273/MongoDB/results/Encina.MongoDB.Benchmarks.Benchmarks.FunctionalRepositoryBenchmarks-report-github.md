```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method       | Mean     | Error    | StdDev  | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------- |---------:|---------:|--------:|------:|--------:|-------:|----------:|------------:|
| AddAsync     | 248.7 μs | 45.02 μs | 2.47 μs |  0.89 |    0.01 | 1.2207 |  20.55 KB |        0.91 |
| GetByIdAsync | 280.6 μs | 75.74 μs | 4.15 μs |  1.00 |    0.02 | 0.9766 |  22.51 KB |        1.00 |
