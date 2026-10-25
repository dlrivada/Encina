```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.52GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean     | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------- |---------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| ExistsAsync_False  | 751.5 ns | 465.09 ns | 25.49 ns |  0.84 |    0.02 | 0.0362 |     616 B |        1.24 |
| ExistsAsync_True   | 786.0 ns | 264.06 ns | 14.47 ns |  0.88 |    0.01 | 0.0362 |     616 B |        1.24 |
| GetAsync_CacheHit  | 891.9 ns |  56.85 ns |  3.12 ns |  1.00 |    0.00 | 0.0296 |     496 B |        1.00 |
| GetAsync_CacheMiss | 699.8 ns |  73.56 ns |  4.03 ns |  0.78 |    0.00 | 0.0362 |     616 B |        1.24 |
