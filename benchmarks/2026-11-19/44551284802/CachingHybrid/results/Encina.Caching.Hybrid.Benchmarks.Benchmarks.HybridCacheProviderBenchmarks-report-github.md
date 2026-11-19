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
| ExistsAsync_False  | 1.338 μs | 0.1398 μs | 0.0077 μs |  0.79 | 0.0362 |     616 B |        1.24 |
| ExistsAsync_True   | 1.418 μs | 0.0746 μs | 0.0041 μs |  0.84 | 0.0362 |     616 B |        1.24 |
| GetAsync_CacheHit  | 1.691 μs | 0.2234 μs | 0.0122 μs |  1.00 | 0.0286 |     496 B |        1.00 |
| GetAsync_CacheMiss | 1.283 μs | 0.1145 μs | 0.0063 μs |  0.76 | 0.0362 |     616 B |        1.24 |
