```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean       | Error    | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |-----------:|---------:|--------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  |   827.4 ns | 28.33 ns | 1.55 ns |  0.81 | 0.0067 |     616 B |        1.24 |
| ExistsAsync_True   |   819.8 ns | 81.41 ns | 4.46 ns |  0.80 | 0.0067 |     616 B |        1.24 |
| GetAsync_CacheHit  | 1,026.5 ns | 47.00 ns | 2.58 ns |  1.00 | 0.0057 |     496 B |        1.00 |
| GetAsync_CacheMiss |   785.7 ns | 26.64 ns | 1.46 ns |  0.77 | 0.0067 |     616 B |        1.24 |
