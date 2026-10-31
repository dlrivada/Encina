```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.59GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean      | Error    | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------:|---------:|---------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  |  55.36 ns | 0.510 ns | 0.028 ns |  0.30 |      - |         - |        0.00 |
| ExistsAsync_True   |  60.89 ns | 3.383 ns | 0.185 ns |  0.33 |      - |         - |        0.00 |
| GetAsync_CacheHit  | 182.16 ns | 6.318 ns | 0.346 ns |  1.00 | 0.0086 |     144 B |        1.00 |
| GetAsync_CacheMiss | 130.25 ns | 0.349 ns | 0.019 ns |  0.72 |      - |         - |        0.00 |
