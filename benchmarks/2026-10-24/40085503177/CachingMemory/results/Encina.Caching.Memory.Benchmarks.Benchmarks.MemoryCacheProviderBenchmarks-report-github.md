```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| ExistsAsync_False  |  38.26 ns | 45.924 ns | 2.517 ns |  0.36 |    0.02 |      - |         - |        0.00 |
| ExistsAsync_True   |  41.95 ns | 42.709 ns | 2.341 ns |  0.39 |    0.02 |      - |         - |        0.00 |
| GetAsync_CacheHit  | 106.61 ns |  2.771 ns | 0.152 ns |  1.00 |    0.00 | 0.0086 |     144 B |        1.00 |
| GetAsync_CacheMiss |  75.50 ns | 10.846 ns | 0.595 ns |  0.71 |    0.00 |      - |         - |        0.00 |
