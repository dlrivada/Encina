```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean     | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |---------:|----------:|----------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  | 1.090 μs | 0.0347 μs | 0.0019 μs |  0.85 | 0.0362 |     616 B |        1.24 |
| ExistsAsync_True   | 1.123 μs | 0.0162 μs | 0.0009 μs |  0.88 | 0.0362 |     616 B |        1.24 |
| GetAsync_CacheHit  | 1.283 μs | 0.0211 μs | 0.0012 μs |  1.00 | 0.0286 |     496 B |        1.00 |
| GetAsync_CacheMiss | 1.066 μs | 0.0277 μs | 0.0015 μs |  0.83 | 0.0362 |     616 B |        1.24 |
