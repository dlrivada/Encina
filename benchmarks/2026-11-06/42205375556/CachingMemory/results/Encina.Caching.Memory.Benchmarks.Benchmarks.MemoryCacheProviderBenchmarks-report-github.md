```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.51GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean      | Error    | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| ExistsAsync_False  |  36.46 ns | 46.01 ns | 2.522 ns |  0.33 |    0.02 |      - |         - |        0.00 |
| ExistsAsync_True   |  40.54 ns | 30.28 ns | 1.660 ns |  0.37 |    0.02 |      - |         - |        0.00 |
| GetAsync_CacheHit  | 109.31 ns | 50.54 ns | 2.770 ns |  1.00 |    0.03 | 0.0086 |     144 B |        1.00 |
| GetAsync_CacheMiss |  75.24 ns | 11.44 ns | 0.627 ns |  0.69 |    0.02 |      - |         - |        0.00 |
