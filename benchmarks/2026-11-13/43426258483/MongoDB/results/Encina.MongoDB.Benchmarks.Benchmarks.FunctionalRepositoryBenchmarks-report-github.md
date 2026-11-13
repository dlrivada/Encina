```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method       | Mean     | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------- |---------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| AddAsync     | 323.1 μs |  40.94 μs |  2.24 μs |  0.91 |    0.02 | 0.9766 |  20.55 KB |        0.91 |
| GetByIdAsync | 356.6 μs | 198.57 μs | 10.88 μs |  1.00 |    0.04 | 0.9766 |  22.52 KB |        1.00 |
