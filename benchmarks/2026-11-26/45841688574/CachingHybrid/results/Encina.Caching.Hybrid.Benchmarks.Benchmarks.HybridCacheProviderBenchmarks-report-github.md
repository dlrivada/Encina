```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.16GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean     | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------- |---------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| ExistsAsync_False  | 779.4 ns |  79.84 ns |  4.38 ns |  0.81 |    0.01 | 0.0362 |     616 B |        1.24 |
| ExistsAsync_True   | 823.9 ns | 110.19 ns |  6.04 ns |  0.86 |    0.01 | 0.0362 |     616 B |        1.24 |
| GetAsync_CacheHit  | 956.8 ns |  78.03 ns |  4.28 ns |  1.00 |    0.01 | 0.0286 |     496 B |        1.00 |
| GetAsync_CacheMiss | 773.6 ns | 354.82 ns | 19.45 ns |  0.81 |    0.02 | 0.0362 |     616 B |        1.24 |
