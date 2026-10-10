```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method       | Mean     | Error    | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------- |---------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| AddAsync     | 236.9 μs | 132.4 μs |  7.25 μs |  0.84 |    0.05 | 0.9766 |  20.55 KB |        0.91 |
| GetByIdAsync | 281.3 μs | 310.6 μs | 17.02 μs |  1.00 |    0.07 | 0.9766 |  22.54 KB |        1.00 |
