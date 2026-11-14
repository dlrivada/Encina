```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.13GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ExistsAsync_False  | 1.317 μs | 0.0295 μs | 0.0016 μs |  0.79 |    0.01 | 0.0362 |     616 B |        1.24 |
| ExistsAsync_True   | 1.389 μs | 0.0399 μs | 0.0022 μs |  0.84 |    0.01 | 0.0362 |     616 B |        1.24 |
| GetAsync_CacheHit  | 1.660 μs | 0.5450 μs | 0.0299 μs |  1.00 |    0.02 | 0.0286 |     496 B |        1.00 |
| GetAsync_CacheMiss | 1.274 μs | 0.0718 μs | 0.0039 μs |  0.77 |    0.01 | 0.0362 |     616 B |        1.24 |
