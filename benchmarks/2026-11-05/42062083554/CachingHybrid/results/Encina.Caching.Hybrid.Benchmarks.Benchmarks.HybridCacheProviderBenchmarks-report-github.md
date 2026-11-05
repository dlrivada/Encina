```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.11GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean       | Error    | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------- |-----------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| ExistsAsync_False  |   819.1 ns | 110.4 ns |  6.05 ns |  0.80 |    0.01 | 0.0362 |     616 B |        1.24 |
| ExistsAsync_True   |   907.2 ns | 541.2 ns | 29.66 ns |  0.89 |    0.03 | 0.0362 |     616 B |        1.24 |
| GetAsync_CacheHit  | 1,021.5 ns | 217.0 ns | 11.89 ns |  1.00 |    0.01 | 0.0286 |     496 B |        1.00 |
| GetAsync_CacheMiss |   802.1 ns | 148.7 ns |  8.15 ns |  0.79 |    0.01 | 0.0362 |     616 B |        1.24 |
