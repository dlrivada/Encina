```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method       | Mean     | Error    | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------- |---------:|---------:|--------:|------:|-------:|----------:|------------:|
| AddAsync     | 166.6 μs | 25.42 μs | 1.39 μs |  1.10 | 0.9766 |  16.02 KB |        1.40 |
| GetByIdAsync | 151.3 μs |  3.51 μs | 0.19 μs |  1.00 | 0.4883 |  11.42 KB |        1.00 |
