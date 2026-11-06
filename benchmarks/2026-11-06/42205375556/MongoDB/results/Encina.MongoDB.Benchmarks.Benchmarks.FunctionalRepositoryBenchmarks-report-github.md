```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.37GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method       | Mean     | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------- |---------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| AddAsync     | 222.7 μs |  95.80 μs |  5.25 μs |  0.84 |    0.05 | 1.2207 |  20.55 KB |        0.91 |
| GetByIdAsync | 265.2 μs | 332.27 μs | 18.21 μs |  1.00 |    0.08 | 0.9766 |  22.54 KB |        1.00 |
