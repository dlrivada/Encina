```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.00GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| GetOrSetAsync_CacheHit  | 1.103 μs | 0.2237 μs | 0.0123 μs |  1.00 |    0.01 | 0.0057 |      - |     560 B |        1.00 |
| GetOrSetAsync_CacheMiss | 4.576 μs | 2.3417 μs | 0.1284 μs |  4.15 |    0.11 | 0.0153 | 0.0076 |    1792 B |        3.20 |
| GetOrSetAsync_WithTags  | 7.586 μs | 5.5519 μs | 0.3043 μs |  6.88 |    0.25 | 0.0229 | 0.0153 |    2216 B |        3.96 |
