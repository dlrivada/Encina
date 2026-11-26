```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.59GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| ExistsAsync_False  |  47.11 ns |  8.835 ns | 0.484 ns |  0.34 |    0.00 |      - |         - |        0.00 |
| ExistsAsync_True   |  50.24 ns |  1.586 ns | 0.087 ns |  0.37 |    0.00 |      - |         - |        0.00 |
| GetAsync_CacheHit  | 137.13 ns | 16.553 ns | 0.907 ns |  1.00 |    0.01 | 0.0086 |     144 B |        1.00 |
| GetAsync_CacheMiss | 101.09 ns | 60.513 ns | 3.317 ns |  0.74 |    0.02 |      - |         - |        0.00 |
