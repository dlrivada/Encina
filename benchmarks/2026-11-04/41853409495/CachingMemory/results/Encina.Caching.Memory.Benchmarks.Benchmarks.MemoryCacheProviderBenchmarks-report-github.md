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
| ExistsAsync_False  |  44.47 ns | 1.107 ns | 0.061 ns |  0.32 |      - |         - |        0.00 |
| ExistsAsync_True   |  50.10 ns | 2.280 ns | 0.125 ns |  0.36 |      - |         - |        0.00 |
| GetAsync_CacheHit  | 140.53 ns | 7.409 ns | 0.406 ns |  1.00 | 0.0086 |     144 B |        1.00 |
| GetAsync_CacheMiss |  96.66 ns | 0.761 ns | 0.042 ns |  0.69 |      - |         - |        0.00 |
