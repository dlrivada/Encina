```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean     | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |---------:|----------:|----------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  | 1.429 μs | 0.0883 μs | 0.0048 μs |  0.82 | 0.0362 |     616 B |        1.24 |
| ExistsAsync_True   | 1.428 μs | 0.2284 μs | 0.0125 μs |  0.82 | 0.0362 |     616 B |        1.24 |
| GetAsync_CacheHit  | 1.738 μs | 0.0771 μs | 0.0042 μs |  1.00 | 0.0286 |     496 B |        1.00 |
| GetAsync_CacheMiss | 1.311 μs | 0.1962 μs | 0.0108 μs |  0.75 | 0.0362 |     616 B |        1.24 |
