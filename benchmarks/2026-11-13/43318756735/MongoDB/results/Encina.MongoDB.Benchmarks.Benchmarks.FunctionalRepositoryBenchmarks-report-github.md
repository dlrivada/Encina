```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.31GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method       | Mean     | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------- |---------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| AddAsync     | 212.8 μs |  93.86 μs |  5.15 μs |  0.85 |    0.04 | 1.2207 |  20.55 KB |        0.91 |
| GetByIdAsync | 251.5 μs | 208.28 μs | 11.42 μs |  1.00 |    0.06 | 1.2207 |  22.54 KB |        1.00 |
