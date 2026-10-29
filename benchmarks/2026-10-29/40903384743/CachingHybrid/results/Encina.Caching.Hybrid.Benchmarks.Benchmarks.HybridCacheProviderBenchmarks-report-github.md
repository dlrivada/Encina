```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 3.71GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean       | Error      | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------- |-----------:|-----------:|---------:|------:|--------:|-------:|----------:|------------:|
| ExistsAsync_False  |   811.3 ns |   675.6 ns | 37.03 ns |  0.81 |    0.05 | 0.0067 |     616 B |        1.24 |
| ExistsAsync_True   |   796.2 ns |   366.2 ns | 20.07 ns |  0.79 |    0.04 | 0.0067 |     616 B |        1.24 |
| GetAsync_CacheHit  | 1,006.3 ns | 1,128.0 ns | 61.83 ns |  1.00 |    0.07 | 0.0057 |     496 B |        1.00 |
| GetAsync_CacheMiss |   857.0 ns |   695.3 ns | 38.11 ns |  0.85 |    0.05 | 0.0067 |     616 B |        1.24 |
