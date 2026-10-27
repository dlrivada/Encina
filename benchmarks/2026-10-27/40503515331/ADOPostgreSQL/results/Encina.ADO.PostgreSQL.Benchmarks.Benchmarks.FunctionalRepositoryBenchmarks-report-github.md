```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method       | Mean     | Error    | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------- |---------:|---------:|--------:|------:|-------:|----------:|------------:|
| AddAsync     | 162.6 μs | 17.24 μs | 0.94 μs |  1.06 | 0.9766 |  16.02 KB |        1.40 |
| GetByIdAsync | 153.2 μs | 27.80 μs | 1.52 μs |  1.00 | 0.4883 |   11.4 KB |        1.00 |
