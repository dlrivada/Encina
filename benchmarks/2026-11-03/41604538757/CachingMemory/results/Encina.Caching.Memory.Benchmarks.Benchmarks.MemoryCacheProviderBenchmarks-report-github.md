```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6973P-C 4.08GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| ExistsAsync_False  |  39.09 ns | 21.927 ns | 1.202 ns |  0.29 |    0.01 |      - |         - |        0.00 |
| ExistsAsync_True   |  45.05 ns |  3.987 ns | 0.219 ns |  0.34 |    0.01 |      - |         - |        0.00 |
| GetAsync_CacheHit  | 132.59 ns | 69.739 ns | 3.823 ns |  1.00 |    0.04 | 0.0017 |     144 B |        1.00 |
| GetAsync_CacheMiss |  86.98 ns |  5.591 ns | 0.306 ns |  0.66 |    0.02 |      - |         - |        0.00 |
