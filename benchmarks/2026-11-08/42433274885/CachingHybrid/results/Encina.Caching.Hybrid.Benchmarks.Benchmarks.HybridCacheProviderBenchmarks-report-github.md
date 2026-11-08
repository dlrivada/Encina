```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.53GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]    : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method             | Mean     | Error   | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |---------:|--------:|---------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  | 709.6 ns | 5.05 ns |  6.91 ns |  0.81 | 0.0362 |     616 B |        1.24 |
| ExistsAsync_True   | 753.1 ns | 8.82 ns | 12.65 ns |  0.86 | 0.0362 |     616 B |        1.24 |
| GetAsync_CacheHit  | 877.2 ns | 2.09 ns |  3.06 ns |  1.00 | 0.0296 |     496 B |        1.00 |
| GetAsync_CacheMiss | 689.8 ns | 1.45 ns |  2.08 ns |  0.79 | 0.0362 |     616 B |        1.24 |
