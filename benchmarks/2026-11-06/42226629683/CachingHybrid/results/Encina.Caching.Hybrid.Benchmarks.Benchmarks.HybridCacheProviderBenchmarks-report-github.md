```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.53GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean     | Error     | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |---------:|----------:|---------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  | 716.6 ns | 216.85 ns | 11.89 ns |  0.81 | 0.0362 |     616 B |        1.24 |
| ExistsAsync_True   | 791.0 ns |  57.73 ns |  3.16 ns |  0.89 | 0.0362 |     616 B |        1.24 |
| GetAsync_CacheHit  | 884.8 ns | 117.67 ns |  6.45 ns |  1.00 | 0.0296 |     496 B |        1.00 |
| GetAsync_CacheMiss | 689.7 ns |  63.74 ns |  3.49 ns |  0.78 | 0.0362 |     616 B |        1.24 |
