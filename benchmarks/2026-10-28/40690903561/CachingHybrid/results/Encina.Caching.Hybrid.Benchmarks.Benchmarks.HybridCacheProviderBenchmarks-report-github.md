```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.63GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean     | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |---------:|----------:|----------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  | 1.371 μs | 0.0855 μs | 0.0047 μs |  0.83 | 0.0362 |     616 B |        1.24 |
| ExistsAsync_True   | 1.431 μs | 0.0602 μs | 0.0033 μs |  0.86 | 0.0362 |     616 B |        1.24 |
| GetAsync_CacheHit  | 1.656 μs | 0.0985 μs | 0.0054 μs |  1.00 | 0.0286 |     496 B |        1.00 |
| GetAsync_CacheMiss | 1.323 μs | 0.0994 μs | 0.0054 μs |  0.80 | 0.0362 |     616 B |        1.24 |
