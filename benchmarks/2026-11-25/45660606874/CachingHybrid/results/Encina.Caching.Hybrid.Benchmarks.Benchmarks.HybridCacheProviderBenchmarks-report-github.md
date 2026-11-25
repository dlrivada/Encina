```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6973P-C 3.96GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean     | Error       | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------- |---------:|------------:|---------:|------:|--------:|-------:|----------:|------------:|
| ExistsAsync_False  | 788.8 ns | 1,183.59 ns | 64.88 ns |  0.84 |    0.06 | 0.0067 |     616 B |        1.24 |
| ExistsAsync_True   | 793.6 ns |    29.18 ns |  1.60 ns |  0.85 |    0.00 | 0.0067 |     616 B |        1.24 |
| GetAsync_CacheHit  | 936.5 ns |    23.04 ns |  1.26 ns |  1.00 |    0.00 | 0.0057 |     496 B |        1.00 |
| GetAsync_CacheMiss | 753.5 ns |    26.30 ns |  1.44 ns |  0.80 |    0.00 | 0.0067 |     616 B |        1.24 |
