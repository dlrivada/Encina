```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.97GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean      | Error     | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------:|----------:|---------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  |  51.32 ns |  0.632 ns | 0.035 ns |  0.27 |      - |         - |        0.00 |
| ExistsAsync_True   |  58.35 ns |  1.843 ns | 0.101 ns |  0.31 |      - |         - |        0.00 |
| GetAsync_CacheHit  | 189.52 ns | 24.237 ns | 1.329 ns |  1.00 | 0.0017 |     144 B |        1.00 |
| GetAsync_CacheMiss | 129.17 ns |  9.506 ns | 0.521 ns |  0.68 |      - |         - |        0.00 |
