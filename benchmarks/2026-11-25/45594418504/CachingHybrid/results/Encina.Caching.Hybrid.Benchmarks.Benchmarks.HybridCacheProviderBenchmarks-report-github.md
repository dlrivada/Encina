```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean       | Error     | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |-----------:|----------:|--------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  |   799.3 ns | 170.07 ns | 9.32 ns |  0.76 | 0.0067 |     616 B |        1.24 |
| ExistsAsync_True   |   806.8 ns |  98.16 ns | 5.38 ns |  0.77 | 0.0067 |     616 B |        1.24 |
| GetAsync_CacheHit  | 1,045.1 ns | 108.53 ns | 5.95 ns |  1.00 | 0.0057 |     496 B |        1.00 |
| GetAsync_CacheMiss |   767.0 ns | 104.97 ns | 5.75 ns |  0.73 | 0.0067 |     616 B |        1.24 |
