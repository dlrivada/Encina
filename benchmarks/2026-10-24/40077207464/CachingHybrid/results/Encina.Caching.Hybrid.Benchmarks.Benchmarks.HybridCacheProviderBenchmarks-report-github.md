```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean     | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |---------:|----------:|----------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  | 1.314 μs | 0.3309 μs | 0.0181 μs |  0.79 | 0.0362 |     616 B |        1.24 |
| ExistsAsync_True   | 1.399 μs | 0.0778 μs | 0.0043 μs |  0.84 | 0.0362 |     616 B |        1.24 |
| GetAsync_CacheHit  | 1.670 μs | 0.0748 μs | 0.0041 μs |  1.00 | 0.0286 |     496 B |        1.00 |
| GetAsync_CacheMiss | 1.322 μs | 0.0223 μs | 0.0012 μs |  0.79 | 0.0362 |     616 B |        1.24 |
