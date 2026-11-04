```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.11GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean     | Error     | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |---------:|----------:|---------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  | 756.5 ns | 222.20 ns | 12.18 ns |  0.78 | 0.0362 |     616 B |        1.24 |
| ExistsAsync_True   | 809.6 ns | 158.46 ns |  8.69 ns |  0.83 | 0.0362 |     616 B |        1.24 |
| GetAsync_CacheHit  | 972.6 ns | 130.96 ns |  7.18 ns |  1.00 | 0.0286 |     496 B |        1.00 |
| GetAsync_CacheMiss | 760.8 ns |  20.78 ns |  1.14 ns |  0.78 | 0.0362 |     616 B |        1.24 |
