```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean      | Error    | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------:|---------:|---------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  |  45.26 ns | 4.312 ns | 0.236 ns |  0.33 |      - |         - |        0.00 |
| ExistsAsync_True   |  50.07 ns | 3.726 ns | 0.204 ns |  0.37 |      - |         - |        0.00 |
| GetAsync_CacheHit  | 135.65 ns | 5.673 ns | 0.311 ns |  1.00 | 0.0086 |     144 B |        1.00 |
| GetAsync_CacheMiss |  99.50 ns | 3.559 ns | 0.195 ns |  0.73 |      - |         - |        0.00 |
