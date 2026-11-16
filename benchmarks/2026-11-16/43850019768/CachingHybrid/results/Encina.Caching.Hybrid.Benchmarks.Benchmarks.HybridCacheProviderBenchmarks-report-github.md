```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean       | Error     | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |-----------:|----------:|--------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  |   764.9 ns | 100.93 ns | 5.53 ns |  0.76 | 0.0067 |     616 B |        1.24 |
| ExistsAsync_True   |   803.7 ns |  20.82 ns | 1.14 ns |  0.80 | 0.0067 |     616 B |        1.24 |
| GetAsync_CacheHit  | 1,008.6 ns |  27.88 ns | 1.53 ns |  1.00 | 0.0057 |     496 B |        1.00 |
| GetAsync_CacheMiss |   767.8 ns | 111.30 ns | 6.10 ns |  0.76 | 0.0067 |     616 B |        1.24 |
