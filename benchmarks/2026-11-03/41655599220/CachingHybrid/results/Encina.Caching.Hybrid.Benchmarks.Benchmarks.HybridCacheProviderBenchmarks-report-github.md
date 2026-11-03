```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.33GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean     | Error     | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |---------:|----------:|---------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  | 756.3 ns | 202.92 ns | 11.12 ns |  0.80 | 0.0362 |     616 B |        1.24 |
| ExistsAsync_True   | 786.6 ns |  62.18 ns |  3.41 ns |  0.83 | 0.0362 |     616 B |        1.24 |
| GetAsync_CacheHit  | 942.9 ns |  64.77 ns |  3.55 ns |  1.00 | 0.0286 |     496 B |        1.00 |
| GetAsync_CacheMiss | 747.2 ns |  84.22 ns |  4.62 ns |  0.79 | 0.0362 |     616 B |        1.24 |
