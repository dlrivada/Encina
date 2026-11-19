```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.02GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean     | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |---------:|----------:|----------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  | 1.325 μs | 0.0151 μs | 0.0008 μs |  0.79 | 0.0362 |     616 B |        1.24 |
| ExistsAsync_True   | 1.413 μs | 0.0545 μs | 0.0030 μs |  0.84 | 0.0362 |     616 B |        1.24 |
| GetAsync_CacheHit  | 1.676 μs | 0.0559 μs | 0.0031 μs |  1.00 | 0.0286 |     496 B |        1.00 |
| GetAsync_CacheMiss | 1.289 μs | 0.1527 μs | 0.0084 μs |  0.77 | 0.0362 |     616 B |        1.24 |
