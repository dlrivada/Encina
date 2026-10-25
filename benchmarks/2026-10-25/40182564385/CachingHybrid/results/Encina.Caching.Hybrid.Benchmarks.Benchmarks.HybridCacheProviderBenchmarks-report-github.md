```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean     | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------- |---------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| ExistsAsync_False  | 797.1 ns | 708.69 ns | 38.85 ns |  0.81 |    0.03 | 0.0362 |     616 B |        1.24 |
| ExistsAsync_True   | 854.6 ns | 527.25 ns | 28.90 ns |  0.87 |    0.03 | 0.0362 |     616 B |        1.24 |
| GetAsync_CacheHit  | 983.0 ns |  79.84 ns |  4.38 ns |  1.00 |    0.01 | 0.0286 |     496 B |        1.00 |
| GetAsync_CacheMiss | 758.2 ns | 150.98 ns |  8.28 ns |  0.77 |    0.01 | 0.0362 |     616 B |        1.24 |
