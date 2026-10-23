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
| ExistsAsync_False  | 791.7 ns | 425.91 ns | 23.35 ns |  0.81 |    0.02 | 0.0362 |     616 B |        1.24 |
| ExistsAsync_True   | 848.5 ns | 142.98 ns |  7.84 ns |  0.87 |    0.01 | 0.0362 |     616 B |        1.24 |
| GetAsync_CacheHit  | 978.0 ns |  49.19 ns |  2.70 ns |  1.00 |    0.00 | 0.0286 |     496 B |        1.00 |
| GetAsync_CacheMiss | 746.7 ns |  75.52 ns |  4.14 ns |  0.76 |    0.00 | 0.0362 |     616 B |        1.24 |
