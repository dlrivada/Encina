```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.64GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean      | Error    | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------:|---------:|---------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  |  45.09 ns | 2.142 ns | 0.117 ns |  0.34 |      - |         - |        0.00 |
| ExistsAsync_True   |  50.23 ns | 2.214 ns | 0.121 ns |  0.38 |      - |         - |        0.00 |
| GetAsync_CacheHit  | 132.85 ns | 1.304 ns | 0.071 ns |  1.00 | 0.0086 |     144 B |        1.00 |
| GetAsync_CacheMiss |  97.49 ns | 0.381 ns | 0.021 ns |  0.73 |      - |         - |        0.00 |
