```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]    : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method             | Mean     | Error    | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------- |---------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| ExistsAsync_False  | 770.0 ns | 10.09 ns | 14.46 ns |  0.82 |    0.02 | 0.0362 |     616 B |        1.24 |
| ExistsAsync_True   | 837.0 ns | 12.93 ns | 18.95 ns |  0.90 |    0.02 | 0.0362 |     616 B |        1.24 |
| GetAsync_CacheHit  | 935.0 ns |  8.98 ns | 13.17 ns |  1.00 |    0.02 | 0.0286 |     496 B |        1.00 |
| GetAsync_CacheMiss | 748.7 ns |  5.86 ns |  8.58 ns |  0.80 |    0.01 | 0.0362 |     616 B |        1.24 |
