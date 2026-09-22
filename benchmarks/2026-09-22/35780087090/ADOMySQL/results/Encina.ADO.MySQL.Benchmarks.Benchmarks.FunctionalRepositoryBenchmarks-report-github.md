```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method       | Mean       | Error        | StdDev      | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------- |-----------:|-------------:|------------:|------:|--------:|-------:|----------:|------------:|
| AddAsync     | 3,411.5 μs | 33,327.35 μs | 1,826.78 μs | 22.76 |   10.56 |      - |   6.63 KB |        1.04 |
| GetByIdAsync |   150.0 μs |     64.62 μs |     3.54 μs |  1.00 |    0.03 | 0.2441 |   6.38 KB |        1.00 |
