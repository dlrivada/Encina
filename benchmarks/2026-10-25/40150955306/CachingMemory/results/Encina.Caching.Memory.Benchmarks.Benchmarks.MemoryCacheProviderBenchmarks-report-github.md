```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]    : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method             | Mean      | Error    | StdDev   | Median    | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------:|---------:|---------:|----------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  |  43.72 ns | 0.038 ns | 0.055 ns |  43.73 ns |  0.33 |      - |         - |        0.00 |
| ExistsAsync_True   |  50.39 ns | 0.246 ns | 0.361 ns |  50.12 ns |  0.38 |      - |         - |        0.00 |
| GetAsync_CacheHit  | 132.36 ns | 0.268 ns | 0.402 ns | 132.34 ns |  1.00 | 0.0086 |     144 B |        1.00 |
| GetAsync_CacheMiss |  97.36 ns | 0.108 ns | 0.148 ns |  97.40 ns |  0.74 |      - |         - |        0.00 |
