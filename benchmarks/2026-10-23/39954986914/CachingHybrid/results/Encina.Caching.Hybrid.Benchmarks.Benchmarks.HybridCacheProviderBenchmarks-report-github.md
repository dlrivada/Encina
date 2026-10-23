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
| ExistsAsync_False  | 1.314 μs | 0.0349 μs | 0.0019 μs |  0.81 | 0.0362 |     616 B |        1.24 |
| ExistsAsync_True   | 1.425 μs | 0.0377 μs | 0.0021 μs |  0.88 | 0.0362 |     616 B |        1.24 |
| GetAsync_CacheHit  | 1.625 μs | 0.0156 μs | 0.0009 μs |  1.00 | 0.0286 |     496 B |        1.00 |
| GetAsync_CacheMiss | 1.293 μs | 0.0178 μs | 0.0010 μs |  0.80 | 0.0362 |     616 B |        1.24 |
