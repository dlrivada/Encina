```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.53GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method       | Mean     | Error    | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------- |---------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| AddAsync     | 225.3 μs | 234.8 μs | 12.87 μs |  0.92 |    0.05 | 1.2207 |  20.55 KB |        0.91 |
| GetByIdAsync | 246.2 μs | 144.6 μs |  7.93 μs |  1.00 |    0.04 | 1.2207 |  22.52 KB |        1.00 |
