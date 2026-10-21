```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.14GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method       | Mean     | Error    | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------- |---------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| AddAsync     | 238.3 μs | 180.7 μs |  9.90 μs |  0.93 |    0.05 | 1.2207 |  20.55 KB |        0.91 |
| GetByIdAsync | 255.6 μs | 221.4 μs | 12.14 μs |  1.00 |    0.06 | 1.2207 |  22.53 KB |        1.00 |
