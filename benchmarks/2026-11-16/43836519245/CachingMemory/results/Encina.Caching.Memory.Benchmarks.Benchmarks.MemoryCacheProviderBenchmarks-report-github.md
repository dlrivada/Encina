```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean      | Error    | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------:|---------:|---------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  |  52.31 ns | 2.106 ns | 0.115 ns |  0.29 |      - |         - |        0.00 |
| ExistsAsync_True   |  60.48 ns | 7.619 ns | 0.418 ns |  0.34 |      - |         - |        0.00 |
| GetAsync_CacheHit  | 179.34 ns | 5.603 ns | 0.307 ns |  1.00 | 0.0086 |     144 B |        1.00 |
| GetAsync_CacheMiss | 126.84 ns | 1.774 ns | 0.097 ns |  0.71 |      - |         - |        0.00 |
