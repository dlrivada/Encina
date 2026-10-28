```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.63GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| GetOrSetAsync_CacheHit  |  1.476 μs | 0.0433 μs | 0.0024 μs |  1.00 |    0.00 | 0.0324 |      - |     560 B |        1.00 |
| GetOrSetAsync_CacheMiss |  7.011 μs | 1.9065 μs | 0.1045 μs |  4.75 |    0.06 | 0.1068 | 0.0992 |    1792 B |        3.20 |
| GetOrSetAsync_WithTags  | 10.538 μs | 4.7371 μs | 0.2597 μs |  7.14 |    0.15 | 0.1221 | 0.1068 |    2216 B |        3.96 |
