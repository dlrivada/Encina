```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.49GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean       | Error       | StdDev   | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-----------:|------------:|---------:|------:|--------:|-------:|-------:|----------:|------------:|
| GetOrSetAsync_CacheHit  |   133.9 ns |    59.61 ns |  3.27 ns |  1.00 |    0.03 | 0.0167 |      - |     280 B |        1.00 |
| GetOrSetAsync_CacheMiss | 4,063.5 ns | 1,706.26 ns | 93.53 ns | 30.35 |    0.88 | 0.0610 | 0.0572 |    1080 B |        3.86 |
