```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.72GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------- |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ExistsAsync_False  | 1.413 μs | 0.2459 μs | 0.0135 μs |  0.84 |    0.01 | 0.0229 |     616 B |        1.24 |
| ExistsAsync_True   | 1.465 μs | 0.1400 μs | 0.0077 μs |  0.87 |    0.01 | 0.0229 |     616 B |        1.24 |
| GetAsync_CacheHit  | 1.683 μs | 0.4935 μs | 0.0271 μs |  1.00 |    0.02 | 0.0191 |     496 B |        1.00 |
| GetAsync_CacheMiss | 1.343 μs | 0.0505 μs | 0.0028 μs |  0.80 |    0.01 | 0.0229 |     616 B |        1.24 |
