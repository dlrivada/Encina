```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean     | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |---------:|----------:|----------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  | 1.054 μs | 0.0168 μs | 0.0009 μs |  0.81 | 0.0362 |     616 B |        1.24 |
| ExistsAsync_True   | 1.139 μs | 0.0522 μs | 0.0029 μs |  0.88 | 0.0362 |     616 B |        1.24 |
| GetAsync_CacheHit  | 1.294 μs | 0.1565 μs | 0.0086 μs |  1.00 | 0.0286 |     496 B |        1.00 |
| GetAsync_CacheMiss | 1.037 μs | 0.0436 μs | 0.0024 μs |  0.80 | 0.0362 |     616 B |        1.24 |
