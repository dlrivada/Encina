```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| GetOrSetAsync_CacheHit  | 1.083 μs | 0.0757 μs | 0.0041 μs |  1.00 |    0.00 | 0.0324 |      - |     560 B |        1.00 |
| GetOrSetAsync_CacheMiss | 5.653 μs | 4.0689 μs | 0.2230 μs |  5.22 |    0.18 | 0.1068 | 0.0992 |    1792 B |        3.20 |
| GetOrSetAsync_WithTags  | 8.600 μs | 2.3643 μs | 0.1296 μs |  7.94 |    0.11 | 0.1221 | 0.1068 |    2216 B |        3.96 |
