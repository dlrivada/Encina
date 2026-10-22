```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]    : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method             | Mean      | Error    | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| ExistsAsync_False  |  43.79 ns | 0.039 ns | 0.058 ns |  0.33 |    0.00 |      - |         - |        0.00 |
| ExistsAsync_True   |  50.22 ns | 0.067 ns | 0.089 ns |  0.38 |    0.00 |      - |         - |        0.00 |
| GetAsync_CacheHit  | 133.30 ns | 0.330 ns | 0.483 ns |  1.00 |    0.01 | 0.0086 |     144 B |        1.00 |
| GetAsync_CacheMiss | 100.76 ns | 2.625 ns | 3.593 ns |  0.76 |    0.03 |      - |         - |        0.00 |
