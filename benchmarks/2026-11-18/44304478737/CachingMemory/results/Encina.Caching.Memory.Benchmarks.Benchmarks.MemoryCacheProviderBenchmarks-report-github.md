```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean      | Error    | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------:|---------:|---------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  |  44.54 ns | 2.817 ns | 0.154 ns |  0.29 |      - |         - |        0.00 |
| ExistsAsync_True   |  50.41 ns | 3.888 ns | 0.213 ns |  0.33 |      - |         - |        0.00 |
| GetAsync_CacheHit  | 151.50 ns | 7.206 ns | 0.395 ns |  1.00 | 0.0017 |     144 B |        1.00 |
| GetAsync_CacheMiss |  98.39 ns | 1.527 ns | 0.084 ns |  0.65 |      - |         - |        0.00 |
