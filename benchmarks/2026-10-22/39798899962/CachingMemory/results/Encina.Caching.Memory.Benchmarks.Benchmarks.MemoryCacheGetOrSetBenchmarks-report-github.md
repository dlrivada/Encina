```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean       | Error       | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-----------:|------------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| GetOrSetAsync_CacheHit  |   232.0 ns |     2.45 ns |   0.13 ns |  1.00 |    0.00 | 0.0167 |      - |     280 B |        1.00 |
| GetOrSetAsync_CacheMiss | 5,348.3 ns | 1,847.57 ns | 101.27 ns | 23.05 |    0.38 | 0.0610 | 0.0572 |    1080 B |        3.86 |
