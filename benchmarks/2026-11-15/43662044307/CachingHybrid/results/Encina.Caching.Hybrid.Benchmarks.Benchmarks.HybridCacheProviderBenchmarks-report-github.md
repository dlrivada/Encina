```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.42GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method             | Mean     | Error    | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------- |---------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| ExistsAsync_False  | 737.0 ns | 11.73 ns | 17.19 ns |  0.81 |    0.02 | 0.0362 |     616 B |        1.24 |
| ExistsAsync_True   | 793.9 ns |  7.25 ns | 10.16 ns |  0.88 |    0.02 | 0.0362 |     616 B |        1.24 |
| GetAsync_CacheHit  | 907.3 ns |  8.46 ns | 12.13 ns |  1.00 |    0.02 | 0.0296 |     496 B |        1.00 |
| GetAsync_CacheMiss | 721.4 ns |  2.95 ns |  3.94 ns |  0.80 |    0.01 | 0.0362 |     616 B |        1.24 |
