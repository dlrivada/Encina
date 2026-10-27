```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean     | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |---------:|----------:|----------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  | 1.360 μs | 0.0768 μs | 0.0042 μs |  0.82 | 0.0362 |     616 B |        1.24 |
| ExistsAsync_True   | 1.445 μs | 0.1838 μs | 0.0101 μs |  0.87 | 0.0362 |     616 B |        1.24 |
| GetAsync_CacheHit  | 1.665 μs | 0.0828 μs | 0.0045 μs |  1.00 | 0.0286 |     496 B |        1.00 |
| GetAsync_CacheMiss | 1.290 μs | 0.0239 μs | 0.0013 μs |  0.78 | 0.0362 |     616 B |        1.24 |
