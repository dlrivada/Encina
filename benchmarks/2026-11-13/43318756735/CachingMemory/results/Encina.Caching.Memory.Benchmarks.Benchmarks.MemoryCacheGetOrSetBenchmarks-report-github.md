```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean       | Error       | StdDev   | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-----------:|------------:|---------:|------:|--------:|-------:|-------:|----------:|------------:|
| GetOrSetAsync_CacheHit  |   172.9 ns |     5.66 ns |  0.31 ns |  1.00 |    0.00 | 0.0167 |      - |     280 B |        1.00 |
| GetOrSetAsync_CacheMiss | 4,735.7 ns | 1,651.45 ns | 90.52 ns | 27.38 |    0.46 | 0.0610 | 0.0572 |    1080 B |        3.86 |
