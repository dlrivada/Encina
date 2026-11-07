```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean      | Error    | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------:|---------:|---------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  |  43.79 ns | 1.073 ns | 0.059 ns |  0.33 |      - |         - |        0.00 |
| ExistsAsync_True   |  50.35 ns | 5.110 ns | 0.280 ns |  0.38 |      - |         - |        0.00 |
| GetAsync_CacheHit  | 133.62 ns | 3.377 ns | 0.185 ns |  1.00 | 0.0086 |     144 B |        1.00 |
| GetAsync_CacheMiss | 105.15 ns | 2.540 ns | 0.139 ns |  0.79 |      - |         - |        0.00 |
