```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.67GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean     | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |---------:|----------:|----------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  | 1.351 μs | 0.0290 μs | 0.0016 μs |  0.83 | 0.0362 |     616 B |        1.24 |
| ExistsAsync_True   | 1.471 μs | 0.0809 μs | 0.0044 μs |  0.90 | 0.0362 |     616 B |        1.24 |
| GetAsync_CacheHit  | 1.626 μs | 0.0364 μs | 0.0020 μs |  1.00 | 0.0286 |     496 B |        1.00 |
| GetAsync_CacheMiss | 1.300 μs | 0.0572 μs | 0.0031 μs |  0.80 | 0.0362 |     616 B |        1.24 |
