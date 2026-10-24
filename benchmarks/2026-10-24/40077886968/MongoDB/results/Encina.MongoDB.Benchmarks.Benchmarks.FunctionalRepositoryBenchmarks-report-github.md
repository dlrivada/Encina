```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method       | Mean     | Error     | StdDev  | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------- |---------:|----------:|--------:|------:|--------:|-------:|----------:|------------:|
| AddAsync     | 331.2 μs |  35.22 μs | 1.93 μs |  0.92 |    0.01 | 0.9766 |  20.55 KB |        0.91 |
| GetByIdAsync | 358.7 μs | 116.43 μs | 6.38 μs |  1.00 |    0.02 | 0.9766 |  22.52 KB |        1.00 |
