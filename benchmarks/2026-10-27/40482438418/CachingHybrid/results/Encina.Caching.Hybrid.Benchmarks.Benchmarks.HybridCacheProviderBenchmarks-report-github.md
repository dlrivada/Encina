```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.17GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean     | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------- |---------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| ExistsAsync_False  | 766.9 ns |  16.45 ns |  0.90 ns |  0.79 |    0.02 | 0.0362 |     616 B |        1.24 |
| ExistsAsync_True   | 817.9 ns | 148.97 ns |  8.17 ns |  0.84 |    0.02 | 0.0362 |     616 B |        1.24 |
| GetAsync_CacheHit  | 975.4 ns | 483.19 ns | 26.49 ns |  1.00 |    0.03 | 0.0296 |     496 B |        1.00 |
| GetAsync_CacheMiss | 754.8 ns |  17.07 ns |  0.94 ns |  0.77 |    0.02 | 0.0362 |     616 B |        1.24 |
