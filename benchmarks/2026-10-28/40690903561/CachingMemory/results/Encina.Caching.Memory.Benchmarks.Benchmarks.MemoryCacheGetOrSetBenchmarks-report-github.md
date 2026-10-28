```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean       | Error       | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-----------:|------------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| GetOrSetAsync_CacheHit  |   220.2 ns |     6.02 ns |   0.33 ns |  1.00 |    0.00 | 0.0167 |      - |     280 B |        1.00 |
| GetOrSetAsync_CacheMiss | 4,789.7 ns | 3,863.97 ns | 211.80 ns | 21.75 |    0.83 | 0.0610 | 0.0534 |    1080 B |        3.86 |
