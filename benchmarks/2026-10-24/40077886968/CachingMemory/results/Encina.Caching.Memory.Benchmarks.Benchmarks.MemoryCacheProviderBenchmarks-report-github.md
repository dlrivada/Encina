```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean      | Error    | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------:|---------:|---------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  |  52.15 ns | 1.300 ns | 0.071 ns |  0.28 |      - |         - |        0.00 |
| ExistsAsync_True   |  60.22 ns | 4.561 ns | 0.250 ns |  0.32 |      - |         - |        0.00 |
| GetAsync_CacheHit  | 187.36 ns | 6.421 ns | 0.352 ns |  1.00 | 0.0086 |     144 B |        1.00 |
| GetAsync_CacheMiss | 127.72 ns | 2.540 ns | 0.139 ns |  0.68 |      - |         - |        0.00 |
