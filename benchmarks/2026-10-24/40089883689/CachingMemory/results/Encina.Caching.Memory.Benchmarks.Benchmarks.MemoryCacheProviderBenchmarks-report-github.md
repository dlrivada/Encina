```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| ExistsAsync_False  |  51.83 ns |  0.222 ns | 0.012 ns |  0.28 |    0.00 |      - |         - |        0.00 |
| ExistsAsync_True   |  61.42 ns |  3.596 ns | 0.197 ns |  0.33 |    0.00 |      - |         - |        0.00 |
| GetAsync_CacheHit  | 184.97 ns |  5.733 ns | 0.314 ns |  1.00 |    0.00 | 0.0086 |     144 B |        1.00 |
| GetAsync_CacheMiss | 129.04 ns | 58.644 ns | 3.214 ns |  0.70 |    0.02 |      - |         - |        0.00 |
