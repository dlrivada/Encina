```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method       | Mean     | Error     | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------- |---------:|----------:|---------:|------:|--------:|----------:|------------:|
| AddAsync     | 275.6 μs |  41.07 μs |  2.25 μs |  0.93 |    0.03 |  20.55 KB |        0.91 |
| GetByIdAsync | 297.9 μs | 186.26 μs | 10.21 μs |  1.00 |    0.04 |  22.52 KB |        1.00 |
