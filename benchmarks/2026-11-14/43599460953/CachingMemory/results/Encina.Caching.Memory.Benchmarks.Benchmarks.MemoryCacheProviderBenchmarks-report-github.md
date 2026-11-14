```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.10GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| ExistsAsync_False  |  36.44 ns |  2.796 ns | 0.153 ns |  0.34 |    0.00 |      - |         - |        0.00 |
| ExistsAsync_True   |  41.08 ns |  9.245 ns | 0.507 ns |  0.38 |    0.01 |      - |         - |        0.00 |
| GetAsync_CacheHit  | 107.65 ns | 18.917 ns | 1.037 ns |  1.00 |    0.01 | 0.0086 |     144 B |        1.00 |
| GetAsync_CacheMiss |  80.90 ns | 46.000 ns | 2.521 ns |  0.75 |    0.02 |      - |         - |        0.00 |
