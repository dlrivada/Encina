```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| ExistsAsync_False  |  44.24 ns |  0.496 ns | 0.027 ns |  0.32 |    0.00 |      - |         - |        0.00 |
| ExistsAsync_True   |  51.16 ns |  4.694 ns | 0.257 ns |  0.37 |    0.01 |      - |         - |        0.00 |
| GetAsync_CacheHit  | 138.25 ns | 41.540 ns | 2.277 ns |  1.00 |    0.02 | 0.0086 |     144 B |        1.00 |
| GetAsync_CacheMiss |  97.50 ns |  4.385 ns | 0.240 ns |  0.71 |    0.01 |      - |         - |        0.00 |
