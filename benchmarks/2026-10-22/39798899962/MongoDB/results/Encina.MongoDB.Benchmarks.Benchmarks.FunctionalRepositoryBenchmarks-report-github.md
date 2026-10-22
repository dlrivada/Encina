```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method       | Mean     | Error     | StdDev  | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------- |---------:|----------:|--------:|------:|--------:|-------:|----------:|------------:|
| AddAsync     | 228.1 μs | 166.31 μs | 9.12 μs |  0.89 |    0.03 | 1.2207 |  20.55 KB |        0.91 |
| GetByIdAsync | 256.2 μs |  57.91 μs | 3.17 μs |  1.00 |    0.02 | 0.9766 |  22.52 KB |        1.00 |
