```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method       | Mean     | Error    | StdDev  | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------- |---------:|---------:|--------:|------:|--------:|-------:|----------:|------------:|
| AddAsync     | 162.1 μs | 40.28 μs | 2.21 μs |  1.08 |    0.02 | 0.9766 |  16.01 KB |        1.40 |
| GetByIdAsync | 150.9 μs | 47.78 μs | 2.62 μs |  1.00 |    0.02 | 0.4883 |   11.4 KB |        1.00 |
