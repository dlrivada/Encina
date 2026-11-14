```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean      | Error     | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------:|----------:|---------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  |  44.76 ns |  5.107 ns | 0.280 ns |  0.33 |      - |         - |        0.00 |
| ExistsAsync_True   |  51.60 ns | 15.144 ns | 0.830 ns |  0.38 |      - |         - |        0.00 |
| GetAsync_CacheHit  | 136.47 ns | 24.046 ns | 1.318 ns |  1.00 | 0.0086 |     144 B |        1.00 |
| GetAsync_CacheMiss |  98.00 ns |  6.121 ns | 0.336 ns |  0.72 |      - |         - |        0.00 |
