```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.20GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean       | Error       | StdDev   | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-----------:|------------:|---------:|------:|--------:|-------:|-------:|----------:|------------:|
| GetOrSetAsync_CacheHit  |   232.7 ns |    24.39 ns |  1.34 ns |  1.00 |    0.01 | 0.0167 |      - |     280 B |        1.00 |
| GetOrSetAsync_CacheMiss | 5,123.3 ns | 1,134.05 ns | 62.16 ns | 22.02 |    0.26 | 0.0610 | 0.0572 |    1080 B |        3.86 |
