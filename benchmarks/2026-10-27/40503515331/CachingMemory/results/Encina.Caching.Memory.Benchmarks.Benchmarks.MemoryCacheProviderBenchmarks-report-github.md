```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean      | Error     | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------:|----------:|---------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  |  52.16 ns |  1.947 ns | 0.107 ns |  0.29 |      - |         - |        0.00 |
| ExistsAsync_True   |  59.94 ns |  1.429 ns | 0.078 ns |  0.33 |      - |         - |        0.00 |
| GetAsync_CacheHit  | 181.71 ns |  3.931 ns | 0.215 ns |  1.00 | 0.0086 |     144 B |        1.00 |
| GetAsync_CacheMiss | 129.47 ns | 33.475 ns | 1.835 ns |  0.71 |      - |         - |        0.00 |
