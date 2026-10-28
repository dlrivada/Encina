```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 3.95GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| ExistsAsync_False  |  41.87 ns |  28.40 ns | 1.557 ns |  0.30 |    0.02 |      - |         - |        0.00 |
| ExistsAsync_True   |  47.05 ns |  30.54 ns | 1.674 ns |  0.34 |    0.02 |      - |         - |        0.00 |
| GetAsync_CacheHit  | 138.41 ns | 137.49 ns | 7.536 ns |  1.00 |    0.07 | 0.0017 |     144 B |        1.00 |
| GetAsync_CacheMiss |  86.64 ns |  11.20 ns | 0.614 ns |  0.63 |    0.03 |      - |         - |        0.00 |
