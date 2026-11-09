```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.77GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean      | Error     | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------:|----------:|---------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  |  53.33 ns |  1.561 ns | 0.086 ns |  0.29 |      - |         - |        0.00 |
| ExistsAsync_True   |  60.42 ns |  1.623 ns | 0.089 ns |  0.33 |      - |         - |        0.00 |
| GetAsync_CacheHit  | 181.70 ns | 32.836 ns | 1.800 ns |  1.00 | 0.0086 |     144 B |        1.00 |
| GetAsync_CacheMiss | 130.59 ns | 11.554 ns | 0.633 ns |  0.72 |      - |         - |        0.00 |
