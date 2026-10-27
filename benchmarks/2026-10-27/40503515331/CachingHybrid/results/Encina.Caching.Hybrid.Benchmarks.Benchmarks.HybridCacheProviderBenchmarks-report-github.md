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
| ExistsAsync_False  | 1.321 μs | 0.0674 μs | 0.0037 μs |  0.80 | 0.0362 |     616 B |        1.24 |
| ExistsAsync_True   | 1.440 μs | 0.0229 μs | 0.0013 μs |  0.87 | 0.0362 |     616 B |        1.24 |
| GetAsync_CacheHit  | 1.660 μs | 0.0358 μs | 0.0020 μs |  1.00 | 0.0286 |     496 B |        1.00 |
| GetAsync_CacheMiss | 1.308 μs | 0.0740 μs | 0.0041 μs |  0.79 | 0.0362 |     616 B |        1.24 |
