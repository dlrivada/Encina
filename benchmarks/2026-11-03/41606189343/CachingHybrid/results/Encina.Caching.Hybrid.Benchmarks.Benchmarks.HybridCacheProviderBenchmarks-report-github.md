```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 2.85GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean     | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |---------:|----------:|----------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  | 1.377 μs | 0.0373 μs | 0.0020 μs |  0.85 | 0.0362 |     616 B |        1.24 |
| ExistsAsync_True   | 1.428 μs | 0.0108 μs | 0.0006 μs |  0.88 | 0.0362 |     616 B |        1.24 |
| GetAsync_CacheHit  | 1.621 μs | 0.0340 μs | 0.0019 μs |  1.00 | 0.0286 |     496 B |        1.00 |
| GetAsync_CacheMiss | 1.348 μs | 0.0228 μs | 0.0012 μs |  0.83 | 0.0362 |     616 B |        1.24 |
