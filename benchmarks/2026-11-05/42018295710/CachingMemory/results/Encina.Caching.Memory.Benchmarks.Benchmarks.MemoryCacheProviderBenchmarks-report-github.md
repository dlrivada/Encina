```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 2.86GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean      | Error    | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------:|---------:|---------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  |  56.84 ns | 0.301 ns | 0.016 ns |  0.33 |      - |         - |        0.00 |
| ExistsAsync_True   |  65.16 ns | 0.293 ns | 0.016 ns |  0.37 |      - |         - |        0.00 |
| GetAsync_CacheHit  | 174.17 ns | 7.596 ns | 0.416 ns |  1.00 | 0.0086 |     144 B |        1.00 |
| GetAsync_CacheMiss | 132.97 ns | 1.199 ns | 0.066 ns |  0.76 |      - |         - |        0.00 |
