```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean     | Error     | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |---------:|----------:|---------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  | 34.06 ns | 11.754 ns | 0.644 ns |  0.34 |      - |         - |        0.00 |
| ExistsAsync_True   | 37.94 ns |  0.146 ns | 0.008 ns |  0.38 |      - |         - |        0.00 |
| GetAsync_CacheHit  | 98.73 ns |  4.382 ns | 0.240 ns |  1.00 | 0.0086 |     144 B |        1.00 |
| GetAsync_CacheMiss | 71.66 ns |  6.141 ns | 0.337 ns |  0.73 |      - |         - |        0.00 |
