```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.54GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean      | Error     | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------:|----------:|---------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  |  36.22 ns |  2.419 ns | 0.133 ns |  0.36 |      - |         - |        0.00 |
| ExistsAsync_True   |  37.99 ns |  3.367 ns | 0.185 ns |  0.38 |      - |         - |        0.00 |
| GetAsync_CacheHit  | 100.58 ns | 14.668 ns | 0.804 ns |  1.00 | 0.0086 |     144 B |        1.00 |
| GetAsync_CacheMiss |  73.19 ns |  6.561 ns | 0.360 ns |  0.73 |      - |         - |        0.00 |
