```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.67GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean      | Error     | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------:|----------:|---------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  |  54.47 ns |  3.360 ns | 0.184 ns |  0.30 |      - |         - |        0.00 |
| ExistsAsync_True   |  60.28 ns |  4.021 ns | 0.220 ns |  0.33 |      - |         - |        0.00 |
| GetAsync_CacheHit  | 181.11 ns | 37.990 ns | 2.082 ns |  1.00 | 0.0086 |     144 B |        1.00 |
| GetAsync_CacheMiss | 124.90 ns |  1.579 ns | 0.087 ns |  0.69 |      - |         - |        0.00 |
