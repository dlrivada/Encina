```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| ExistsAsync_False  |  42.56 ns |  1.076 ns | 0.059 ns |  0.27 |    0.01 |      - |         - |        0.00 |
| ExistsAsync_True   |  47.73 ns |  0.575 ns | 0.032 ns |  0.30 |    0.01 |      - |         - |        0.00 |
| GetAsync_CacheHit  | 159.76 ns | 90.367 ns | 4.953 ns |  1.00 |    0.04 | 0.0017 |     144 B |        1.00 |
| GetAsync_CacheMiss | 105.47 ns |  8.046 ns | 0.441 ns |  0.66 |    0.02 |      - |         - |        0.00 |
