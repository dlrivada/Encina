```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.13GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method       | Mean     | Error    | StdDev  | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------- |---------:|---------:|--------:|------:|--------:|-------:|----------:|------------:|
| AddAsync     | 544.2 μs | 95.65 μs | 5.24 μs |  3.67 |    0.06 |      - |   6.63 KB |        1.04 |
| GetByIdAsync | 148.4 μs | 39.75 μs | 2.18 μs |  1.00 |    0.02 | 0.2441 |    6.4 KB |        1.00 |
