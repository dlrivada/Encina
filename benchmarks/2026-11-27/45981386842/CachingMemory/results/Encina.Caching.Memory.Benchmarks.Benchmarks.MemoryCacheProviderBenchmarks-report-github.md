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
| ExistsAsync_False  |  44.64 ns | 1.546 ns | 0.085 ns |  0.32 |      - |         - |        0.00 |
| ExistsAsync_True   |  50.39 ns | 0.903 ns | 0.049 ns |  0.36 |      - |         - |        0.00 |
| GetAsync_CacheHit  | 138.75 ns | 4.354 ns | 0.239 ns |  1.00 | 0.0086 |     144 B |        1.00 |
| GetAsync_CacheMiss |  97.33 ns | 0.251 ns | 0.014 ns |  0.70 |      - |         - |        0.00 |
