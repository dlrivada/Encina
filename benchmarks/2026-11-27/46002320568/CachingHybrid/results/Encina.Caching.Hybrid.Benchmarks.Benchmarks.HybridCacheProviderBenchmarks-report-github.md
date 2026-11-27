```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean       | Error    | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |-----------:|---------:|--------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  |   799.8 ns | 16.35 ns | 0.90 ns |  0.77 | 0.0067 |     616 B |        1.24 |
| ExistsAsync_True   |   849.8 ns | 42.65 ns | 2.34 ns |  0.82 | 0.0067 |     616 B |        1.24 |
| GetAsync_CacheHit  | 1,034.7 ns | 17.50 ns | 0.96 ns |  1.00 | 0.0057 |     496 B |        1.00 |
| GetAsync_CacheMiss |   793.3 ns | 31.24 ns | 1.71 ns |  0.77 | 0.0067 |     616 B |        1.24 |
