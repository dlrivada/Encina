```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6973P-C 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean       | Error    | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------- |-----------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| ExistsAsync_False  |   803.2 ns | 217.4 ns | 11.92 ns |  0.75 |    0.01 | 0.0067 |     616 B |        1.24 |
| ExistsAsync_True   |   845.5 ns | 125.8 ns |  6.89 ns |  0.79 |    0.01 | 0.0067 |     616 B |        1.24 |
| GetAsync_CacheHit  | 1,068.7 ns | 122.8 ns |  6.73 ns |  1.00 |    0.01 | 0.0057 |     496 B |        1.00 |
| GetAsync_CacheMiss |   823.1 ns | 370.7 ns | 20.32 ns |  0.77 |    0.02 | 0.0067 |     616 B |        1.24 |
