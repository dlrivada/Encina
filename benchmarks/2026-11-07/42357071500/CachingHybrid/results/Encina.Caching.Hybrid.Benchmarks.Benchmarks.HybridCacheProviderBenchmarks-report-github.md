```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.23GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean     | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |---------:|----------:|----------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  | 1.284 μs | 0.0630 μs | 0.0035 μs |  0.80 | 0.0362 |     616 B |        1.24 |
| ExistsAsync_True   | 1.366 μs | 0.0325 μs | 0.0018 μs |  0.85 | 0.0362 |     616 B |        1.24 |
| GetAsync_CacheHit  | 1.604 μs | 0.0464 μs | 0.0025 μs |  1.00 | 0.0286 |     496 B |        1.00 |
| GetAsync_CacheMiss | 1.314 μs | 0.0681 μs | 0.0037 μs |  0.82 | 0.0362 |     616 B |        1.24 |
