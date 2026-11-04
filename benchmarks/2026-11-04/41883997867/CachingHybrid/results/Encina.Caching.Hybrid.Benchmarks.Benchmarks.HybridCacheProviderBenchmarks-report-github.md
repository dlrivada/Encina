```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.33GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean     | Error    | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------- |---------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| ExistsAsync_False  | 781.1 ns | 537.5 ns | 29.46 ns |  0.85 |    0.03 | 0.0362 |     616 B |        1.24 |
| ExistsAsync_True   | 801.8 ns | 423.4 ns | 23.21 ns |  0.87 |    0.03 | 0.0362 |     616 B |        1.24 |
| GetAsync_CacheHit  | 918.6 ns | 407.6 ns | 22.34 ns |  1.00 |    0.03 | 0.0296 |     496 B |        1.00 |
| GetAsync_CacheMiss | 738.0 ns | 262.2 ns | 14.37 ns |  0.80 |    0.02 | 0.0362 |     616 B |        1.24 |
