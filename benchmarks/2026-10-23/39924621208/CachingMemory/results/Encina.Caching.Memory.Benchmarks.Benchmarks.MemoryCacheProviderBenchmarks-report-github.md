```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean      | Error     | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------:|----------:|---------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  |  51.99 ns |  0.860 ns | 0.047 ns |  0.28 |      - |         - |        0.00 |
| ExistsAsync_True   |  60.27 ns |  0.983 ns | 0.054 ns |  0.32 |      - |         - |        0.00 |
| GetAsync_CacheHit  | 185.91 ns | 10.365 ns | 0.568 ns |  1.00 | 0.0086 |     144 B |        1.00 |
| GetAsync_CacheMiss | 128.63 ns |  6.955 ns | 0.381 ns |  0.69 |      - |         - |        0.00 |
