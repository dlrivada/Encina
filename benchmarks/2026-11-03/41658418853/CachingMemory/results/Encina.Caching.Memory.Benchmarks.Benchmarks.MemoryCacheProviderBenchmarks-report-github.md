```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.31GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| ExistsAsync_False  |  35.77 ns | 19.190 ns | 1.052 ns |  0.33 |    0.01 |      - |         - |        0.00 |
| ExistsAsync_True   |  40.90 ns |  4.557 ns | 0.250 ns |  0.38 |    0.00 |      - |         - |        0.00 |
| GetAsync_CacheHit  | 108.64 ns |  7.951 ns | 0.436 ns |  1.00 |    0.00 | 0.0086 |     144 B |        1.00 |
| GetAsync_CacheMiss |  76.58 ns | 47.753 ns | 2.618 ns |  0.70 |    0.02 |      - |         - |        0.00 |
