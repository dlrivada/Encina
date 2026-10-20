```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.21GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method       | Mean     | Error     | StdDev  | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------- |---------:|----------:|--------:|------:|--------:|-------:|----------:|------------:|
| AddAsync     | 162.5 μs | 128.01 μs | 7.02 μs |  1.08 |    0.04 | 0.9766 |  16.03 KB |        1.40 |
| GetByIdAsync | 149.9 μs |   9.38 μs | 0.51 μs |  1.00 |    0.00 | 0.4883 |  11.41 KB |        1.00 |
