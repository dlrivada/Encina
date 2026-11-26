```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean      | Error     | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------:|----------:|---------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  |  44.20 ns | 11.144 ns | 0.611 ns |  0.32 |      - |         - |        0.00 |
| ExistsAsync_True   |  51.10 ns |  2.655 ns | 0.146 ns |  0.37 |      - |         - |        0.00 |
| GetAsync_CacheHit  | 136.36 ns |  8.319 ns | 0.456 ns |  1.00 | 0.0086 |     144 B |        1.00 |
| GetAsync_CacheMiss |  99.19 ns | 19.383 ns | 1.062 ns |  0.73 |      - |         - |        0.00 |
