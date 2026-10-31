```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean     | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |---------:|----------:|----------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  | 1.367 μs | 0.0213 μs | 0.0012 μs |  0.83 | 0.0362 |     616 B |        1.24 |
| ExistsAsync_True   | 1.423 μs | 0.1002 μs | 0.0055 μs |  0.86 | 0.0362 |     616 B |        1.24 |
| GetAsync_CacheHit  | 1.652 μs | 0.0464 μs | 0.0025 μs |  1.00 | 0.0286 |     496 B |        1.00 |
| GetAsync_CacheMiss | 1.279 μs | 0.0388 μs | 0.0021 μs |  0.77 | 0.0362 |     616 B |        1.24 |
