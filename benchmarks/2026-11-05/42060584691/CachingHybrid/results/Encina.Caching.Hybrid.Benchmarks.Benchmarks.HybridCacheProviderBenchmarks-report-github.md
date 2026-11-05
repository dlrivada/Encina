```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean     | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |---------:|----------:|----------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  | 1.328 μs | 0.1054 μs | 0.0058 μs |  0.80 | 0.0362 |     616 B |        1.24 |
| ExistsAsync_True   | 1.415 μs | 0.0536 μs | 0.0029 μs |  0.85 | 0.0362 |     616 B |        1.24 |
| GetAsync_CacheHit  | 1.665 μs | 0.0175 μs | 0.0010 μs |  1.00 | 0.0286 |     496 B |        1.00 |
| GetAsync_CacheMiss | 1.312 μs | 0.0429 μs | 0.0024 μs |  0.79 | 0.0362 |     616 B |        1.24 |
