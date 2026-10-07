```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method       | Mean     | Error     | StdDev  | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------- |---------:|----------:|--------:|------:|--------:|-------:|----------:|------------:|
| AddAsync     | 241.1 μs |  78.58 μs | 4.31 μs |  0.88 |    0.02 | 1.2207 |  20.55 KB |        0.91 |
| GetByIdAsync | 275.4 μs | 133.31 μs | 7.31 μs |  1.00 |    0.03 | 0.9766 |  22.52 KB |        1.00 |
