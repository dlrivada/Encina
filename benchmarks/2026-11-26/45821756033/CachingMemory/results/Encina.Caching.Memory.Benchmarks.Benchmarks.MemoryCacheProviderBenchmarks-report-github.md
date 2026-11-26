```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| ExistsAsync_False  |  35.92 ns | 14.186 ns | 0.778 ns |  0.34 |    0.01 |      - |         - |        0.00 |
| ExistsAsync_True   |  40.31 ns |  3.308 ns | 0.181 ns |  0.38 |    0.00 |      - |         - |        0.00 |
| GetAsync_CacheHit  | 106.14 ns |  9.839 ns | 0.539 ns |  1.00 |    0.01 | 0.0086 |     144 B |        1.00 |
| GetAsync_CacheMiss |  79.16 ns | 57.401 ns | 3.146 ns |  0.75 |    0.03 |      - |         - |        0.00 |
