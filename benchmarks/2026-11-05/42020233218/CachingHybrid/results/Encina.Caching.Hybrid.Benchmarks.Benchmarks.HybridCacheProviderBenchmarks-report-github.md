```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.22GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean     | Error     | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |---------:|----------:|---------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  | 773.0 ns | 110.97 ns |  6.08 ns |  0.81 | 0.0362 |     616 B |        1.24 |
| ExistsAsync_True   | 805.9 ns | 226.66 ns | 12.42 ns |  0.85 | 0.0362 |     616 B |        1.24 |
| GetAsync_CacheHit  | 949.6 ns |  35.04 ns |  1.92 ns |  1.00 | 0.0296 |     496 B |        1.00 |
| GetAsync_CacheMiss | 754.5 ns | 261.22 ns | 14.32 ns |  0.79 | 0.0362 |     616 B |        1.24 |
