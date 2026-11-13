```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.48GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method       | Mean     | Error    | StdDev  | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------- |---------:|---------:|--------:|------:|--------:|-------:|----------:|------------:|
| AddAsync     | 150.7 μs | 11.38 μs | 0.62 μs |  1.07 |    0.03 | 0.9766 |  16.03 KB |        1.40 |
| GetByIdAsync | 140.8 μs | 69.70 μs | 3.82 μs |  1.00 |    0.03 | 0.4883 |  11.42 KB |        1.00 |
