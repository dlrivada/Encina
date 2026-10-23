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
| ExistsAsync_False  |  51.90 ns |  1.448 ns | 0.079 ns |  0.27 |      - |         - |        0.00 |
| ExistsAsync_True   |  60.73 ns |  4.615 ns | 0.253 ns |  0.32 |      - |         - |        0.00 |
| GetAsync_CacheHit  | 191.58 ns | 41.413 ns | 2.270 ns |  1.00 | 0.0086 |     144 B |        1.00 |
| GetAsync_CacheMiss | 127.47 ns |  1.462 ns | 0.080 ns |  0.67 |      - |         - |        0.00 |
