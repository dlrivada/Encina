```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean     | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |---------:|----------:|----------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  | 1.263 μs | 0.0999 μs | 0.0055 μs |  0.78 | 0.0362 |     616 B |        1.24 |
| ExistsAsync_True   | 1.360 μs | 0.2778 μs | 0.0152 μs |  0.84 | 0.0362 |     616 B |        1.24 |
| GetAsync_CacheHit  | 1.617 μs | 0.0497 μs | 0.0027 μs |  1.00 | 0.0286 |     496 B |        1.00 |
| GetAsync_CacheMiss | 1.242 μs | 0.0574 μs | 0.0031 μs |  0.77 | 0.0362 |     616 B |        1.24 |
