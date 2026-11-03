```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.01GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean     | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |---------:|----------:|----------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  | 1.340 μs | 0.0291 μs | 0.0016 μs |  0.81 | 0.0362 |     616 B |        1.24 |
| ExistsAsync_True   | 1.405 μs | 0.3202 μs | 0.0176 μs |  0.85 | 0.0362 |     616 B |        1.24 |
| GetAsync_CacheHit  | 1.649 μs | 0.0198 μs | 0.0011 μs |  1.00 | 0.0286 |     496 B |        1.00 |
| GetAsync_CacheMiss | 1.320 μs | 0.0424 μs | 0.0023 μs |  0.80 | 0.0362 |     616 B |        1.24 |
