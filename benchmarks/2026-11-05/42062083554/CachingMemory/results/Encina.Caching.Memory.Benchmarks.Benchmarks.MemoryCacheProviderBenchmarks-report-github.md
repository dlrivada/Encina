```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.65GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean      | Error    | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------:|---------:|---------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  |  44.24 ns | 0.838 ns | 0.046 ns |  0.33 |      - |         - |        0.00 |
| ExistsAsync_True   |  50.16 ns | 0.355 ns | 0.019 ns |  0.37 |      - |         - |        0.00 |
| GetAsync_CacheHit  | 133.97 ns | 8.526 ns | 0.467 ns |  1.00 | 0.0086 |     144 B |        1.00 |
| GetAsync_CacheMiss | 100.93 ns | 1.253 ns | 0.069 ns |  0.75 |      - |         - |        0.00 |
