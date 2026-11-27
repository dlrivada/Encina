```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.43GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean       | Error    | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |-----------:|---------:|--------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  |   824.8 ns | 39.82 ns | 2.18 ns |  0.80 | 0.0067 |     616 B |        1.24 |
| ExistsAsync_True   |   833.5 ns | 30.87 ns | 1.69 ns |  0.81 | 0.0067 |     616 B |        1.24 |
| GetAsync_CacheHit  | 1,026.4 ns | 48.28 ns | 2.65 ns |  1.00 | 0.0057 |     496 B |        1.00 |
| GetAsync_CacheMiss |   823.6 ns | 19.49 ns | 1.07 ns |  0.80 | 0.0067 |     616 B |        1.24 |
