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
| ExistsAsync_False  | 1.350 μs | 0.0182 μs | 0.0010 μs |  0.82 | 0.0362 |     616 B |        1.24 |
| ExistsAsync_True   | 1.446 μs | 0.0512 μs | 0.0028 μs |  0.88 | 0.0362 |     616 B |        1.24 |
| GetAsync_CacheHit  | 1.650 μs | 0.0324 μs | 0.0018 μs |  1.00 | 0.0286 |     496 B |        1.00 |
| GetAsync_CacheMiss | 1.299 μs | 0.0062 μs | 0.0003 μs |  0.79 | 0.0362 |     616 B |        1.24 |
