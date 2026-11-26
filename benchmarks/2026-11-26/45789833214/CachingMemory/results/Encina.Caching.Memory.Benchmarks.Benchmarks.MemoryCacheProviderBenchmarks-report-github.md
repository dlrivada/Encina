```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean      | Error     | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------:|----------:|---------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  |  43.98 ns |  4.482 ns | 0.246 ns |  0.32 |      - |         - |        0.00 |
| ExistsAsync_True   |  51.30 ns |  0.251 ns | 0.014 ns |  0.37 |      - |         - |        0.00 |
| GetAsync_CacheHit  | 139.47 ns |  5.785 ns | 0.317 ns |  1.00 | 0.0086 |     144 B |        1.00 |
| GetAsync_CacheMiss |  98.00 ns | 14.282 ns | 0.783 ns |  0.70 |      - |         - |        0.00 |
