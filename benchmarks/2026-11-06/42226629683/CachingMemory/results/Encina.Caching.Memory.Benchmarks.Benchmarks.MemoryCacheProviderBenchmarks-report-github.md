```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 2.85GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean      | Error    | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------:|---------:|---------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  |  60.08 ns | 1.381 ns | 0.076 ns |  0.35 |      - |         - |        0.00 |
| ExistsAsync_True   |  65.03 ns | 4.439 ns | 0.243 ns |  0.38 |      - |         - |        0.00 |
| GetAsync_CacheHit  | 172.77 ns | 8.748 ns | 0.480 ns |  1.00 | 0.0086 |     144 B |        1.00 |
| GetAsync_CacheMiss | 124.56 ns | 3.389 ns | 0.186 ns |  0.72 |      - |         - |        0.00 |
